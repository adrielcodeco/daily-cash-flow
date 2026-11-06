# Operação

## Recuperação de desastres

| Cenário | Estratégia | RTO proposto | RPO proposto |
|---|---|---:|---:|
| Falha de uma instância | Cloud Run recria a instância | Segundos | 0 |
| Falha de uma zona | Cloud SQL regional faz failover, e Cloud Run segue em outras zonas | Minutos | 0 |
| Erro humano no banco | Recuperação a um ponto no tempo | 1 hora | 5 minutos |
| Perda do consolidado | Reconstrução a partir dos lançamentos | 1 hora | 0 |
| Perda da região | Réplica entre regiões promovida, com serviços implantados na região secundária | 1 hora | 5 minutos |

O consolidado é derivado dos lançamentos, então o banco de lançamentos é o ativo crítico. Backups diários e logs de transação têm retenção definida pela conformidade.

Uma réplica fora do Brasil depende de avaliação regulatória, porque o GCP tem uma única região no país. Enquanto isso não estiver resolvido, a recuperação regional fica como proposta.

O plano só vale se for testado. A proposta é um exercício de restauração por trimestre, com o tempo medido.

## Runbooks

### Outbox parada

**Sintoma:** evento pendente há mais de 2 minutos.

1. Verifique se `outbox-publisher` está em execução e leia os logs.
2. Verifique se a mensageria responde.
3. Reinicie o publicador, se necessário. Ele retoma do que ficou pendente.
4. Lançamentos continuam sendo aceitos durante o problema.

### Consumidor atrasado

**Sintoma:** mensagem mais antiga da assinatura há mais de 2 minutos.

1. Verifique se `consolidation-consumer` está em execução.
2. Verifique as conexões e a CPU do banco do consolidado.
3. Aumente as instâncias do consumidor sem ultrapassar o limite de conexões.
4. Acompanhe a defasagem até voltar à meta.

### Mensagens mortas

**Sintoma:** mensagem no tópico de mensagens mortas.

1. Leia a mensagem e os logs do consumidor.
2. Se o problema for do consumidor, corrija e reenvie a mensagem ao tópico principal.
3. Se a mensagem for inválida, corrija a origem. O lançamento continua no banco de lançamentos.
4. Rode a reconciliação do comerciante afetado.

### Banco indisponível

**Sintoma:** escritas falhando e `health/ready` com erro.

1. Verifique se há failover em andamento no Cloud SQL.
2. Se for esgotamento de conexões, revise pools e instâncias.
3. Se for perda de dados, inicie a recuperação a um ponto no tempo.
4. O saldo calculado segue consultável durante uma queda do banco de lançamentos.

### Saldo divergente

**Sintoma:** diferença entre os lançamentos e o saldo.

1. Confira se há mensagens mortas ou fila acumulada.
2. Pare o consumidor e limpe o consolidado.
3. Reenvie os eventos. Localmente, `./scripts/rebuild-consolidation.sh` faz os dois passos.
4. Confira o saldo. Evento repetido não altera o resultado, porque `eventId` é a identidade do lançamento.

## Implantação

- Cada serviço é implantado de forma independente, com revisões do Cloud Run e migração gradual de tráfego
- Migrações de banco rodam como job antes da nova revisão e são compatíveis com a versão anterior
- Tópico e assinatura são provisionados antes do publicador
- Uma revisão com erro volta à anterior pelo roteamento de tráfego

## Manutenção pendente

| Tarefa | Situação |
|---|---|
| Limpeza de eventos publicados da outbox | Proposta, com retenção a definir |
| Limpeza de `processed_event` e `idempotency_key` | Proposta, com retenção maior que a janela de repetição |
| Reconciliação periódica automática | Proposta |
