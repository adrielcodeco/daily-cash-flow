<!-- @anchors
  code: GTBAL
  updated_at: 2026-11-05
-->

# GetDailyBalance — a consulta do saldo de um dia

> **Código**: `GTBAL`

## Visão Geral

O comerciante consulta o saldo sem depender do serviço de lançamentos. A resposta diz até quando os dados estão atualizados, porque o saldo pode estar alguns segundos atrás.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `merchantId` | comerciante da requisição | — | a API |
| `date` | qualquer data válida | data inválida | a API |

## Regras

### GTBAL-R01 — a consulta devolve apenas dados do comerciante da requisição @realizes MRISO-R02

### GTBAL-R02 — a abertura é o acumulado de todos os dias anteriores

### GTBAL-R03 — a resposta informa o momento do último evento aplicado

### GTBAL-R04 — um comerciante sem movimento recebe saldo zerado e sem momento de atualização

### GTBAL-R05 — um saldo em cache é devolvido sem consultar o banco

### GTBAL-R06 — sem cache disponível, a consulta vai ao banco

O cache melhora a latência, mas nunca derruba uma consulta.

### GTBAL-R07 — um lançamento retroativo muda a abertura dos dias seguintes

## Uso das regras

| Regra | Usa |
| --- | --- |
| `GTBAL-R01` | `merchantId` |
| `GTBAL-R02` | `date` |
| `GTBAL-R03` | `merchantId`, `date` |
| `GTBAL-R04` | `merchantId`, `date` |
| `GTBAL-R05` | `merchantId`, `date` |
| `GTBAL-R06` | `merchantId`, `date` |
| `GTBAL-R07` | `date` |

## Decisões em aberto

nenhuma
