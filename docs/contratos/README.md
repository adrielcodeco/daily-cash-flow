# Contratos

| Arquivo | Conteúdo |
|---|---|
| [ledger.openapi.yaml](ledger.openapi.yaml) | API de Lançamentos, em OpenAPI 3.1 |
| [consolidation.openapi.yaml](consolidation.openapi.yaml) | API do Consolidado, em OpenAPI 3.1 |
| [events.asyncapi.yaml](events.asyncapi.yaml) | Canal de eventos, em AsyncAPI 3.0 |
| [entry-recorded.v1.schema.json](entry-recorded.v1.schema.json) | Esquema do evento, em JSON Schema |

## Regras de versionamento

- Rotas usam a versão principal no caminho, como `/v1`
- Campos novos e opcionais não mudam a versão
- Remover ou mudar um campo cria uma nova versão, e as duas convivem durante a migração
- Eventos levam o campo `version`, e consumidores ignoram campos desconhecidos
- Erros seguem o formato Problem Details, da RFC 9457
