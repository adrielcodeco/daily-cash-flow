# ADR-0015: Envio da outbox

- **Status:** aceita
- **Data:** 2026-11-06
- **Registro:** escrito depois da implementação, para documentar uma escolha que já estava no código

## Contexto

Os eventos gravados na outbox precisam chegar à mensageria sem perda e sem exigir componentes novos. O ADR-0002 já escolheu a outbox, e este ADR decide como ela é lida.

## Decisão

Consultar a outbox em lotes, com `FOR UPDATE SKIP LOCKED`, publicar cada evento e marcar `published_at` na mesma transação da leitura. Eventos são publicados em sequência, e a falha interrompe o lote.

## Opções

<!-- radar:start -->
![Radar de Envio da outbox](radar/ADR-0015.svg)

| Critério | Peso | **Consulta periódica com SKIP LOCKED** | LISTEN/NOTIFY com consulta de segurança | CDC |
|---|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 5 | 4 |
| Confiabilidade | 3 | 5 | 5 | 3 |
| Operabilidade | 3 | 5 | 4 | 2 |
| Aderência ao problema | 2 | 5 | 3 | 4 |
| Desempenho | 2 | 5 | 4 | 5 |
| Escalabilidade | 2 | 5 | 4 | 3 |
| Simplicidade | 2 | 5 | 3 | 2 |
| Economia | 1 | 5 | 5 | 3 |
| **Resultado** | | **5,00** | **4,21** | **3,26** |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0015).
<!-- radar:end -->

## Leitura do radar

LISTEN/NOTIFY reduz a espera, mas ainda precisa da consulta para não perder avisos, então acrescenta um caminho sem remover o outro. CDC tem a melhor vazão, mas pede outra plataforma para operar. A consulta periódica tem a menor vazão, que é suficiente: a defasagem medida é de segundos, e o `SKIP LOCKED` permite mais de um publicador.

## Consequências

**Positivas**

- Sem componente novo além do banco e da mensageria
- Falha no meio do lote preserva o que já foi confirmado

**Negativas**

- A transação e a conexão ficam abertas durante a publicação do lote, então um broker lento prende recursos
- A publicação em sequência limita a vazão
- Quando a fila está vazia, a consulta roda a cada 200 ms mesmo sem eventos
- Se o commit falhar depois de publicar, os eventos saem de novo, por isso o consumidor é idempotente

## Gatilho de revisão

Rever se a vazão de escrita passar do que um publicador sequencial entrega. O primeiro passo é publicar o lote em paralelo. Depois, CDC.
