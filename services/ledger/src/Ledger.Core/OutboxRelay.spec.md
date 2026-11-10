<!-- @anchors
  code: OBRLY
  updated_at: 2026-11-09
-->

# OutboxRelay — o envio dos eventos pendentes

> **Código**: `OBRLY`

## Visão Geral

Os eventos ficam pendentes até a mensageria confirmar o recebimento. Esta unidade envia um lote e marca o que foi confirmado. Uma falha nunca perde um evento, mesmo que isso provoque um envio repetido.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `Messages` | até um lote de eventos pendentes | — | o armazenamento |
| `publisher` | mensageria que confirma o recebimento | — | o tipo |

## Regras

### OBRLY-R01 — um evento só é marcado como publicado depois da confirmação da mensageria

### OBRLY-R02 — ao falhar, o lote para e o restante continua pendente

Parar evita insistir contra uma mensageria fora do ar. A próxima rodada retoma do que ficou pendente.

### OBRLY-R03 — o que foi confirmado antes da falha continua marcado

Vale também quando o publicador é encerrado no meio do lote. Desmarcar o que já foi confirmado só causaria envios repetidos.

### OBRLY-R04 — o evento que falhou espera mais a cada falha antes de nova tentativa

Sem isso, um evento que a mensageria sempre recusa voltaria ao início de cada lote e travaria os que vêm depois. A espera dobra a cada falha, até um minuto. A ordem entre eventos não importa, porque o consolidado soma valores.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `OBRLY-R01` | `Messages`, `publisher` |
| `OBRLY-R02` | `Messages`, `publisher` |
| `OBRLY-R03` | `Messages`, `publisher` |
| `OBRLY-R04` | `Messages`, `publisher` |

## Decisões em aberto

nenhuma
