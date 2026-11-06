// Generates docs/20-metodo-dos-radares.md. The analysis comes from docs/adr/radar/data.json.
// Usage: node tools/radar/method.mjs
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const DECISIVE_WEIGHT = 3;
const docsDir = join(dirname(fileURLToPath(import.meta.url)), '..', '..', 'docs');
const { pillars, adrs } = JSON.parse(readFileSync(join(docsDir, 'adr', 'radar', 'data.json'), 'utf8'));

const sum = (values) => values.reduce((total, value) => total + value, 0);
const average = (values) => (sum(values) / values.length).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const table = (head, rows) => [`| ${head.join(' | ')} |`, `|${head.map(() => '---').join('|')}|`, ...rows.map((row) => `| ${row.join(' | ')} |`)].join('\n');
const shortId = (adr) => adr.id.replace('ADR-', '');
const link = (adr) => `[${shortId(adr)}](adr/${adr.file})`;

const weighted = (adr, option) => {
  const index = pillars.indexOf(adr.decisive);
  return sum(option.scores) + (DECISIVE_WEIGHT - 1) * option.scores[index];
};

const analysis = adrs.map((adr) => {
  const [chosen, ...others] = adr.options;
  const plain = sum(chosen.scores);
  const bestOther = Math.max(...others.map((option) => sum(option.scores)));
  const margin = plain - bestOther;
  const weightedMargin = weighted(adr, chosen) - Math.max(...others.map((option) => weighted(adr, option)));
  return { adr, plain, bestOther, margin, weightedMargin };
});

const wins = analysis.filter((item) => item.margin > 0).length;
const ties = analysis.filter((item) => item.margin === 0);
const losses = analysis.filter((item) => item.margin < 0);
const weightedWins = analysis.filter((item) => item.weightedMargin > 0).length;
const weightedLosses = analysis.filter((item) => item.weightedMargin <= 0);
const fragile = analysis.filter((item) => item.margin >= 0 && item.margin <= 1);
const allScores = adrs.flatMap((adr) => adr.options.flatMap((option) => option.scores));
const signed = (value) => (value > 0 ? `+${value}` : `${value}`.replace('-', '−'));
const list = (items) => items.map((item) => shortId(item.adr)).join(', ');
const bases = ['Requisito', 'Medição', 'Julgamento', 'Fora dos pilares'];

