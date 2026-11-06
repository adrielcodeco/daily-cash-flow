<!-- @anchors
  code: LEDEN
  updated_at: 2026-11-05
-->

# LedgerEntry — o registro imutável de um movimento do caixa

> **Código**: `LEDEN`

## Visão Geral

O lançamento é o fato central do sistema: um crédito ou um débito de um comerciante, em uma data de competência. Esta unidade decide o que é um lançamento válido.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `merchantId` | identificador não vazio | identificador vazio | esta unidade (`LEDEN-R01`) |
| `amountCents` | inteiro maior que zero | zero ou negativo | esta unidade (`LEDEN-R02`) |
| `accountingDate` | hoje ou uma data passada | data futura | esta unidade (`LEDEN-R03`) |
| `description` | até 200 caracteres, ou ausente | texto maior | esta unidade (`LEDEN-R04`) |

## Regras

### LEDEN-R01 — o lançamento pertence a um comerciante @realizes MRISO-R01

### LEDEN-R02 — o valor é um inteiro de centavos maior que zero @realizes LDIMM-R02

### LEDEN-R03 — a data de competência não pode estar no futuro, no fuso de São Paulo

Às 22h de um dia em São Paulo já é o dia seguinte em UTC. A data válida é a do fuso do negócio.

### LEDEN-R04 — a descrição tem no máximo 200 caracteres

### LEDEN-R05 — um lançamento válido nasce com identidade e momento de criação @realizes LDIMM-R01

## Uso das regras

| Regra | Usa |
| --- | --- |
| `LEDEN-R01` | `merchantId` |
| `LEDEN-R02` | `amountCents` |
| `LEDEN-R03` | `accountingDate` |
| `LEDEN-R04` | `description` |
| `LEDEN-R05` | `merchantId`, `amountCents`, `accountingDate` |

## Decisões em aberto

nenhuma
