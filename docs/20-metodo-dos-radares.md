# Método dos radares

Este documento explica o que significam as notas dos gráficos de radar, como elas foram dadas e até onde confiar nelas.

## Em resumo

- **As notas são opinião técnica, e não medição.** Elas comparam três opções para este problema e esta carga.
- **A régua abaixo foi escrita depois das notas.** Ela descreve o critério aplicado, para que as notas possam ser revistas. As notas não nasceram de uma régua definida antes.
- **O radar não decide.** A decisão vem do pilar que atende ao requisito central. Ele aparece em "Leitura do radar", em cada ADR.
- **Só uma parte das notas tem evidência.** A maioria nasce de conhecimento técnico sobre cada opção.

## O que cada nota quer dizer

A nota compara a opção com a necessidade deste sistema, e não com o melhor produto do mercado. Um 5 aqui não é um 5 universal.

| Nota | Significado | Na prática |
|---:|---|---|
| 1 | Crítico | Compromete um requisito, ou exige grande compensação |
| 2 | Fraco | Atende, com esforço relevante ou risco claro |
| 3 | Adequado | Atende, sem vantagem. É o ponto de partida |
| 4 | Bom | Atende com folga |
| 5 | Excelente | É um ponto forte da opção neste problema |

Nota maior é sempre melhor. Em Custo, nota maior significa mais barato. Em Simplicidade, significa menos para construir e entender.

## Régua de cada pilar

As notas 2 e 4 ficam entre as colunas.

| Pilar | Pergunta | Nota 1 | Nota 3 | Nota 5 |
|---|---|---|---|---|
| Segurança | Protege dados e acesso, e facilita auditar? | Expõe dados ou não permite controlar acesso | Atende com controles comuns, como rede e TLS | Controle forte por padrão, com identidade, expiração e auditoria |
| Desempenho | A latência e o consumo são baixos? | Inviabiliza a meta de latência | Cumpre a meta, sem folga relevante | Cumpre a meta com muita folga, ou é a opção mais rápida |
| Escalabilidade | Acompanha o crescimento da carga? | Para de atender com pouco crescimento | Atende o cenário Crescimento com ajustes | Cresce sem mudar a arquitetura |
| Confiabilidade | Isola falhas e evita perda de dados? | Quebra um requisito, como a independência da escrita | Recupera com intervenção, e o risco é aceitável | Falha isolada, sem perda, e com recuperação automática |
| Custo | O custo total é baixo? | Custo muito acima do que o volume justifica | Custo proporcional ao valor | Custo marginal ou nenhum componente novo |
| Simplicidade | É fácil construir e entender? | Exige novo paradigma ou muitos componentes | Exige um componente ou conceito novo | Cabe no que a equipe já usa, com pouco código |
| Operabilidade | É fácil operar e manter com a equipe do cliente? | Exige equipe especializada, com operação manual constante | Operação comum, com rotinas conhecidas | Serviço gerenciado ou prática que a equipe já domina |

## Como as notas foram dadas

Este procedimento é uma reconstrução. Ele descreve o raciocínio usado, mas não foi seguido como um formulário nem registrado nota a nota. Por isso, algumas notas podem não bater exatamente com a régua.

1. **Três opções por decisão.** A escolhida e as duas alternativas mais fortes. Outras aparecem só no texto do ADR.
2. **Partida em 3.** Cada nota começa em "adequado".
3. **Ajuste por fatos da opção.** Sobe ou desce por características conhecidas. Exemplo: Pub/Sub é gerenciado, então recebe 5 em Operabilidade. RabbitMQ exige cluster próprio, então recebe 2.
4. **Nota 1 marca o que quebra um requisito.** HTTP síncrono recebe 1 em Confiabilidade porque a queda do consolidado afetaria a escrita.
5. **Comparação na mesma linha.** Cada pilar é comparado entre as três opções do ADR, e não entre ADRs.
6. **Sem pesos e sem soma.** A leitura aponta o pilar decisivo, e o texto explica a escolha.

**Quem deu as notas:** o agente de IA que escreveu a documentação, com as decisões já discutidas com o autor. Não houve comitê nem votação.

**Quando:** nos ADRs 0001 a 0010, durante o desenho. Nos ADRs 0011 a 0021, depois da implementação, com a escolha já feita.

## O que sustenta cada decisão

A coluna **Base** diz de onde veio a força da decisão, e não de cada nota.