const doc = `# Método dos radares

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

${table(['Pilar', 'Pergunta', 'Nota 1', 'Nota 3', 'Nota 5'], [
  ['Segurança', 'Protege dados e acesso, e facilita auditar?', 'Expõe dados ou não permite controlar acesso', 'Atende com controles comuns, como rede e TLS', 'Controle forte por padrão, com identidade, expiração e auditoria'],
  ['Desempenho', 'A latência e o consumo são baixos?', 'Inviabiliza a meta de latência', 'Cumpre a meta, sem folga relevante', 'Cumpre a meta com muita folga, ou é a opção mais rápida'],
  ['Escalabilidade', 'Acompanha o crescimento da carga?', 'Para de atender com pouco crescimento', 'Atende o cenário Crescimento com ajustes', 'Cresce sem mudar a arquitetura'],
  ['Confiabilidade', 'Isola falhas e evita perda de dados?', 'Quebra um requisito, como a independência da escrita', 'Recupera com intervenção, e o risco é aceitável', 'Falha isolada, sem perda, e com recuperação automática'],
  ['Custo', 'O custo total é baixo?', 'Custo muito acima do que o volume justifica', 'Custo proporcional ao valor', 'Custo marginal ou nenhum componente novo'],
  ['Simplicidade', 'É fácil construir e entender?', 'Exige novo paradigma ou muitos componentes', 'Exige um componente ou conceito novo', 'Cabe no que a equipe já usa, com pouco código'],
  ['Operabilidade', 'É fácil operar e manter com a equipe do cliente?', 'Exige equipe especializada, com operação manual constante', 'Operação comum, com rotinas conhecidas', 'Serviço gerenciado ou prática que a equipe já domina'],
])}

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

${table(['ADR', 'Decisão', 'Pilar decisivo', 'Base', 'Motivo'], adrs.map((adr) => [link(adr), adr.title, adr.decisive, adr.basis, adr.reason]))}

${table(['Base', 'ADRs', 'Significado'], [
  ['Requisito', adrs.filter((adr) => adr.basis === bases[0]).length, 'O desafio ou um requisito aceito define a escolha'],
  ['Medição', adrs.filter((adr) => adr.basis === bases[1]).length, 'Um teste deste projeto confirmou o motivo'],
  ['Julgamento', adrs.filter((adr) => adr.basis === bases[2]).length, 'Argumento técnico, sem medição'],
  ['Fora dos pilares', adrs.filter((adr) => adr.basis === bases[3]).length, 'O motivo real não aparece nos sete pilares'],
])}

Mesmo nas decisões com medição, só a opção escolhida foi medida. **Nenhuma alternativa foi implementada para comparação.** As notas de Custo das alternativas também são estimativas: só a opção escolhida teve preço pesquisado.

## Análise das notas

Esta seção é calculada a partir das ${allScores.length} notas de \`docs/adr/radar/data.json\`.

### A escolhida nem sempre vence na soma

A soma simples das notas é só um teste. Ela trata todos os pilares como iguais, o que o método não faz.

${table(['Resultado', 'ADRs', 'Quais'], [
  ['A escolhida vence', wins, 'Os demais'],
  ['Empate', ties.length, list(ties)],
  ['A escolhida perde', losses.length, list(losses)],
])}

Com peso ${DECISIVE_WEIGHT} no pilar decisivo, a escolhida vence em ${weightedWins} de ${adrs.length} ADRs.${weightedLosses.length ? ` Continua sem vencer em: ${list(weightedLosses)}.` : ''}

${table(['ADR', 'Soma da escolhida', 'Melhor alternativa', 'Margem', 'Margem com peso'], analysis.map((item) => [link(item.adr), item.plain, item.bestOther, signed(item.margin), signed(item.weightedMargin)]))}

Quando a escolhida perde, a explicação está fora da soma:

- **${shortId(losses[0].adr)}:** o monolito é mais simples e barato, mas quebra a independência da escrita. O requisito elimina a opção, mesmo com soma maior.
- **0004:** Firestore soma mais, mas agregações e restrições ficariam na aplicação. Esse motivo não é um pilar.
- **0009:** o SDK do provedor soma mais, mas prende o código ao GCP. Portabilidade não é um pilar.
- **0012:** Dapper soma mais, e o ADR admite que seria igualmente bom. A escolha é uma preferência defensável, e não uma vitória.

### Decisões com margem pequena

Com margem de até 1 ponto, mudar uma única nota pode empatar ou inverter a soma: ${list(fragile)}. Nesses casos, a defesa precisa vir do argumento, e não do gráfico.

### Sinais de viés

${table(['Pilar', 'Média da escolhida', 'Média das alternativas'], pillars.map((pillar, index) => [
  pillar,
  average(adrs.map((adr) => adr.options[0].scores[index])),
  average(adrs.flatMap((adr) => adr.options.slice(1).map((option) => option.scores[index]))),
]))}

${table(['Nota', 'Quantidade', 'Parte do total'], [1, 2, 3, 4, 5].map((score) => {
  const count = allScores.filter((value) => value === score).length;
  return [score, count, `${Math.round((count / allScores.length) * 100)}%`];
}))}

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

1. Altere \`docs/adr/radar/data.json\`.
2. Registre o motivo no ADR.
3. Rode \`node tools/radar/generate.mjs\` e \`node tools/radar/method.mjs\`.

## O que daria mais rigor

| Melhoria | Custo |
|---|---|
| Duas ou mais pessoas darem notas sem ver as dos outros | Baixo |
| Definir pesos antes de dar as notas | Baixo |
| Dar as notas antes de escolher a opção | Baixo, em novas decisões |
| Acrescentar Portabilidade e Aderência ao problema | Exige rever todas as notas |
| Medir as alternativas com protótipos | Alto. Vale só para decisões difíceis de reverter |
`;

writeFileSync(join(docsDir, '20-metodo-dos-radares.md'), doc);
console.log({ wins, ties: list(ties), losses: list(losses), weightedWins, weightedLosses: list(weightedLosses), fragile: list(fragile), total: allScores.length });
