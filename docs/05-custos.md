# Estimativa de custos

Estimativa mensal em dólares, para `southamerica-east1`. Os preços foram lidos em 06/11/2026 nas páginas oficiais de preços do Google Cloud, com a região São Paulo selecionada onde a página permite. Antes de uma decisão real, confirme tudo na calculadora oficial ou na conta de faturamento do cliente, porque contratos corporativos costumam ter condições próprias.

O cálculo usa 730 horas por mês, preços de lista e nenhum desconto por compromisso. Impostos, suporte do GCP e custo da equipe ficam de fora.

## Cenários

| Premissa | Início | Crescimento |
|---|---:|---:|
| Pico de leituras | 50 req/s, como no PDF | 500 req/s |
| Média de requisições | 5 req/s | 50 req/s |
| Requisições por mês | 13 milhões | 130 milhões |

Esses volumes são premissas minhas. O PDF só informa o pico de 50 req/s.

## Início

| Item | Dimensionamento | US$ por mês |
|---|---|---:|
| Cloud Run | 6 instâncias sempre ativas de 1 vCPU e 0,5 GiB | 359,51 |
| Cloud SQL lançamentos | HA, 2 vCPU, 8 GiB, 50 GiB SSD e 50 GiB de backup | 335,03 |
| Cloud SQL consolidado | HA, 2 vCPU, 8 GiB, 50 GiB SSD e 50 GiB de backup | 335,03 |
| Memorystore for Redis | Basic, 1 GiB | 63,51 |
| Pub/Sub | 10 GiB por mês, sem a cota gratuita | 0,39 |
| Balanceador | Regras de encaminhamento e 20 GiB | 28,22 |
| Cloud Armor Standard | 1 política, 5 regras e 13 milhões de requisições | 19,75 |
| API Gateway | 13 milhões de chamadas, 2 milhões grátis | 33,00 |
| Cloud NAT e Private Service Connect | Gateway NAT e um endpoint | 40,15 |
| Cloud Logging | 30 GiB, 50 GiB grátis | 0,00 |
| Cloud Monitoring | 300 MiB, 150 MiB grátis | 38,70 |
| Cloud Trace | 7 milhões de spans, 2,5 milhões grátis | 0,90 |
| **Total** | | **1.254,20** |

## Crescimento

| Item | Dimensionamento | US$ por mês |
|---|---|---:|
| Cloud Run | 13 instâncias sempre ativas de 1 vCPU e 0,5 GiB | 778,94 |
| Cloud SQL lançamentos | HA, 4 vCPU, 16 GiB, 500 GiB SSD e 500 GiB de backup | 922,07 |
| Cloud SQL consolidado | HA, 4 vCPU, 16 GiB, 200 GiB SSD e 200 GiB de backup | 733,07 |
| Memorystore for Redis | Standard, 5 GiB | 197,10 |
| Pub/Sub | 100 GiB por mês, sem a cota gratuita | 3,91 |
| Balanceador | Regras de encaminhamento e 200 GiB | 32,54 |
| Cloud Armor Standard | 1 política, 5 regras e 130 milhões de requisições | 107,50 |
| API Gateway | 130 milhões de chamadas, 2 milhões grátis | 384,00 |
| Cloud NAT e Private Service Connect | Gateway NAT e um endpoint | 40,15 |
| Cloud Logging | 300 GiB, 50 GiB grátis | 125,00 |
| Cloud Monitoring | 1000 MiB, 150 MiB grátis | 219,30 |
| Cloud Trace | 65 milhões de spans, 2,5 milhões grátis | 12,50 |
| **Total** | | **3.556,07** |

## Preços usados

| Serviço | Item | Preço | Região do valor |
|---|---|---:|---|
| Cloud Run | vCPU-segundo, faturamento por instância | US$ 0,0000216 | São Paulo |
| Cloud Run | GiB-segundo, faturamento por instância | US$ 0,0000024 | São Paulo |
| Cloud SQL Enterprise | vCPU por hora, com HA | US$ 0,1239 | São Paulo |
| Cloud SQL Enterprise | GiB de memória por hora, com HA | US$ 0,021 | São Paulo |
| Cloud SQL | GiB de SSD por hora, com HA | US$ 0,00069863 | São Paulo |
| Cloud SQL | GiB de backup por hora | US$ 0,000164384 | São Paulo |
| Memorystore | GiB por hora, Basic M1 | US$ 0,087 | São Paulo |
| Memorystore | GiB por hora, Standard M2 | US$ 0,054 | São Paulo |
| Balanceador | Até cinco regras, por hora | US$ 0,038 | São Paulo |
| Balanceador | GiB de entrada ou saída | US$ 0,012 | São Paulo |
| Pub/Sub | TiB | US$ 40 | Todas |
| Cloud Armor | Política por hora | US$ 0,006849315 | Global |
| Cloud Armor | Regra por hora | US$ 0,001369863 | Global |
| Cloud Armor | Milhão de requisições, política global | US$ 0,75 | Global |
| API Gateway | Milhão de chamadas, acima de 2 milhões | US$ 3,00 | Global |
| Cloud NAT | Gateway por hora, valor usado como teto | US$ 0,045 | Tabela genérica |
| Private Service Connect | Endpoint por hora | US$ 0,01 | Tabela genérica |
| Cloud Logging | GiB, acima de 50 GiB | US$ 0,50 | Global |
| Cloud Monitoring | MiB, acima de 150 MiB | US$ 0,258 | Global |
| Cloud Trace | Milhão de spans, acima de 2,5 milhões | US$ 0,20 | Global |

Pontos a confirmar na calculadora: o preço do Memorystore Standard apareceu igual ao do Basic na mesma faixa, e o Cloud NAT usa um valor genérico.

## Licenças

| Software | Licença | Custo |
|---|---|---:|
| .NET e ASP.NET Core | MIT | US$ 0 |
| PostgreSQL | PostgreSQL License | US$ 0 |
| Redis | Incluído no Memorystore | No preço do serviço |
| OpenTelemetry, Prometheus e Grafana OSS | Apache 2.0 e AGPL | US$ 0 |
| Keycloak | Apache 2.0, só no ambiente local | US$ 0 |

No alvo, a identidade usa o provedor corporativo do cliente, cujo custo não está aqui.

## Onde economizar

| Medida | Efeito estimado |
|---|---|
| Desconto por compromisso de 1 ano no Cloud SQL | Cerca de 25% sobre vCPU e memória |
| Deixar o cache para depois, já que a carga medida não mostrou ganho de latência | US$ 63,51 por mês no início |
| Banco do consolidado sem HA, porque ele pode ser reconstruído | Cerca de metade do custo dessa instância, com RTO maior |
| Instância mínima de 1 nas APIs fora do pico | Reduz o Cloud Run, com menos folga contra falhas |

O maior custo é o Cloud SQL com alta disponibilidade, que corresponde a 53% do cenário Início. Ele protege a fonte da verdade, por isso a primeira economia deve vir do consolidado, e não do banco de lançamentos.

## Fora desta estimativa

- Réplica em outra região para recuperação de desastres
- Ambientes de desenvolvimento e homologação
- Cloud Interconnect ou VPN com o cliente
- Custo de operação da equipe
