<!-- @anchors
  code: RSBAL
  updated_at: 2026-11-05
-->

# ResetBalances — limpar o consolidado antes de reconstruir

> **Código**: `RSBAL`

## Visão Geral

O consolidado é derivado dos lançamentos, então pode ser apagado e refeito. Esta unidade limpa o saldo e o registro de eventos processados para que os eventos reenviados sejam aplicados de novo.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `store` | armazenamento do consolidado | — | o tipo |

## Regras

### RSBAL-R01 — depois da limpeza, nenhum saldo nem evento processado permanece

### RSBAL-R02 — os eventos reenviados recompõem o mesmo saldo

## Uso das regras

| Regra | Usa |
| --- | --- |
| `RSBAL-R01` | `store` |
| `RSBAL-R02` | `store` |

## Decisões em aberto

nenhuma
