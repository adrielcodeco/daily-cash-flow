# ADR-0002: Integração entre serviços

- **Status:** aceita
- **Data:** 2026-11-05

## Contexto

O consolidado precisa saber de cada lançamento. Gravar no banco e publicar na mensageria são duas operações sem transação comum, e uma falha entre elas pode perder um evento ou publicar um fato que não existe.

## Decisão

Usar transactional outbox. O lançamento e o evento são gravados na mesma transação, e um publicador envia o evento depois. A entrega é at-least-once, e o consumidor é idempotente.

## Opções

<!-- radar:start -->
![Radar de Integração entre serviços](radar/ADR-0002.svg)

| Critério | Peso | **Outbox na transação e mensageria** | CDC do banco | Publicar na mensageria durante a requisição | HTTP síncrono entre os serviços |
|---|---:|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 3 | 5 | Veto |
| Aderência ao problema | 3 | 5 | 4 | Veto | Veto |
| Confiabilidade | 3 | 5 | 3 | 1 | Veto |
| Operabilidade | 3 | 4 | 3 | 4 | 5 |
| Desempenho | 2 | 5 | 5 | 4 | 2 |
| Escalabilidade | 2 | 5 | 3 | 5 | 3 |
| Simplicidade | 2 | 2 | 3 | 4 | 5 |
| Economia | 1 | 3 | 3 | 3 | 4 |
| **Resultado** | | **4,45** | **3,35** | Eliminada | Eliminada |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0002).
<!-- radar:end -->

## Leitura do radar

HTTP síncrono é a opção mais simples, mas cria a dependência que o requisito proíbe, por isso é eliminada por veto. CDC também evita a escrita dupla, mas acrescenta um conector, mais operação e expõe o esquema interno do banco.

Publicar direto na mensageria durante a requisição foi descartado: a queda do broker impediria os lançamentos.

## Consequências

**Positivas**

- Nenhum lançamento confirmado perde seu evento
- Lançamentos aceita escrita com o broker ou o consolidado fora do ar

**Negativas**

- Eventos podem chegar repetidos ou fora de ordem
- A outbox precisa de publicador, limpeza e monitoramento

## Gatilho de revisão

Rever se o volume tornar a leitura da outbox um gargalo. CDC passa a ser a alternativa natural.
