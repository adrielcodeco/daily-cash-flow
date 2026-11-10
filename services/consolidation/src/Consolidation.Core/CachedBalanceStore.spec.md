<!-- @anchors
  code: CBSTR
  updated_at: 2026-11-09
-->

# CachedBalanceStore — o cache configurável das consultas

> **Código**: `CBSTR`

## Visão Geral

O cache é opcional e fica desligado por padrão. Ele pode ser ligado por rota, ou por tabela, e nesse caso vale para todas as rotas dela. Esta unidade fica na frente do armazenamento, então os casos de uso não sabem se há cache.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `policy` | nomes de rota ou de tabela, separados por vírgula | nome desconhecido, que é ignorado | esta unidade (`CBSTR-R01`) |
| `merchantId` | comerciante da requisição | — | a API |

## Regras

### CBSTR-R01 — sem alvo ligado, a consulta vai ao armazenamento e não toca no cache

### CBSTR-R02 — um alvo de rota liga o cache só naquela rota

### CBSTR-R03 — um alvo de tabela liga o cache em todas as rotas dela

### CBSTR-R04 — um valor em cache é devolvido sem consultar o armazenamento

### CBSTR-R05 — uma falha do cache não derruba a consulta

O cache melhora a latência, mas nunca impede uma resposta.

### CBSTR-R06 — um evento aplicado invalida o cache do comerciante

### CBSTR-R07 — uma falha do cache não impede a aplicação do evento

O cache expira sozinho. Perder o evento por causa dele seria pior que servir um saldo antigo por alguns segundos.

### CBSTR-R08 — um valor é gravado na versão lida antes da consulta

Sem isso, um valor antigo poderia ficar sob a versão nova se uma invalidação acontecesse durante a consulta.

### CBSTR-R09 — o reset descarta o cache de todos os comerciantes, ou falha

Depois de um reset, um saldo em cache seria de uma base que não existe mais. Ao contrário de um evento, o reset é um comando do operador, que pode repeti-lo, então a falha do cache o interrompe.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `CBSTR-R01` | `policy` |
| `CBSTR-R02` | `policy` |
| `CBSTR-R03` | `policy` |
| `CBSTR-R04` | `merchantId` |
| `CBSTR-R05` | `merchantId` |
| `CBSTR-R06` | `merchantId` |
| `CBSTR-R07` | `merchantId` |
| `CBSTR-R08` | `merchantId` |
| `CBSTR-R09` | `policy` |

## Decisões em aberto

nenhuma
