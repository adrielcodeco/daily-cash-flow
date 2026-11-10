<!-- @anchors
  code: CNSCP
  updated_at: 2026-11-09
-->

# ApiSecurity — quem consulta a API do Consolidado, e quanto

> **Código**: `CNSCP`

## Visão Geral

Antes de qualquer consulta de saldo, a API do Consolidado confere o token de quem chama, o escopo que ele dá e o comerciante que ele diz. Também reparte a capacidade entre os comerciantes, para que um cliente não esgote os outros.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `audience` | token válido, assinado pelo provedor de identidade e emitido para esta API | token ausente, assinado por outra chave ou emitido para outra API | esta unidade (`CNSCP-R01`) |
| `scope` | `balances.read` | escopo que não abre a consulta | esta unidade (`CNSCP-R02`) |
| `merchant` | o comerciante declarado no token | token sem o comerciante | esta unidade (`CNSCP-R03`) |
| `permitsPerSecond` | cota de pedidos por segundo de cada comerciante | — | a configuração |

## Regras

### CNSCP-R01 — sem um token válido para esta API, o pedido é recusado com 401

Vale para token ausente, assinado por outra chave ou emitido para outra API, nas duas consultas.

### CNSCP-R02 — as consultas exigem o escopo balances.read, e sem ele o pedido é recusado com 403

Os escopos de Lançamentos não abrem as consultas de saldo.

### CNSCP-R03 — um token sem o comerciante é recusado com 403

O comerciante vem só do token. Um token válido que não diz o comerciante não consulta nada.

### CNSCP-R04 — a cota de pedidos é de cada comerciante, e nenhum comerciante escapa dela

Passada a cota, o pedido é recusado com 429 e o código `rate_limited`, sem afetar outro comerciante. Nenhum valor do comerciante no token, nem um igual ao nome da verificação de saúde, cai fora da contagem. A verificação de saúde não é limitada.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `CNSCP-R01` | `audience` |
| `CNSCP-R02` | `scope` |
| `CNSCP-R03` | `merchant` |
| `CNSCP-R04` | `merchant`, `permitsPerSecond` |

## Decisões em aberto

nenhuma
