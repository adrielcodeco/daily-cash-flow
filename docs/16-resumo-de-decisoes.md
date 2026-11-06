# Resumo das decisões

Lista do que ficou decidido, sem os trade-offs. Os motivos estão nos [ADRs](adr/README.md) e no [registro de decisões](15-registro-de-decisoes.md).

Este arquivo é gerado. Para atualizar, edite as fontes e rode `node tools/decisions/generate.mjs`.

## Arquitetura

| ADR | Tema | Decisão |
|---|---|---|
| [ADR-0001](adr/0001-estilo-arquitetural.md) | Estilo arquitetural | Dois serviços orientados a eventos |
| [ADR-0002](adr/0002-integracao-entre-servicos.md) | Integração entre serviços | Outbox e mensageria |
| [ADR-0003](adr/0003-mensageria.md) | Mensageria | Pub/Sub |
| [ADR-0004](adr/0004-banco-de-dados.md) | Banco de dados | Cloud SQL para PostgreSQL |
| [ADR-0005](adr/0005-modelo-de-leitura.md) | Modelo de leitura do saldo | Agregado diário e cache |
| [ADR-0006](adr/0006-computacao.md) | Plataforma de computação | Cloud Run |
| [ADR-0007](adr/0007-linguagem.md) | Linguagem e plataforma | C# com .NET 10 |
| [ADR-0008](adr/0008-autenticacao-e-autorizacao.md) | Autenticação e autorização | Gateway, OIDC e JWT |
| [ADR-0009](adr/0009-observabilidade.md) | Observabilidade | OpenTelemetry |
| [ADR-0010](adr/0010-estrategia-de-migracao.md) | Estratégia de migração | Strangler fig |
| [ADR-0011](adr/0011-isolamento-de-comerciantes.md) | Isolamento entre comerciantes | Tabelas compartilhadas com `merchant_id` |
| [ADR-0012](adr/0012-acesso-a-dados.md) | Acesso a dados | Npgsql com SQL explícito |
| [ADR-0013](adr/0013-migracoes-de-esquema.md) | Migrações de esquema | DbUp, em execução separada |
| [ADR-0014](adr/0014-separacao-de-processos.md) | Separação de processos | Quatro processos, dois por serviço |
| [ADR-0015](adr/0015-envio-da-outbox.md) | Envio da outbox | Consulta periódica com `SKIP LOCKED` |
| [ADR-0016](adr/0016-idempotencia-da-escrita.md) | Idempotência da escrita | `Idempotency-Key` guardada no banco |
| [ADR-0017](adr/0017-vazao-do-consumidor.md) | Vazão do consumidor | Limite fixo de eventos em processamento |
| [ADR-0018](adr/0018-reconstrucao-do-saldo.md) | Reconstrução do saldo | Reenvio pela outbox |
| [ADR-0019](adr/0019-paginacao.md) | Paginação de lançamentos | Keyset, com cursor |
| [ADR-0020](adr/0020-estilo-da-api-http.md) | Estilo da API HTTP | Minimal APIs com REST |
| [ADR-0021](adr/0021-estrategia-de-testes.md) | Estratégia de testes | Domínio, banco real e mutação |

## Domínio e dados

| ID | Decisão |
|---|---|
| D-01 | Dinheiro em `long`, em centavos |
| D-02 | Uma única moeda: BRL |
| D-03 | Dia pela data de competência, em `America/Sao_Paulo` |
| D-04 | Correção por estorno; lançamento é imutável |
| D-05 | Um lançamento só pode ser estornado uma vez, garantido por índice único |
| D-06 | Lançamento retroativo é permitido |
| D-07 | Abertura calculada na consulta |
| D-08 | Ids em UUID v7 |
| D-09 | `eventId` igual ao id do lançamento |
| D-10 | Evento carrega os dados do lançamento, sem exigir consulta de volta |
| D-11 | Tópico versionado no nome: `entries-recorded-v1` |
| D-12 | `processed_event` guarda todo evento aplicado |
| D-13 | Limpeza da outbox fica como proposta |

## Mensageria e fluxo

| ID | Decisão |
|---|---|
| D-14 | Lote de 100 eventos na outbox |
| D-15 | Espera de 200 ms com a fila vazia |
| D-16 | Espera de 2 s depois de falha ao publicar |
| D-17 | 5 tentativas antes das mensagens mortas |
| D-18 | Nova entrega com espera de 1 a 60 s |
| D-19 | Provisionar a assinatura antes da primeira publicação |
| D-20 | Lote de 500 lançamentos no reenvio |
| D-21 | 10 s para o consumidor encerrar |

## Cache

| ID | Decisão |
|---|---|
| D-22 | Versão por comerciante na chave do cache |
| D-23 | TTL de 60 s |
| D-24 | Ignorar o cache enquanto estiver desconectado |
| D-25 | Manter o cache mesmo sem ganho medido de latência |
| D-26 | Redis local sem persistência, com 64 MB e `allkeys-lru` |

## API e contratos

| ID | Decisão |
|---|---|
| D-27 | Versão na rota: `/v1` |
| D-28 | Registro de outro comerciante responde 404 |
| D-29 | `Idempotency-Key` de 8 a 128 caracteres |
| D-30 | Listagem com 50 itens por padrão e 200 no máximo |
| D-31 | Resposta informa `updatedThrough` |
| D-32 | `X-Merchant-Id` provisório até o M6 |

## Ambiente, teste e entrega

| ID | Decisão |
|---|---|
| D-33 | Emulador do Pub/Sub no ambiente local |
| D-34 | Docker Compose, com build em contêiner |
| D-35 | Sem deploy real no GCP |
| D-36 | Portas locais 18081 e 18082 |
| D-37 | Pool local de 30 conexões, e orçamento menor no GCP |
| D-38 | PostgreSQL 17 e Redis 8 no ambiente local |
| D-39 | Carga com 50 leituras e 10 escritas por segundo |
| D-40 | Mutação com corte em 70% e meta de 90% |
| D-41 | Mutação fora do CI |
| D-42 | Pacotes com versão central e sem vulnerabilidade conhecida |

## Método e documentação

| ID | Decisão |
|---|---|
| D-43 | Documentação em pt-BR, código em en-US |
| D-45 | Nota de radar é qualitativa, com sete pilares e três opções |
| D-46 | Preços de lista em São Paulo, sem desconto |
| D-47 | Volumes de custo são premissas, e só o pico vem do desafio |
