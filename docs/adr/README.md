# Decisões de arquitetura

Cada ADR registra o contexto, as opções, a decisão, as consequências e o gatilho para rever a escolha. As decisões menores e os parâmetros ficam no [registro de decisões](../15-registro-de-decisoes.md).

| ADR | Decisão | Escolha |
|---|---|---|
| [0001](0001-estilo-arquitetural.md) | Estilo arquitetural | Dois serviços orientados a eventos |
| [0002](0002-integracao-entre-servicos.md) | Integração entre serviços | Outbox e mensageria |
| [0003](0003-mensageria.md) | Mensageria | Pub/Sub |
| [0004](0004-banco-de-dados.md) | Banco de dados | Cloud SQL para PostgreSQL |
| [0005](0005-modelo-de-leitura.md) | Modelo de leitura do saldo | Agregado diário e cache |
| [0006](0006-computacao.md) | Plataforma de computação | Cloud Run |
| [0007](0007-linguagem.md) | Linguagem e plataforma | C# com .NET 10 |
| [0008](0008-autenticacao-e-autorizacao.md) | Autenticação e autorização | Gateway, OIDC e JWT |
| [0009](0009-observabilidade.md) | Observabilidade | OpenTelemetry |
| [0010](0010-estrategia-de-migracao.md) | Estratégia de migração | Strangler fig |
| [0011](0011-isolamento-de-comerciantes.md) | Isolamento entre comerciantes | Tabelas compartilhadas com `merchant_id` |
| [0012](0012-acesso-a-dados.md) | Acesso a dados | Npgsql com SQL explícito |
| [0013](0013-migracoes-de-esquema.md) | Migrações de esquema | DbUp, em execução separada |
| [0014](0014-separacao-de-processos.md) | Separação de processos | Quatro processos, dois por serviço |
| [0015](0015-envio-da-outbox.md) | Envio da outbox | Consulta periódica com `SKIP LOCKED` |
| [0016](0016-idempotencia-da-escrita.md) | Idempotência da escrita | `Idempotency-Key` guardada no banco |
| [0017](0017-vazao-do-consumidor.md) | Vazão do consumidor | Limite fixo de eventos em processamento |
| [0018](0018-reconstrucao-do-saldo.md) | Reconstrução do saldo | Reenvio pela outbox |
| [0019](0019-paginacao.md) | Paginação de lançamentos | Keyset, com cursor |
| [0020](0020-estilo-da-api-http.md) | Estilo da API HTTP | Minimal APIs com REST |
| [0021](0021-estrategia-de-testes.md) | Estratégia de testes | Domínio, banco real e mutação |

## Como ler os radares

Todos os radares usam os mesmos sete pilares e a mesma escala. O [método dos radares](../20-metodo-dos-radares.md) explica a régua, como as notas foram dadas e os limites delas. As notas são uma avaliação qualitativa para este problema e esta escala de carga. Elas não são medições e não valem para qualquer sistema.

| Nota | Significado |
|---:|---|
| 1 | Crítico: compromete o requisito ou exige grande compensação |
| 2 | Fraco: atende com esforço relevante |
| 3 | Adequado: atende sem vantagem |
| 4 | Bom: atende com folga |
| 5 | Excelente: é um ponto forte da opção |

| Pilar | Pergunta |
|---|---|
| Segurança | Quão bem protege dados e acesso, e quão fácil é auditar? |
| Desempenho | Quão baixas são a latência e o consumo de recursos? |
| Escalabilidade | Quão bem acompanha o crescimento da carga? |
| Confiabilidade | Quão bem isola falhas e evita perda de dados? |
| Custo | Quão baixo é o custo total? Nota maior significa mais barato |
| Simplicidade | Quão fácil é construir e entender? |
| Operabilidade | Quão fácil é operar, observar e manter com a equipe do cliente? |

Um polígono maior não decide sozinho. O pilar que atende ao requisito central pesa mais, e cada ADR explica esse peso em "Leitura do radar". Sempre há três opções no gráfico para mantê-lo legível. Outras alternativas aparecem no texto.

## Atualizar os radares

As notas ficam em `radar/data.json`. O comando abaixo gera os SVGs e atualiza as tabelas dos ADRs:

```bash
node tools/radar/generate.mjs
```
