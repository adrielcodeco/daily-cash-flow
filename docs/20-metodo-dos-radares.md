# Método dos radares

Este documento explica o que significam as notas dos gráficos de radar e como elas são dadas.

## Em resumo

- **A escala vai de 1 a 5 e sai de fatos.** Cada critério tem 4 perguntas de sim ou não, e a nota é 1 mais o número de "sim".
- **Cada decisão tem seus critérios, pesos e opções.** Hoje são de 4 a 10 critérios, e de 2 a 4 opções por decisão.
- **Alinhamento com o negócio é o critério de maior peso.** Ele é a forma de o arquiteto aproximar o resultado do que o negócio precisa.
- **Veto elimina a opção.** Uma opção que quebra uma restrição sai da comparação, sem importar a média.
- **O resultado apoia a decisão, mas não a substitui.** A decisão final é do arquiteto, e todo ajuste fica registrado.

## O que cada nota quer dizer

| Nota | "Sim" nas perguntas | Significado |
|---:|---:|---|
| 1 | 0, ou veto | Crítico |
| 2 | 1 | Fraco |
| 3 | 2 | Adequado |
| 4 | 3 | Bom |
| 5 | 4 | Excelente |

Nota maior é sempre melhor, em todos os critérios. No gráfico, quanto mais longe do centro, melhor. O critério de custo se chama **Economia** para deixar isso claro.

A nota compara a opção com a necessidade deste sistema. Ela não mede o produto em geral.

## Como uma decisão é avaliada

1. **Escolher os critérios** do catálogo que se aplicam à decisão, com no mínimo 3. Se faltar algo, a decisão pode criar um critério próprio, com 4 perguntas.
2. **Definir os pesos**, antes de avaliar as opções.
3. **Responder às perguntas** de cada opção, com a evidência.
4. **Aplicar os vetos**, que eliminam a opção.
5. **Calcular o resultado:** a média das notas, ponderada pelos pesos.

| Peso | Regra |
|---:|---|
| 4 | Alinhamento com o negócio, em toda decisão |
| 3 | A decisão afeta diretamente uma restrição do negócio por este critério |
| 2 | Relevante para esta decisão |
| 1 | Secundário |

## Catálogo de critérios

| Critério | Perguntas | Veto |
|---|---|---|
| Alinhamento com o negócio | Atende a uma restrição ou capacidade do negócio? Entrega valor cedo? É compatível com a stack, o provedor e as regras de uma instituição financeira? Apoia a evolução prevista? | Contraria uma restrição do negócio |
| Segurança | Controla acesso por identidade? Não acrescenta exposição? Facilita auditar? O isolamento não depende só de disciplina no código? | Expõe dados de um comerciante a outro |
| Desempenho | Cumpre as metas de p95? Não acrescenta chamada de rede ao caminho crítico? A latência é estável? O consumo é baixo? | Não cumpre a meta de latência |
| Escalabilidade | Atende 500 req/s sem mudar a arquitetura? Escala sem coordenação manual? Não cria gargalo novo? O esforço cresce menos que a carga? | Não atende 50 req/s |
| Confiabilidade | A falha fica isolada? Falhas comuns não perdem dado confirmado? Recupera sozinho? Não cria ponto único de falha, e repetir é seguro? | A escrita depende do consolidado, ou há perda de lançamento em falha comum |
| Economia | Não acrescenta componente pago? O custo fixo adicional fica abaixo de US$ 100 por mês? O custo cresce menos que a carga? Construir e manter custa menos de 1 pessoa-mês por ano? | Custo acima de 3 vezes US$ 1.250 por mês |
| Simplicidade | Não acrescenta componente? Não exige conceito desconhecido de uma equipe .NET? Exige pouco código? É fácil testar localmente? | Nenhum |
| Operabilidade | É gerenciado? Atualiza sem parada? Oferece telemetria padrão? A equipe já domina? | Exige especialista dedicado |
| Portabilidade | Funciona igual no local e na nuvem? Permite trocar de fornecedor? Usa padrão aberto? Não prende dados em formato proprietário? | Nenhum |
| Aderência ao problema | Combina com dados financeiros relacionais? Oferece as garantias nativamente? Evita lógica compensatória? Foi feita para este volume? | Não oferece uma garantia exigida |