| ADR | Decisão | Pilar decisivo | Base | Motivo |
|---|---|---|---|---|
| [0001](adr/0001-estilo-arquitetural.md) | Estilo arquitetural | Confiabilidade | Requisito | Isolar a falha do consolidado |
| [0002](adr/0002-integracao-entre-servicos.md) | Integração entre serviços | Confiabilidade | Requisito | Nenhuma dependência síncrona entre os serviços |
| [0003](adr/0003-mensageria.md) | Mensageria | Operabilidade | Julgamento | Serviço gerenciado, sem cluster próprio |
| [0004](adr/0004-banco-de-dados.md) | Banco de dados | Simplicidade | Fora dos pilares | Transações, restrições e SQL. A aderência ao modelo relacional não é um pilar |
| [0005](adr/0005-modelo-de-leitura.md) | Modelo de leitura do saldo | Desempenho | Medição | Leitura rápida e isolada, medida na carga |
| [0006](adr/0006-computacao.md) | Plataforma de computação | Operabilidade | Julgamento | Menos operação que GKE e máquinas virtuais |
| [0007](adr/0007-linguagem.md) | Linguagem e plataforma | Operabilidade | Julgamento | Equipe que já domina C# |
| [0008](adr/0008-autenticacao-e-autorizacao.md) | Autenticação e autorização | Segurança | Julgamento | Tokens curtos e escopos |
| [0009](adr/0009-observabilidade.md) | Observabilidade | Operabilidade | Fora dos pilares | Portabilidade: mesmo código no local e na nuvem. Portabilidade não é um pilar |
| [0010](adr/0010-estrategia-de-migracao.md) | Estratégia de migração | Confiabilidade | Julgamento | Poder parar e voltar em cada fase |
| [0011](adr/0011-isolamento-de-comerciantes.md) | Isolamento entre comerciantes | Custo | Julgamento | Custo que não cresce com cada comerciante |
| [0012](adr/0012-acesso-a-dados.md) | Acesso a dados | Simplicidade | Fora dos pilares | Uma dependência a menos e SQL visível. O texto admite que Dapper seria igualmente bom |
| [0013](adr/0013-migracoes-de-esquema.md) | Migrações de esquema | Simplicidade | Julgamento | SQL explícito no binário do serviço |
| [0014](adr/0014-separacao-de-processos.md) | Separação de processos | Confiabilidade | Requisito | Isolar falhas entre API e worker |
| [0015](adr/0015-envio-da-outbox.md) | Envio da outbox | Simplicidade | Julgamento | Nenhum componente novo |
| [0016](adr/0016-idempotencia-da-escrita.md) | Idempotência da escrita | Confiabilidade | Requisito | Garantia na mesma transação, sem depender do cache |
| [0017](adr/0017-vazao-do-consumidor.md) | Vazão do consumidor | Confiabilidade | Medição | O teste de resiliência esgotou as conexões |
| [0018](adr/0018-reconstrucao-do-saldo.md) | Reconstrução do saldo | Confiabilidade | Requisito | Um só caminho de cálculo, sem ler o banco do outro serviço |
| [0019](adr/0019-paginacao.md) | Paginação de lançamentos | Desempenho | Julgamento | Tempo estável em qualquer página |
| [0020](adr/0020-estilo-da-api-http.md) | Estilo da API HTTP | Simplicidade | Julgamento | Pouco código para poucas rotas |
| [0021](adr/0021-estrategia-de-testes.md) | Estratégia de testes | Confiabilidade | Medição | O banco real achou um defeito que falsos não achariam |

| Base | ADRs | Significado |
|---|---|---|
| Requisito | 5 | O desafio ou um requisito aceito define a escolha |
| Medição | 3 | Um teste deste projeto confirmou o motivo |
| Julgamento | 10 | Argumento técnico, sem medição |
| Fora dos pilares | 3 | O motivo real não aparece nos sete pilares |

Mesmo nas decisões com medição, só a opção escolhida foi medida. **Nenhuma alternativa foi implementada para comparação.** As notas de Custo das alternativas também são estimativas: só a opção escolhida teve preço pesquisado.

## Análise das notas

Esta seção é calculada a partir das 441 notas de `docs/adr/radar/data.json`.

### A escolhida nem sempre vence na soma

A soma simples das notas é só um teste. Ela trata todos os pilares como iguais, o que o método não faz.

| Resultado | ADRs | Quais |
|---|---|---|
| A escolhida vence | 16 | Os demais |
| Empate | 1 | 0007 |
| A escolhida perde | 4 | 0001, 0004, 0009, 0012 |

Com peso 3 no pilar decisivo, a escolhida vence em 20 de 21 ADRs. Continua sem vencer em: 0012.

