# Operação

O plano para desastres está em [recuperação de desastres](17-recuperacao-de-desastres.md). Este documento trata do dia a dia.

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
4. Confira o saldo do comerciante afetado. A reconciliação automática ainda é uma proposta.

### Banco indisponível

**Sintoma:** escritas falhando e `health/ready` com erro.

1. Verifique se há failover em andamento no Cloud SQL.
2. Se for esgotamento de conexões, revise pools e instâncias.
3. Se for perda de dados, siga o [plano de recuperação](17-recuperacao-de-desastres.md#erro-humano-no-banco-de-lançamentos).
4. O saldo calculado segue consultável durante uma queda do banco de lançamentos.

### Saldo divergente

**Sintoma:** diferença entre os lançamentos e o saldo.

1. Confira se há mensagens mortas ou fila acumulada.
2. Coloque a leitura de saldo em manutenção, porque a API responde saldo parcial durante a reconstrução.
3. Pare o consumidor e limpe o consolidado.
4. Reenvie os eventos. Localmente, `./scripts/rebuild-consolidation.sh` faz os dois passos.
5. Confira o saldo. Evento repetido não altera o resultado, porque `eventId` é a identidade do lançamento.

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