## Como esta avaliação foi feita

Três agentes de IA avaliaram as 21 decisões e as 69 opções, sem acesso ao repositório. Eles não sabiam qual opção tinha sido escolhida, as opções vieram embaralhadas, e eles definiram critérios e pesos antes das notas. Depois disso, o autor decidiu o que aceitar. As respostas e a evidência estão na [avaliação dos radares](21-avaliacao-dos-radares.md).

## Ajustes do arquiteto

A avaliação não é a palavra final. Estes ajustes foram decididos pelo autor, depois da avaliação:

| ADR | Opção | Critério | Antes | Depois | Motivo |
|---|---|---|---|---|---|
| 0004 | AlloyDB | Alinhamento com o negócio | 5 | 4 | Capacidade acima da necessidade atual do negócio, conforme o princípio P6. O preço maior ainda precisa ser confirmado na calculadora do GCP. |
| 0007 | Go | Alinhamento com o negócio | Veto | 2 | C# é a stack da empresa, mas é preferência de negócio, e não restrição. A opção perde em Alinhamento, sem ser eliminada. |
| 0007 | Java com Spring Boot | Alinhamento com o negócio | Veto | 2 | C# é a stack da empresa, mas é preferência de negócio, e não restrição. A opção perde em Alinhamento, sem ser eliminada. |
| 0007 | TypeScript com Node.js | Alinhamento com o negócio | Veto | 2 | C# é a stack da empresa, mas é preferência de negócio, e não restrição. A opção perde em Alinhamento, sem ser eliminada. |

A avaliação também mudou a arquitetura em três decisões:

| ADR | Antes | Depois |
|---|---|---|
| 0005 | Cache sempre ligado | Sem cache por padrão, com cache configurável por rota ou tabela |
| 0011 | Isolamento só no código | Row-level security |
| 0012 | Npgsql direto | Dapper |

## Resultado

