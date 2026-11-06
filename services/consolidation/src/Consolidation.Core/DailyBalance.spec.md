<!-- @anchors
  code: DYBAL
  updated_at: 2026-11-06
-->

# DailyBalance — o saldo de um dia

> **Código**: `DYBAL`

## Visão Geral

O comerciante decide olhando o saldo do dia. Esta unidade define como abertura, créditos e débitos formam o fechamento.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `OpeningCents` | inteiro, inclusive negativo | — | o tipo |
| `CreditsCents` | inteiro não negativo | — | `APENT` |
| `DebitsCents` | inteiro não negativo | — | `APENT` |

## Regras

### DYBAL-R01 — o fechamento é a abertura mais os créditos menos os débitos @realizes LDIMM-R02

### DYBAL-R02 — um dia sem movimento mantém a abertura como fechamento

### DYBAL-R03 — um estouro no fechamento gera erro, e nunca um saldo errado

## Uso das regras

| Regra | Usa |
| --- | --- |
| `DYBAL-R01` | `OpeningCents`, `CreditsCents`, `DebitsCents` |
| `DYBAL-R02` | `OpeningCents` |
| `DYBAL-R03` | `OpeningCents`, `CreditsCents`, `DebitsCents` |

## Decisões em aberto

nenhuma
