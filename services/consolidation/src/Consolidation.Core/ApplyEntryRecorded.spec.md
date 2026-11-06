<!-- @anchors
  code: APENT
  updated_at: 2026-11-06
-->

# ApplyEntryRecorded — a aplicação idempotente do evento ao saldo

> **Código**: `APENT`

## Visão Geral

A entrega de eventos pode repetir e pode vir fora de ordem. Esta unidade transforma cada evento em uma alteração do agregado do dia, e o armazenamento aplica cada evento uma única vez.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `EventId` | identidade única do evento | — | o contrato do evento |
| `MerchantId` | comerciante dono do lançamento | — | o contrato do evento |
| `EntryType` | crédito ou débito | tipo desconhecido | esta unidade (`APENT-R07`) |
| `AmountCents` | inteiro maior que zero | — | o contrato do evento |

## Regras

### APENT-R01 — um crédito soma nos créditos do dia, e um débito soma nos débitos do dia

### APENT-R02 — um evento repetido não altera o saldo

### APENT-R03 — a ordem de chegada não altera o resultado

Somas não dependem da ordem, por isso o consumidor não precisa ordenar eventos.

### APENT-R04 — o agregado pertence ao comerciante do evento @realizes MRISO-R01

### APENT-R07 — um evento de tipo desconhecido é recusado, e não marcado como aplicado

Ignorar o evento deixaria o saldo diferente dos lançamentos, sem nenhum aviso. Recusado, ele segue para as mensagens mortas, que geram alerta.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `APENT-R01` | `EntryType`, `AmountCents` |
| `APENT-R02` | `EventId` |
| `APENT-R03` | `AmountCents` |
| `APENT-R04` | `MerchantId` |
| `APENT-R07` | `EntryType` |

## Decisões em aberto

nenhuma
