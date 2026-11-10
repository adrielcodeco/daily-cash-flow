<!-- @anchors
  code: LDSCP
  updated_at: 2026-11-09
-->

# ApiSecurity — quem chama a API de Lançamentos, e quanto

> **Código**: `LDSCP`

## Visão Geral

Antes de qualquer operação, a API de Lançamentos confere o token de quem chama, o escopo que ele dá e o comerciante que ele diz. Também reparte a capacidade entre os comerciantes, para que um cliente não esgote os outros.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `audience` | token válido, assinado pelo provedor de identidade e emitido para esta API | token ausente, assinado por outra chave ou emitido para outra API | esta unidade (`LDSCP-R01`) |
| `scope` | `entries.write` para escrever, `entries.read` para ler | escopo que não abre a operação | esta unidade (`LDSCP-R02`) |
| `merchant` | o comerciante declarado no token | token sem o comerciante | esta unidade (`LDSCP-R03`) |
| `permitsPerSecond` | cota de pedidos por segundo de cada comerciante | — | a configuração |

## Regras

### LDSCP-R01 — sem um token válido para esta API, o pedido é recusado com 401

Vale para token ausente, assinado por outra chave ou emitido para outra API, em todas as operações. Nada é gravado.

### LDSCP-R02 — cada operação exige o seu escopo, e sem ele o pedido é recusado com 403

Registrar e estornar exigem `entries.write`. Listar e consultar exigem `entries.read`. Um escopo não abre a operação do outro.

### LDSCP-R03 — um token sem o comerciante é recusado com 403

O comerciante vem só do token. Um token válido que não diz o comerciante não autoriza nenhuma operação, e nada é gravado.

### LDSCP-R04 — a cota de pedidos é de cada comerciante, e nenhum comerciante escapa dela

Passada a cota, o pedido é recusado com 429 e o código `rate_limited`, sem afetar outro comerciante. Nenhum valor do comerciante no token, nem um igual ao nome da verificação de saúde, cai fora da contagem. A verificação de saúde não é limitada.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `LDSCP-R01` | `audience` |
| `LDSCP-R02` | `scope` |
| `LDSCP-R03` | `merchant` |
| `LDSCP-R04` | `merchant`, `permitsPerSecond` |

## Decisões em aberto

nenhuma
