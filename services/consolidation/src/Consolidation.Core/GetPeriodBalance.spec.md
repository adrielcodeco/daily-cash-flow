<!-- @anchors
  code: GTPRD
  updated_at: 2026-11-05
-->

# GetPeriodBalance — o saldo de cada dia de um período

> **Código**: `GTPRD`

## Visão Geral

O comerciante acompanha a evolução do caixa ao longo de vários dias. Esta unidade monta um saldo para cada dia, e o fechamento de um dia é a abertura do seguinte.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `merchantId` | comerciante da requisição | — | a API |
| `start` | data válida | depois de `end` | esta unidade (`GTPRD-R01`) |
| `end` | data válida | mais de 366 dias depois de `start` | esta unidade (`GTPRD-R02`) |

## Regras

### GTPRD-R01 — o início não pode ser depois do fim

### GTPRD-R02 — o período tem no máximo 366 dias

### GTPRD-R03 — todo dia do período aparece, mesmo sem movimento

### GTPRD-R04 — o fechamento de um dia é a abertura do seguinte

### GTPRD-R05 — a consulta devolve apenas dados do comerciante da requisição @realizes MRISO-R02

## Uso das regras

| Regra | Usa |
| --- | --- |
| `GTPRD-R01` | `start`, `end` |
| `GTPRD-R02` | `start`, `end` |
| `GTPRD-R03` | `start`, `end` |
| `GTPRD-R04` | `start` |
| `GTPRD-R05` | `merchantId` |

## Decisões em aberto

nenhuma
