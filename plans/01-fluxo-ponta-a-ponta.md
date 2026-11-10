<!-- @anchors
  code: FPAPL
  updated_at: 2026-11-09
-->

# Plano 01 — Fluxo de ponta a ponta

> **Código**: `FPAPL`

## Objetivo

Um lançamento criado pela API aparece no saldo do dia, com todo o sistema subindo por um comando.

## Motivo

O fluxo completo valida a decisão central da arquitetura antes de qualquer refinamento. O escopo foi aprovado antes do início da implementação.

## Doutrina de produto semeada

- `product/ledger-immutability.doctrine.md` — fato financeiro não muda
- `product/merchant-isolation.doctrine.md` — o dado de um comerciante nunca alcança outro
- `product/write-independence.doctrine.md` — nada fora de Lançamentos impede uma escrita

## Fases

### FPAPL-W01 — Lançamentos

- `services/ledger/src/Ledger.Core/LedgerEntry.spec.md` — **nasce**: o lançamento e suas regras
- `services/ledger/src/Ledger.Core/RecordEntry.spec.md` — **nasce**: o registro atômico do lançamento e do evento
- `services/ledger/src/Ledger.Core/OutboxRelay.spec.md` — **nasce**: o envio dos eventos pendentes

### FPAPL-W02 — Consolidado

- `services/consolidation/src/Consolidation.Core/DailyBalance.spec.md` — **nasce**: o saldo de um dia
- `services/consolidation/src/Consolidation.Core/ApplyEntryRecorded.spec.md` — **nasce**: a aplicação idempotente do evento
- `services/consolidation/src/Consolidation.Core/GetDailyBalance.spec.md` — **nasce**: a consulta do saldo

## Fora de escopo

Estorno, idempotência da API, cache, reconstrução, autenticação real e telemetria ficam para as etapas seguintes.

## Definição de pronto

- Um comando sobe o ambiente local
- Um lançamento registrado aparece no saldo do dia
- Com o consolidado parado, lançamentos continua aceitando escrita, e o saldo fecha certo quando ele volta
