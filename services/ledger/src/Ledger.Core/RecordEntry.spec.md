<!-- @anchors
  code: RCENT
  updated_at: 2026-11-05
-->

# RecordEntry — o registro atômico do lançamento e do seu evento

> **Código**: `RCENT`

## Visão Geral

Registrar um lançamento precisa deixar o fato e o aviso desse fato juntos. Esta unidade cria o lançamento e entrega os dois ao armazenamento em uma única operação.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `command` | movimento informado pelo comerciante | movimento inválido | `LEDEN` |
| `store` | armazenamento com escrita atômica | — | o tipo |
| `key` | chave de 8 a 128 caracteres | chave fora do tamanho | a API |

## Regras

### RCENT-R01 — o lançamento e o evento são gravados juntos ou nenhum é gravado @realizes WRIND-R02

### RCENT-R02 — o registro não depende do consolidado nem da mensageria @realizes WRIND-R01

### RCENT-R03 — o evento leva os dados do lançamento e usa a identidade dele

Cada lançamento gera um único fato. Com a mesma identidade, um reenvio é reconhecido como repetição pelo consumidor.

### RCENT-R04 — a mesma chave com o mesmo conteúdo devolve o lançamento original

O cliente pode repetir uma chamada depois de uma falha de rede sem criar outro lançamento.

### RCENT-R05 — a mesma chave com outro conteúdo é recusada

### RCENT-R06 — duas chamadas simultâneas com a mesma chave criam um único lançamento

## Uso das regras

| Regra | Usa |
| --- | --- |
| `RCENT-R01` | `command`, `store` |
| `RCENT-R02` | `store` |
| `RCENT-R03` | `command` |
| `RCENT-R04` | `key` |
| `RCENT-R05` | `key` |
| `RCENT-R06` | `key` |

## Decisões em aberto

nenhuma
