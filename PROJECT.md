# Project: daily-cash-flow

Controle de fluxo de caixa para comerciantes: registra débitos e créditos e oferece o saldo diário consolidado. O serviço de lançamentos continua disponível quando o consolidado cai.

## Produto

| item | escolha |
|---|---|
| usuários | comerciantes, por API |
| dados | isolados por comerciante, com o `merchantId` vindo do token |
| moeda | BRL, em centavos inteiros |
| dia | data de competência, no fuso `America/Sao_Paulo` |
| correção | por estorno; lançamento é imutável |
| lançamento retroativo | permitido |
| saldo | abertura + créditos − débitos, com o acumulado calculado na consulta |

## Stack

| item | escolha |
|---|---|
| linguagem | C# com .NET 10 LTS |
| API | ASP.NET Core |
| banco | PostgreSQL, um por serviço |
| mensageria | Pub/Sub; emulador no ambiente local |
| cache | Redis |
| identidade local | Keycloak, por OIDC |
| telemetria | OpenTelemetry, Prometheus e Grafana |
| testes | xUnit, Testcontainers e k6 |
| execução local | Docker Compose, com build em contêiner |
| nuvem-alvo | GCP, região `southamerica-east1` |

## Estrutura

```text
services/ledger/          serviço de escrita
services/consolidation/   serviço de leitura e consumidor
docs/                     documentação de arquitetura
docs/adr/                 decisões, com radares gerados
docs/contratos/           OpenAPI, AsyncAPI e esquema do evento
load-tests/               cenários k6
tools/radar/              gerador dos gráficos de radar
```

## Arquitetura

- Dois serviços, cada um com processo e banco próprios
- Nenhuma chamada síncrona entre os serviços
- Evento gravado em outbox, na mesma transação do lançamento
- Consumidor idempotente, com entrega at-least-once
- Portas e adaptadores dentro de cada serviço; o domínio não conhece banco nem mensageria

## Convenções

- Documentação em pt-BR: `docs/`, specs, features, ADRs e README
- Código em en-US: identificadores, comentários, logs, testes, contratos, rotas, eventos, tabelas, mensagens de erro da API e mensagens de commit
- Termos de negócio ligados ao código pelo glossário em `docs/00-principios-e-glossario.md`
- Rotas versionadas em `/v1`
- Dinheiro sempre em `long`, em centavos (`AmountCents`)
- Formatação por `.editorconfig` e `dotnet format`
