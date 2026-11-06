# Monitoramento e observabilidade

A instrumentação usa OpenTelemetry, conforme o [ADR-0009](adr/0009-observabilidade.md). Telemetria, dashboards e alertas locais entram em código no marco M6.

## SLOs

| SLI | SLO proposto | Janela | Orçamento de erro |
|---|---|---|---|
| Disponibilidade de `POST /v1/entries` | 99,9% | 30 dias | Cerca de 43 minutos |
| Latência de escrita p95 | Até 300 ms | 30 dias | 5% das requisições |
| Sucesso de consultas de saldo | 99,5% | 30 dias | 0,5% das requisições. O PDF tolera 5% no pico |
| Latência de leitura p95 | Até 200 ms | 30 dias | 5% das requisições |
| Defasagem do saldo p95 | Até 5 s | 30 dias | 5% dos eventos |

Se o orçamento de erro acabar, mudanças de funcionalidade param até a confiabilidade voltar à meta.

## Métricas

| Sinal | Métrica | Por quê |
|---|---|---|
| Taxa, erros e latência | Requisições por rota, status e duração | Experiência do comerciante |
| Outbox | Eventos pendentes e idade do mais antigo | Mostra falha no publicador ou na mensageria |
| Fila | Mensagens não confirmadas e idade da mais antiga | Mostra consumidor parado ou lento |
| Mensagens mortas | Quantidade no tópico de mensagens mortas | Toda mensagem ali é um valor fora do saldo |
| Defasagem | Tempo entre `occurredAt` e a aplicação ao saldo | Mede o atraso que o comerciante percebe |
| Banco | Conexões em uso, CPU, disco e replicação | O teste de resiliência mostrou o risco de esgotar conexões |
| Cache | Acertos, erros e estado da conexão | Mostra degradação silenciosa |
| Reconciliação | Diferença entre lançamentos e saldo | Detecta divergência |

## Rastreamento

O contexto do traço segue no atributo da mensagem. Assim, um `POST /v1/entries` fica ligado ao publicador, ao consumidor e à atualização do saldo.

Os logs são estruturados e levam `traceId` e `entryId`. Eles não levam descrição do lançamento nem token.

## Alertas

| Alerta | Condição | Severidade | Ação |
|---|---|---|---|
| Escritas falhando | Erro 5xx acima de 1% por 5 minutos | Crítica | [Banco indisponível](12-operacao.md#banco-indisponível) |
| Consumo do orçamento | Queima do orçamento 14 vezes acima do normal em 1 hora | Crítica | Investigar e congelar mudanças |
| Outbox parada | Evento pendente há mais de 2 minutos | Alta | [Outbox parada](12-operacao.md#outbox-parada) |
| Consumidor atrasado | Mensagem mais antiga há mais de 2 minutos | Alta | [Consumidor atrasado](12-operacao.md#consumidor-atrasado) |
| Mensagens mortas | Qualquer mensagem no tópico | Alta | [Mensagens mortas](12-operacao.md#mensagens-mortas) |
| Conexões do banco | Uso acima de 80% por 5 minutos | Média | Revisar pools e instâncias |
| Cache fora | Sem conexão por 5 minutos | Baixa | Verificar o Redis. A API segue pelo banco |

## Ferramentas

| Função | Local | GCP |
|---|---|---|
| Coleta | OpenTelemetry Collector | OpenTelemetry Collector |
| Métricas | Prometheus | Cloud Monitoring |
| Traços | Tempo ou Jaeger | Cloud Trace |
| Logs | Saída do contêiner | Cloud Logging |
| Dashboards | Grafana | Cloud Monitoring |

## Capacidade

Medições locais estão nos [resultados de testes](09-resultados-de-testes.md). Com 50 leituras e 10 escritas por segundo, o p95 ficou abaixo de 12 ms, com folga sobre as metas.

O limite mais provável são as conexões do banco, então cada processo precisa de um pool definido:

| Processo | Instâncias máximas | Pool por instância | Conexões |
|---|---:|---:|---:|
| `ledger-api` | 10 | 10 | 100 |
| `outbox-publisher` | 2 | 5 | 10 |
| **Banco de lançamentos** | | | **110** |
| `consolidation-api` | 10 | 10 | 100 |
| `consolidation-consumer` | 5 | 16 | 80 |
| **Banco do consolidado** | | | **180** |

A soma precisa ficar abaixo de `max_connections`, com margem para manutenção. Se o número de instâncias crescer, a alternativa é um pooler de conexões.
