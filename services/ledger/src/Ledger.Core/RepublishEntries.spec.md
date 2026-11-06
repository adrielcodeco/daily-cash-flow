<!-- @anchors
  code: RPENT
  updated_at: 2026-11-05
-->

# RepublishEntries — o reenvio dos fatos para reconstruir o consolidado

> **Código**: `RPENT`

## Visão Geral

Lançamentos é a fonte da verdade. Quando o consolidado precisa ser refeito, esta unidade coloca o evento de cada lançamento de volta na fila de envio. A reconstrução usa o mesmo caminho da operação normal, sem que um serviço leia o banco do outro.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `batchSize` | inteiro maior que zero | — | quem chama |

## Regras

### RPENT-R01 — todo lançamento volta a ter seu evento pendente

### RPENT-R02 — o evento reenviado mantém a identidade original

Assim, uma mensagem antiga ainda em trânsito é tratada como repetição.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `RPENT-R01` | `batchSize` |
| `RPENT-R02` | `batchSize` |

## Decisões em aberto

nenhuma
