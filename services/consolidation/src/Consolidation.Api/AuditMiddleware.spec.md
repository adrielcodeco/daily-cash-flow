<!-- @anchors
  code: CNADS
  updated_at: 2026-11-09
-->

# AuditMiddleware — a trilha de auditoria da API do Consolidado

> **Código**: `CNADS`

## Visão Geral

Investigar um incidente pede saber quem tentou ver o que não podia. Esta unidade deixa uma linha na trilha de auditoria, separada dos logs de operação, para cada acesso recusado e cada escrita.

## Domínio

| Entrada | Aceita | Fora do domínio | Quem garante |
| --- | --- | --- | --- |
| `status` | o status final da resposta | — | a API |
| `refused` | resposta 401, 403, 404 ou 429 | — | esta unidade (`CNADS-R01`) |

## Regras

### CNADS-R01 — todo acesso recusado fica na trilha de auditoria

Vale para as respostas 401, 403, 404 e 429. A linha traz a operação, o caminho, o status, o cliente e o comerciante. Uma escrita também entra na trilha, embora hoje esta API só tenha consultas.

### CNADS-R02 — uma consulta bem-sucedida fica fora da trilha

As consultas bem-sucedidas já aparecem nas métricas e nos rastros.

## Uso das regras

| Regra | Usa |
| --- | --- |
| `CNADS-R01` | `refused`, `status` |
| `CNADS-R02` | `status` |

## Decisões em aberto

nenhuma