| ADR | Decisão | Escolhida | Nota | Melhor alternativa | Nota | Margem | Eliminadas |
|---|---|---|---|---|---|---|---|
| [0001](adr/0001-estilo-arquitetural.md) | Estilo arquitetural | Dois serviços orientados a eventos | 4,24 | Funções serverless | 4,06 | 0,18 | 1 |
| [0002](adr/0002-integracao-entre-servicos.md) | Integração entre serviços | Outbox na transação e mensageria | 4,45 | CDC do banco | 3,35 | 1,10 | 2 |
| [0003](adr/0003-mensageria.md) | Mensageria | Pub/Sub | 4,16 | Kafka gerenciado | 4,00 | 0,16 | 0 |
| [0004](adr/0004-banco-de-dados.md) | Banco de dados | Cloud SQL para PostgreSQL | 4,28 | AlloyDB | 4,16 | 0,12 | 0 |
| [0005](adr/0005-modelo-de-leitura.md) | Modelo de leitura do saldo | Agregado diário sem cache | 4,63 | Agregado diário com cache Redis | 3,53 | 1,11 | 0 |
| [0006](adr/0006-computacao.md) | Plataforma de computação | Cloud Run | 4,50 | GKE Autopilot | 4,35 | 0,15 | 0 |
| [0007](adr/0007-linguagem.md) | Linguagem e plataforma | C# com .NET 10 | 4,92 | Go | 3,25 | 1,67 | 0 |
| [0008](adr/0008-autenticacao-e-autorizacao.md) | Autenticação e autorização | Gateway, OIDC e JWT | 4,36 | Chaves de API | 3,36 | 1,00 | 0 |
| [0009](adr/0009-observabilidade.md) | Observabilidade | OpenTelemetry | 4,57 | SDK do provedor de nuvem | 3,71 | 0,86 | 0 |
| [0010](adr/0010-estrategia-de-migracao.md) | Estratégia de migração | Strangler fig, por fases | 4,13 | Migração de uma vez | 2,93 | 1,20 | 1 |
| [0011](adr/0011-isolamento-de-comerciantes.md) | Isolamento entre comerciantes | Tabelas compartilhadas com row-level security | 4,32 | Tabelas compartilhadas com filtro no código | 3,79 | 0,53 | 0 |
| [0012](adr/0012-acesso-a-dados.md) | Acesso a dados | Dapper | 5,00 | Npgsql com SQL explícito | 4,82 | 0,18 | 0 |
| [0013](adr/0013-migracoes-de-esquema.md) | Migrações de esquema | DbUp com SQL | 5,00 | Flyway | 4,31 | 0,69 | 0 |
| [0014](adr/0014-separacao-de-processos.md) | Separação de processos | Processos separados | 4,65 | Job agendado | 4,41 | 0,24 | 0 |
| [0015](adr/0015-envio-da-outbox.md) | Envio da outbox | Consulta periódica com SKIP LOCKED | 5,00 | LISTEN/NOTIFY com consulta de segurança | 4,21 | 0,79 | 0 |
| [0016](adr/0016-idempotencia-da-escrita.md) | Idempotência da escrita | Idempotency-Key guardada no banco | 4,88 | Nenhuma restante | — | — | 2 |
| [0017](adr/0017-vazao-do-consumidor.md) | Vazão do consumidor | Limite fixo de eventos em processamento | 4,76 | Pooler de conexões | 3,18 | 1,59 | 1 |
| [0018](adr/0018-reconstrucao-do-saldo.md) | Reconstrução do saldo | Reenvio dos lançamentos pela outbox | 3,89 | Restaurar backup do consolidado e reprocessar o que faltou | 3,84 | 0,05 | 2 |
| [0019](adr/0019-paginacao.md) | Paginação de lançamentos | Keyset com cursor | 4,86 | Offset e limite | 2,71 | 2,14 | 0 |
| [0020](adr/0020-estilo-da-api-http.md) | Estilo da API HTTP | Minimal APIs | 4,82 | Controllers MVC | 4,73 | 0,09 | 0 |
| [0021](adr/0021-estrategia-de-testes.md) | Estratégia de testes | Domínio, banco real e mutação | 4,23 | Só testes de ponta a ponta | 3,15 | 1,08 | 1 |

10 opções foram eliminadas por veto.

**Decisões apertadas**, com margem menor que 0,25: 0001, 0003, 0004, 0006, 0012, 0014, 0018, 0020. Nesses casos, a defesa vem do argumento, e não do gráfico.

**Sem o critério de negócio**, a escolhida deixa de vencer em: 0004, 0018.

**Com pesos iguais**, a escolhida deixa de vencer em: nenhuma.

## Limites

- **Avaliadores:** são modelos de IA, com uma rodada cada, sem repetição para medir variação.
- **Interpretação:** pontos abertos da régua foram interpretados. O veto do monolito no ADR-0001, por exemplo, trata "mesmo processo" como dependência da escrita.
- **"A equipe já domina":** a resposta foi "não" quando a informação faltava, o que favorece opções em SQL e C#.
- **Fatos não verificados:** ficaram marcados na evidência, principalmente preços em São Paulo. O preço do AlloyDB precisa de confirmação.
- **Medições:** existem só para parte das opções, e nenhuma alternativa foi implementada para comparação.
- **Degraus:** com 4 perguntas por critério, um 5,00 não significa perfeição.
- **Sobreposição:** Alinhamento pode contar de novo um mérito que já aparece em outro critério.
- **Decisão anterior:** a escolha já existia quando a avaliação foi feita, e o autor ajustou resultados depois de vê-los. Os ajustes estão expostos acima.

## Histórico

A primeira avaliação usava sete pilares fixos, três opções e notas por julgamento, sem régua prévia. Na soma simples, a opção escolhida perdia em 4 decisões e empatava em 1, e a escolhida tinha média maior em todos os pilares, o que indicava viés. As notas originais estão em `docs/adr/radar/historico/avaliacao-original.json`.

## Como rever

1. Altere `docs/adr/radar/data.json`. Se for um ajuste de julgamento, registre em `adjustments`, com o motivo.
2. Rode `node tools/radar/generate.mjs` e `node tools/radar/method.mjs`.
