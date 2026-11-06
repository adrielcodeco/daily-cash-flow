<!-- @anchors
  code: RCENT
  updated_at: 2026-11-05
-->

# RecordEntry — o registro atômico do lançamento e do seu evento

> **Código**: `RCENT`

## Visão Geral

Registrar um lançamento precisa deixar o fato e o aviso desse fato juntos. Esta unidade cria o lançamento e entrega os dois ao armazenamento em uma única operação.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `command` | movimento informado pelo comerciante | movimento inválido | `LEDEN` |
| `store` | armazenamento com escrita atômica | — | o tipo |

## Regras

### RCENT-R01 — o lançamento e o evento são gravados juntos ou nenhum é gravado @realizes WRIND-R02

### RCENT-R02 — o registro não depende do consolidado nem da mensageria @realizes WRIND-R01

### RCENT-R03 — o evento leva os dados do lançamento e uma identidade própria

## Uso das regras

| Regra | Usa |
| --- | --- |
| `RCENT-R01` | `command`, `store` |
| `RCENT-R02` | `store` |
| `RCENT-R03` | `command` |

## Decisões em aberto

nenhuma
