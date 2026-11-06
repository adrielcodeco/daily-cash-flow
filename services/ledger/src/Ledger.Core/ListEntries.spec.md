<!-- @anchors
  code: LSTEN
  updated_at: 2026-11-06
-->

# ListEntries — os lançamentos de um dia, em páginas

> **Código**: `LSTEN`

## Visão Geral

O comerciante confere o que foi lançado em um dia. Um dia pode ter muitos lançamentos, então a lista vem em páginas estáveis, mesmo com novos registros chegando.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `merchantId` | comerciante da requisição | — | a API |
| `date` | data válida | data inválida | a API |
| `cursor` | posição devolvida na página anterior, ou ausente | valor inválido | a API |
| `limit` | de 1 a 200 | fora da faixa | esta unidade (`LSTEN-R02`) |

## Regras

### LSTEN-R01 — a lista traz apenas lançamentos do comerciante, na data pedida @realizes MRISO-R02

### LSTEN-R02 — o limite vai de 1 a 200

### LSTEN-R03 — a página informa a próxima posição só quando há mais lançamentos

### LSTEN-R04 — o banco só devolve linhas do comerciante definido na transação @realizes MRISO-R03

## Uso das regras

| Regra | Usa |
| --- | --- |
| `LSTEN-R01` | `merchantId`, `date` |
| `LSTEN-R02` | `limit` |
| `LSTEN-R03` | `cursor`, `limit` |
| `LSTEN-R04` | `merchantId` |

## Decisões em aberto

nenhuma
