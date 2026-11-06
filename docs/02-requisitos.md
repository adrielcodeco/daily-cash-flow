# Requisitos

## Premissas

| ID | Premissa |
|---|---|
| PRE-01 | As 50 req/s são consultas ao relatório. A arquitetura também suporta 50 eventos por segundo |
| PRE-02 | Os 5% de perda valem para consultas. Lançamentos confirmados têm perda zero |
| PRE-03 | O sistema atende vários comerciantes |
| PRE-04 | Moeda única, BRL, em centavos inteiros |
| PRE-05 | O dia é a data de competência, no fuso `America/Sao_Paulo` |
| PRE-06 | Lançamentos são imutáveis |
| PRE-07 | Lançamentos retroativos são permitidos |
| PRE-08 | O saldo acumula os dias anteriores |
| PRE-09 | O legado da arquitetura de transição é um cenário assumido: sistema único com banco compartilhado |
| PRE-10 | A arquitetura-alvo roda no GCP, em `southamerica-east1` |
| PRE-11 | A solução considera exigências de uma instituição financeira. A aplicabilidade exata deve ser validada com a área de conformidade |

## Requisitos funcionais

| ID | Requisito | Prioridade | Critério de aceite |
|---|---|---|---|
| RF-01 | Registrar lançamento | Essencial | Dado um lançamento válido, quando enviado, então ele é gravado e o evento fica na outbox |
| RF-02 | Evitar duplicidade | Essencial | Dada a mesma `Idempotency-Key`, quando repetida, então nenhum lançamento novo é criado e a resposta original é devolvida |
| RF-03 | Recusar chave reutilizada | Importante | Dada a mesma chave com outro conteúdo, quando enviada, então a resposta é 409 |
| RF-04 | Consultar lançamento | Essencial | Dado um lançamento de outro comerciante, quando consultado, então a resposta é 404 |
| RF-05 | Listar lançamentos do dia | Importante | Dada uma data, quando consultada, então a lista vem paginada |
| RF-06 | Estornar lançamento | Essencial | Dado um lançamento já estornado, quando estornado de novo, então a resposta é 409 |
| RF-07 | Consultar saldo do dia | Essencial | Dada uma data, quando consultada, então vêm abertura, créditos, débitos, fechamento e `updatedThrough` |
| RF-08 | Consultar saldo do período | Importante | Dado um período de até 366 dias, quando consultado, então vem um saldo por dia |
| RF-09 | Reconstruir consolidado | Importante | Dada uma reconstrução, quando concluída, então o saldo é igual à soma dos lançamentos |

## Requisitos não funcionais

| ID | Atributo | Meta | Medição | Origem |
|---|---|---|---|---|
| RNF-01 | Independência | Lançamentos segue operando com o consolidado fora do ar | Teste de resiliência | PDF |
| RNF-02 | Vazão | 50 req/s no consolidado | k6, por 5 minutos | PDF |
| RNF-03 | Perda de consultas | No máximo 5%, ou seja, pelo menos 47,5 req/s com sucesso | k6 | PDF |
| RNF-04 | Durabilidade | Nenhum lançamento confirmado é perdido | Teste de resiliência e reconciliação | Proposta |
| RNF-05 | Disponibilidade | 99,9% ao mês em Lançamentos, cerca de 43 minutos de parada | SLO | Proposta |
| RNF-06 | Latência de escrita | p95 de até 300 ms | Métricas de produção | Proposta |
| RNF-07 | Latência de leitura | p95 de até 200 ms | k6 | Proposta |
| RNF-08 | Defasagem | p95 de até 5 s em operação normal | Métrica de atraso | Proposta |
| RNF-09 | Recuperação | RTO de 1 hora e RPO de 5 minutos em desastre regional | Exercício de recuperação | Proposta |
| RNF-10 | Rastreabilidade | Um lançamento pode ser seguido até o saldo | Traço distribuído | Proposta |

## Requisitos de dados, segurança e conformidade

| ID | Requisito |
|---|---|
| RD-01 | Dados de lançamentos são retidos pelo prazo definido pela área de conformidade |
| RD-02 | Dados ficam no Brasil, em `southamerica-east1` |
| RS-01 | Todo acesso às APIs exige autenticação e escopo adequado |
| RS-02 | Dados são criptografados em trânsito e em repouso |
| RS-03 | Acessos e operações são auditáveis |
| RC-01 | O tratamento de dados pessoais segue a LGPD, quando o comerciante for pessoa física |
| RC-02 | A contratação de nuvem segue as regras aplicáveis a instituições financeiras, como a Resolução CMN 4.893 |

## Cenários de atributos de qualidade

| ID | Atributo | Fonte e estímulo | Ambiente | Resposta | Medida |
|---|---|---|---|---|---|
| Q-01 | Disponibilidade | O consolidado para | Operação normal | Lançamentos aceita escritas e os eventos ficam retidos | 100% de escritas válidas aceitas |
| Q-02 | Recuperação | O consolidado volta | Fila acumulada | Os eventos são processados | Saldo correto e fila vazia |
| Q-03 | Disponibilidade | A mensageria para | Operação normal | Lançamentos aceita escritas e os eventos ficam na outbox | Nenhum evento perdido |
| Q-04 | Desempenho | 50 consultas por segundo | Pico | O consolidado responde | Menos de 5% de falha e p95 de até 200 ms |
| Q-05 | Integridade | Um evento é entregue duas vezes | Operação normal | O saldo não muda na segunda entrega | Saldo igual à soma dos lançamentos |
| Q-06 | Integridade | Eventos chegam fora de ordem | Operação normal | O saldo final fica correto | Saldo igual à soma dos lançamentos |
| Q-07 | Segurança | Um comerciante pede dados de outro | Operação normal | O acesso é negado | Resposta 404 e registro de auditoria |
| Q-08 | Modificabilidade | Um novo consumidor precisa dos eventos | Evolução | Ele assina o tópico | Nenhuma mudança em Lançamentos |

## Fora de escopo

- Interface web e exportação de relatórios
- Múltiplas moedas e contas por comerciante
- Integração bancária e conciliação
- Deploy real em nuvem
