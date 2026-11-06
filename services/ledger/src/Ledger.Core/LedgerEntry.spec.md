<!-- @anchors
  code: LEDEN
  updated_at: 2026-11-06
-->

# LedgerEntry — o registro imutável de um movimento do caixa

> **Código**: `LEDEN`

## Visão Geral

O lançamento é o fato central do sistema: um crédito ou um débito de um comerciante, em uma data de competência. Esta unidade decide o que é um lançamento válido.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `merchantId` | identificador não vazio | identificador vazio | esta unidade (`LEDEN-R01`) |
| `entryType` | crédito ou débito | valor ausente ou desconhecido | esta unidade (`LEDEN-R09`) |
| `amountCents` | inteiro maior que zero, até 100.000.000.000 | zero, negativo ou acima do limite | esta unidade (`LEDEN-R02`, `LEDEN-R08`) |
| `accountingDate` | hoje ou uma data passada | data futura | esta unidade (`LEDEN-R03`) |
| `description` | até 200 caracteres, ou ausente | texto maior | esta unidade (`LEDEN-R04`) |
| `original` | lançamento existente do comerciante | estorno | esta unidade (`LEDEN-R07`) |

## Regras

### LEDEN-R01 — o lançamento pertence a um comerciante @realizes MRISO-R01

### LEDEN-R02 — o valor é um inteiro de centavos maior que zero @realizes LDIMM-R02

### LEDEN-R03 — a data de competência não pode estar no futuro, no fuso de São Paulo

Às 22h de um dia em São Paulo já é o dia seguinte em UTC. A data válida é a do fuso do negócio.

### LEDEN-R04 — a descrição tem no máximo 200 caracteres

### LEDEN-R05 — um lançamento válido nasce com identidade e momento de criação @realizes LDIMM-R01

### LEDEN-R06 — o estorno tem o tipo inverso, o mesmo valor e a mesma data de competência do original @realizes LDIMM-R01

### LEDEN-R07 — um estorno não pode ser estornado

### LEDEN-R08 — o valor de um lançamento não passa de R$ 1 bilhão

O limite impede que poucos lançamentos estourem a soma do saldo. O banco repete a regra, como última defesa.

### LEDEN-R09 — o tipo é crédito ou débito, sem valor padrão

Tratar um tipo ausente como crédito lançaria uma saída como entrada. O tipo chega por nome, e número não é aceito. O armazenamento recusa um tipo desconhecido, em vez de adivinhar.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `LEDEN-R01` | `merchantId` |
| `LEDEN-R02` | `amountCents` |
| `LEDEN-R03` | `accountingDate` |
| `LEDEN-R04` | `description` |
| `LEDEN-R05` | `merchantId`, `amountCents`, `accountingDate` |
| `LEDEN-R06` | `original` |
| `LEDEN-R07` | `original` |
| `LEDEN-R08` | `amountCents` |
| `LEDEN-R09` | `entryType` |

## Decisões em aberto

nenhuma
