<!-- @anchors
  code: LDEPS
  updated_at: 2026-11-09
-->

# EntryEndpoints — as operações HTTP de lançamentos

> **Código**: `LDEPS`

## Visão Geral

O comerciante registra, estorna, lista e consulta lançamentos por quatro operações HTTP. Esta unidade confere o que o pedido traz antes de entregá-lo ao domínio, e responde como inexistente o que não é do comerciante.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `key` | Idempotency-Key de 8 a 128 caracteres | ausente, menor ou maior | esta unidade (`LDEPS-R01`) |
| `request` | lançamento com o tipo pelo nome | o tipo em número | esta unidade (`LDEPS-R02`) |
| `id` | lançamento do comerciante do token | lançamento de outro comerciante | esta unidade (`LDEPS-R03`) |
| `merchantId` | comerciante lido do token | comerciante que não pode ser identificado | `LDMRC` |

## Regras

### LDEPS-R01 — a escrita exige uma Idempotency-Key de 8 a 128 caracteres

Registrar e estornar sem a chave, ou com uma chave fora do tamanho, é recusado com 400 e o código `invalid_idempotency_key`, e nada é gravado. Os limites, 8 e 128, são aceitos.

### LDEPS-R02 — o tipo do lançamento enviado como número é recusado com 400

O tipo chega por nome. Um número, conhecido ou não, é recusado na leitura do pedido, e nada é gravado.

### LDEPS-R03 — o lançamento de outro comerciante responde como inexistente @realizes MRISO-R02

Consultar ou estornar o lançamento de outro comerciante responde 404. A lista de um comerciante não mostra lançamentos de outro.

### LDEPS-R04 — um comerciante que não pode ser identificado é recusado com 401

Vale para as quatro operações. Nada é gravado nem consultado.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `LDEPS-R01` | `key` |
| `LDEPS-R02` | `request` |
| `LDEPS-R03` | `id`, `merchantId` |
| `LDEPS-R04` | `merchantId` |

## Decisões em aberto

nenhuma
