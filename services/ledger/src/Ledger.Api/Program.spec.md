<!-- @anchors
  code: LDHTP
  updated_at: 2026-11-09
-->

# Program — as respostas de erro da API de Lançamentos

> **Código**: `LDHTP`

## Visão Geral

Quem integra com a API trata os erros por um único formato. Esta unidade monta a API e faz toda recusa responder no formato de problema, com o status, um título e, quando há, o código do erro.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `failure` | recusa de uma regra de domínio, ou pedido malformado | — | o domínio e a leitura do pedido |
| `status` | o status da recusa | — | esta unidade (`LDHTP-R01`, `LDHTP-R02`) |
| `title` | a mensagem da recusa | — | esta unidade (`LDHTP-R01`, `LDHTP-R02`) |

## Regras

### LDHTP-R01 — a recusa de uma regra de domínio responde no formato de problema, com o código da regra

O status segue o tipo da recusa: 400 para um pedido inválido, 404 para o que não existe, 409 para um conflito e 422 para uma regra do lançamento.

### LDHTP-R02 — a recusa sem corpo próprio também responde no formato de problema

Vale para o pedido malformado, para o token ausente e para o escopo ausente.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `LDHTP-R01` | `failure`, `status`, `title` |
| `LDHTP-R02` | `status` |

## Decisões em aberto

nenhuma
