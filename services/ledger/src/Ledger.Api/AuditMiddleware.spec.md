<!-- @anchors
  code: LDADS
  updated_at: 2026-11-09
-->

# AuditMiddleware — a trilha de auditoria da API de Lançamentos

> **Código**: `LDADS`

## Visão Geral

Investigar um incidente pede saber quem escreveu e quem tentou o que não podia. Esta unidade deixa uma linha na trilha de auditoria, separada dos logs de operação, para cada escrita e cada acesso recusado.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `status` | o status final da resposta | — | a API |
| `refused` | resposta 401, 403, 404 ou 429 | — | esta unidade (`LDADS-R02`) |

## Regras

### LDADS-R01 — toda escrita fica na trilha de auditoria

A linha traz a operação, o caminho, o status, o cliente e o comerciante.

### LDADS-R02 — todo acesso recusado fica na trilha de auditoria

Vale para as respostas 401, 403, 404 e 429. O 404 entra porque é a resposta a quem pede o lançamento de outro comerciante.

### LDADS-R03 — uma leitura bem-sucedida fica fora da trilha

As leituras bem-sucedidas já aparecem nas métricas e nos rastros.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `LDADS-R01` | `status` |
| `LDADS-R02` | `refused`, `status` |
| `LDADS-R03` | `status` |

## Decisões em aberto

nenhuma
