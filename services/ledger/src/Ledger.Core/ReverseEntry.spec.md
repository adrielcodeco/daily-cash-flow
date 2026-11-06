<!-- @anchors
  code: RVENT
  updated_at: 2026-11-05
-->

# ReverseEntry — a correção de um lançamento sem apagar o histórico

> **Código**: `RVENT`

## Visão Geral

Um lançamento errado nunca é alterado. Esta unidade registra o lançamento inverso, com seu evento, e garante que cada lançamento seja estornado uma única vez.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `merchantId` | comerciante da requisição | — | a API |
| `entryId` | identificador de um lançamento | lançamento de outro comerciante | esta unidade (`RVENT-R01`) |
| `key` | chave de 8 a 128 caracteres | chave fora do tamanho | a API |

## Regras

### RVENT-R01 — lançamento inexistente ou de outro comerciante não é encontrado @realizes MRISO-R02

### RVENT-R02 — um lançamento só pode ser estornado uma vez

A garantia vale também para chamadas simultâneas, por isso o armazenamento é quem decide.

### RVENT-R03 — o estorno e seu evento são gravados juntos @realizes WRIND-R02

### RVENT-R04 — a mesma chave devolve o estorno original

## Uso das regras

| Regra | Usa |
| --- | --- |
| `RVENT-R01` | `merchantId`, `entryId` |
| `RVENT-R02` | `entryId` |
| `RVENT-R03` | `entryId` |
| `RVENT-R04` | `key` |

## Decisões em aberto

nenhuma
