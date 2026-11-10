<!-- @anchors
  code: CNHTP
  updated_at: 2026-11-09
-->

# Program — as consultas HTTP de saldo

> **Código**: `CNHTP`

## Visão Geral

O comerciante consulta o saldo de um dia ou de um período por duas operações HTTP. Esta unidade monta a API, lê o comerciante do token e faz toda recusa responder no formato de problema, com o status, um título e, quando há, o código do erro.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `merchantId` | identificador de comerciante diferente de zero, declarado no token | que não é identificador, vazio ou o identificador zerado | esta unidade (`CNHTP-R01`) |
| `failure` | recusa da consulta, ou pedido malformado | — | `GTPRD` e a leitura do pedido |
| `status` | o status da recusa | — | esta unidade (`CNHTP-R03`, `CNHTP-R04`) |
| `title` | a mensagem da recusa | — | esta unidade (`CNHTP-R03`, `CNHTP-R04`) |

## Regras

### CNHTP-R01 — um comerciante que não pode ser identificado é recusado com 401

Vale para um valor que não é identificador, para o valor vazio e para o identificador zerado. Nenhuma consulta chega ao armazenamento.

### CNHTP-R02 — o saldo é sempre o do comerciante do token @realizes MRISO-R02

Um comerciante nunca vê o movimento de outro, mesmo na mesma data.

### CNHTP-R03 — a recusa da consulta responde no formato de problema, com o código

Vale para o período invertido e para o período longo demais.

### CNHTP-R04 — a recusa sem corpo próprio também responde no formato de problema

Vale para a data ausente ou malformada, para o token ausente e para o escopo ausente.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `CNHTP-R01` | `merchantId` |
| `CNHTP-R02` | `merchantId` |
| `CNHTP-R03` | `failure`, `status`, `title` |
| `CNHTP-R04` | `status` |

## Decisões em aberto

nenhuma