| ADR | Soma da escolhida | Melhor alternativa | Margem | Margem com peso |
|---|---|---|---|---|
| [0001](adr/0001-estilo-arquitetural.md) | 27 | 28 | −1 | +2 |
| [0002](adr/0002-integracao-entre-servicos.md) | 28 | 24 | +4 | +8 |
| [0003](adr/0003-mensageria.md) | 32 | 25 | +7 | +11 |
| [0004](adr/0004-banco-de-dados.md) | 28 | 29 | −1 | +3 |
| [0005](adr/0005-modelo-de-leitura.md) | 30 | 22 | +8 | +10 |
| [0006](adr/0006-computacao.md) | 31 | 27 | +4 | +8 |
| [0007](adr/0007-linguagem.md) | 28 | 28 | 0 | +4 |
| [0008](adr/0008-autenticacao-e-autorizacao.md) | 29 | 28 | +1 | +7 |
| [0009](adr/0009-observabilidade.md) | 28 | 29 | −1 | +3 |
| [0010](adr/0010-estrategia-de-migracao.md) | 26 | 22 | +4 | +12 |
| [0011](adr/0011-isolamento-de-comerciantes.md) | 31 | 22 | +9 | +13 |
| [0012](adr/0012-acesso-a-dados.md) | 28 | 29 | −1 | −3 |
| [0013](adr/0013-migracoes-de-esquema.md) | 29 | 27 | +2 | +4 |
| [0014](adr/0014-separacao-de-processos.md) | 28 | 26 | +2 | +6 |
| [0015](adr/0015-envio-da-outbox.md) | 29 | 26 | +3 | +7 |
| [0016](adr/0016-idempotencia-da-escrita.md) | 29 | 24 | +5 | +12 |
| [0017](adr/0017-vazao-do-consumidor.md) | 28 | 25 | +3 | +5 |
| [0018](adr/0018-reconstrucao-do-saldo.md) | 28 | 26 | +2 | +4 |
| [0019](adr/0019-paginacao.md) | 30 | 24 | +6 | +12 |
| [0020](adr/0020-estilo-da-api-http.md) | 30 | 27 | +3 | +7 |
| [0021](adr/0021-estrategia-de-testes.md) | 27 | 26 | +1 | +7 |

Quando a escolhida perde, a explicação está fora da soma:

- **0001:** o monolito é mais simples e barato, mas quebra a independência da escrita. O requisito elimina a opção, mesmo com soma maior.
- **0004:** Firestore soma mais, mas agregações e restrições ficariam na aplicação. Esse motivo não é um pilar.
- **0009:** o SDK do provedor soma mais, mas prende o código ao GCP. Portabilidade não é um pilar.
- **0012:** Dapper soma mais, e o ADR admite que seria igualmente bom. A escolha é uma preferência defensável, e não uma vitória.

### Decisões com margem pequena

Com margem de até 1 ponto, mudar uma única nota pode empatar ou inverter a soma: 0007, 0008, 0021. Nesses casos, a defesa precisa vir do argumento, e não do gráfico.

### Sinais de viés

| Pilar | Média da escolhida | Média das alternativas |
|---|---|---|
| Segurança | 4,05 | 3,64 |
| Desempenho | 4,10 | 3,81 |
| Escalabilidade | 4,24 | 3,57 |
| Confiabilidade | 4,52 | 3,36 |
| Custo | 3,95 | 3,57 |
| Simplicidade | 3,81 | 3,36 |
| Operabilidade | 4,10 | 2,93 |

| Nota | Quantidade | Parte do total |
|---|---|---|
| 1 | 12 | 3% |
| 2 | 44 | 10% |
| 3 | 107 | 24% |
| 4 | 189 | 43% |
| 5 | 89 | 20% |

- **A escolhida tem média maior em todos os pilares.** Parte disso é legítima, porque ela foi escolhida por ser melhor. Parte pode ser viés de quem já conhecia a resposta.
- **As notas se concentram em 4.** A escala usa pouco o 1 e o 2, então as diferenças ficam pequenas.

### Inconsistências conhecidas

| Caso | Problema |
|---|---|
| CDC nos ADRs 0002 e 0015 | Recebe Desempenho 4 em um e 5 no outro, sem motivo declarado |
| Operabilidade no ADR-0007 | Mede o domínio da equipe, e não a operação do sistema |
| Pilares no ADR-0021 | Segurança e Escalabilidade dizem pouco sobre estratégia de testes |
| Portabilidade | Decide o ADR-0009, mas não é um pilar |
| Aderência ao problema | Decide o ADR-0004, mas não é um pilar |

As notas foram mantidas como estavam. Corrigi-las depois de ver esta análise seria ajustar o resultado à conclusão.

## Como usar em uma revisão

Para defender uma decisão, parta do requisito e do pilar decisivo, e use o radar como resumo visual. Se alguém questionar uma nota, a resposta honesta é: "é um julgamento, e o motivo é este". Discordar de uma nota é esperado, e o que importa é saber se a discordância muda a decisão.

Para rever uma nota:

1. Altere `docs/adr/radar/data.json`.
2. Registre o motivo no ADR.
3. Rode `node tools/radar/generate.mjs` e `node tools/radar/method.mjs`.

## O que daria mais rigor

| Melhoria | Custo |
|---|---|
| Duas ou mais pessoas darem notas sem ver as dos outros | Baixo |
| Definir pesos antes de dar as notas | Baixo |
| Dar as notas antes de escolher a opção | Baixo, em novas decisões |
| Acrescentar Portabilidade e Aderência ao problema | Exige rever todas as notas |
| Medir as alternativas com protótipos | Alto. Vale só para decisões difíceis de reverter |
