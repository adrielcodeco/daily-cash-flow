# ADR-0009: Observabilidade

- **Status:** aceita
- **Data:** 2026-11-05

## Contexto

O sistema é assíncrono, então uma falha pode acontecer longe da requisição que a causou. O rastreamento precisa acompanhar o lançamento até o saldo.

## Decisão

Instrumentar com OpenTelemetry. Localmente, usar Prometheus e Grafana. No GCP, exportar para o Google Cloud Observability.

## Opções

<!-- radar:start -->
![Radar de Observabilidade](radar/ADR-0009.svg)

| Critério | Peso | **OpenTelemetry** | SDK do provedor de nuvem | APM comercial |
|---|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 4 | 4 |
| Operabilidade | 3 | 4 | 4 | 4 |
| Economia | 2 | 5 | 5 | 2 |
| Portabilidade | 2 | 5 | 1 | 1 |
| Simplicidade | 2 | 4 | 4 | 3 |
| Segurança | 2 | 4 | 4 | 2 |
| **Resultado** | | **4,53** | **3,73** | **2,93** |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0009).
<!-- radar:end -->

## Leitura do radar

O SDK do provedor é integrado e empata em operabilidade e simplicidade, mas prende a instrumentação ao GCP e não funciona igual no ambiente local. O APM comercial é forte em experiência, com custo maior. OpenTelemetry mantém o mesmo código nos dois ambientes.

## Consequências

**Positivas**

- A mesma instrumentação serve no local e na nuvem
- O backend pode mudar sem alterar o código

**Negativas**

- Exige mais configuração inicial
- O coletor é mais um componente para operar

## Resultado

Implementado. `smoke.sh` prova que um traço liga a API, o publicador e o consumidor, e que a queda do coletor não afeta a escrita. O custo medido da telemetria está nos [resultados de testes](../09-resultados-de-testes.md).

## Gatilho de revisão

Rever se o cliente tiver um APM corporativo. Nesse caso, basta trocar o destino do coletor.
