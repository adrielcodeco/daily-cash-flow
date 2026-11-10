<!-- @anchors
  code: LDMRC
  updated_at: 2026-11-09
-->

# MerchantContext — o comerciante do pedido vem do token

> **Código**: `LDMRC`

## Visão Geral

Toda operação da API de Lançamentos age em nome de um comerciante. Esta unidade lê esse comerciante do token já validado e nunca de algo que o cliente escolhe.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `merchantId` | identificador de comerciante diferente de zero, declarado no token | ausente, que não é identificador, vazio ou o identificador zerado | esta unidade (`LDMRC-R02`) |

## Regras

### LDMRC-R01 — o comerciante é lido do token @realizes MRISO-R02

### LDMRC-R02 — um comerciante que não é um identificador válido não é lido

Vale para o token sem o comerciante, para um valor que não é identificador, para o valor vazio e para o identificador zerado. Quem chama recebe a recusa, e não um comerciante padrão.

### LDMRC-R03 — um comerciante escolhido pelo cliente é ignorado

Um cabeçalho ou um parâmetro com o comerciante não substitui o token.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `LDMRC-R01` | `merchantId` |
| `LDMRC-R02` | `merchantId` |
| `LDMRC-R03` | `merchantId` |

## Decisões em aberto

nenhuma
