<!-- @anchors
  code: CESTR
  updated_at: 2026-11-06
-->

# CachedEntryStore — o cache configurável das consultas

> **Código**: `CESTR`

## Visão Geral

O cache é opcional e fica desligado por padrão. Ele pode ser ligado por rota, ou por tabela, e nesse caso vale para todas as rotas dela. Esta unidade fica na frente do armazenamento, então os casos de uso não sabem se há cache.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `policy` | nomes de rota ou de tabela, separados por vírgula | nome desconhecido, que é ignorado | esta unidade (`CESTR-R01`) |
| `merchantId` | comerciante da requisição | — | a API |

## Regras

### CESTR-R01 — sem alvo ligado, a consulta vai ao armazenamento e não toca no cache

### CESTR-R02 — um alvo de rota liga o cache só naquela rota

### CESTR-R03 — um alvo de tabela liga o cache em todas as rotas dela

### CESTR-R04 — um valor em cache é devolvido sem consultar o armazenamento

### CESTR-R05 — uma falha do cache não derruba a consulta

O cache melhora a latência, mas nunca impede uma resposta.

### CESTR-R06 — um lançamento registrado invalida o cache do comerciante

### CESTR-R07 — uma falha do cache não impede o registro do lançamento @realizes WRIND-R01

Nada fora de Lançamentos pode impedir uma escrita.

### CESTR-R08 — um valor é gravado na versão lida antes da consulta

Sem isso, um valor antigo poderia ficar sob a versão nova se uma invalidação acontecesse durante a consulta.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `CESTR-R01` | `policy` |
| `CESTR-R02` | `policy` |
| `CESTR-R03` | `policy` |
| `CESTR-R04` | `merchantId` |
| `CESTR-R05` | `merchantId` |
| `CESTR-R06` | `merchantId` |
| `CESTR-R07` | `merchantId` |
| `CESTR-R08` | `merchantId` |

## Decisões em aberto

nenhuma
