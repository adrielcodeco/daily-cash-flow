// Generates docs/20-metodo-dos-radares.md and docs/21-avaliacao-dos-radares.md from docs/adr/radar/data.json.
// Usage: node tools/radar/method.mjs
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const BUSINESS = 'Alinhamento com o negócio';
const docsDir = join(dirname(fileURLToPath(import.meta.url)), '..', '..', 'docs');
const { adrs, adjustments } = JSON.parse(readFileSync(join(docsDir, 'adr', 'radar', 'data.json'), 'utf8'));

const sum = (values) => values.reduce((total, value) => total + value, 0);
const decimal = (value) => value.toFixed(2).replace('.', ',');
const table = (head, rows) => [`| ${head.join(' | ')} |`, `|${head.map(() => '---').join('|')}|`, ...rows.map((row) => `| ${row.join(' | ')} |`)].join('\n');
const shortId = (adr) => adr.id.replace('ADR-', '');
const link = (adr) => `[${shortId(adr)}](adr/${adr.file})`;
const inline = (text) => String(text).replaceAll('|', '\\|').replaceAll('\n', ' ');

const weighted = (adr, option, skip) => {
  const used = adr.criteria.map((criterion, i) => ({ criterion, score: option.scores[i] })).filter(({ criterion }) => criterion.name !== skip);
  return sum(used.map(({ criterion, score }) => criterion.weight * score)) / sum(used.map(({ criterion }) => criterion.weight));
};
const plain = (option) => sum(option.scores) / option.scores.length;

const analysis = adrs.map((adr) => {
  const [chosen, ...others] = adr.options;
  const alive = others.filter((option) => option.vetoes.length === 0);
  const best = [...alive].sort((a, b) => weighted(adr, b) - weighted(adr, a))[0];
  const margin = best ? weighted(adr, chosen) - weighted(adr, best) : null;
  const beats = (score) => alive.every((option) => score(chosen) >= score(option));
  return { adr, chosen, best, margin, eliminated: others.length - alive.length, withoutBusiness: beats((o) => weighted(adr, o, BUSINESS)), equalWeights: beats(plain) };
});

const close = analysis.filter((item) => item.margin !== null && item.margin < 0.25);
const options = sum(adrs.map((adr) => adr.options.length));
const eliminated = sum(analysis.map((item) => item.eliminated));
const counts = adrs.map((adr) => adr.criteria.length);
const list = (items) => (items.length ? items.map((item) => shortId(item.adr)).join(', ') : 'nenhuma');

const method = `# Método dos radares

Este documento explica o que significam as notas dos gráficos de radar e como elas são dadas.

## Em resumo

- **A escala vai de 1 a 5 e sai de fatos.** Cada critério tem 4 perguntas de sim ou não, e a nota é 1 mais o número de "sim".
- **Cada decisão tem seus critérios, pesos e opções.** Hoje são de ${Math.min(...counts)} a ${Math.max(...counts)} critérios, e de 2 a 4 opções por decisão.
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

Duas regras completam a tabela:

- **Segurança pesa no mínimo 2** em toda decisão em que é critério, porque este é um sistema financeiro. Ela entra em toda decisão cujas opções mudam a exposição dos dados, e pesa 3 quando a decisão afeta diretamente o isolamento entre comerciantes ou as exigências de uma instituição financeira (PRE-11).
- **A equipe reduzida é premissa, e não restrição do desafio.** A premissa PRE-12 diz que a operação fica com uma equipe reduzida, sem especialista dedicado em infraestrutura, e ela pesa em Operabilidade.

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

Três agentes de IA avaliaram as ${adrs.length} decisões e as ${options} opções, sem acesso ao repositório. Eles não sabiam qual opção tinha sido escolhida, as opções vieram embaralhadas, e eles definiram critérios e pesos antes das notas. Depois disso, o autor decidiu o que aceitar. As respostas e a evidência estão na [avaliação dos radares](21-avaliacao-dos-radares.md).

## Ajustes do arquiteto

A avaliação não é a palavra final. Estes ajustes foram decididos pelo autor, depois da avaliação:

${table(['ADR', 'Opção', 'Critério', 'Antes', 'Depois', 'Motivo'], adjustments.map((item) => [shortId({ id: item.adr }), item.option, item.criterion, item.from === 'veto' ? 'Veto' : item.from, item.to, item.reason]))}

A avaliação também mudou a arquitetura em três decisões:

| ADR | Antes | Depois |
|---|---|---|
| 0005 | Cache sempre ligado | Sem cache por padrão, com cache configurável por rota ou tabela |
| 0011 | Isolamento só no código | Row-level security |
| 0012 | Npgsql direto | Dapper |

## Resultado

${table(['ADR', 'Decisão', 'Escolhida', 'Nota', 'Melhor alternativa', 'Nota', 'Margem', 'Eliminadas'], analysis.map((item) => [
  link(item.adr), item.adr.title, item.chosen.name, decimal(weighted(item.adr, item.chosen)), item.best?.name ?? 'Nenhuma restante', item.best ? decimal(weighted(item.adr, item.best)) : '—', item.margin === null ? '—' : decimal(item.margin), item.eliminated,
]))}

${eliminated} opções foram eliminadas por veto.

**Decisões apertadas**, com margem menor que 0,25: ${list(close)}. Nesses casos, a defesa vem do argumento, e não do gráfico.

**Sem o critério de negócio**, a escolhida deixa de vencer em: ${list(analysis.filter((item) => !item.withoutBusiness))}.

**Com pesos iguais**, a escolhida deixa de vencer em: ${list(analysis.filter((item) => !item.equalWeights))}.

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

A primeira avaliação usava sete pilares fixos, três opções e notas por julgamento, sem régua prévia. Na soma simples, a opção escolhida perdia em 4 decisões e empatava em 1, e a escolhida tinha média maior em todos os pilares, o que indicava viés. As notas originais estão em \`docs/adr/radar/historico/avaliacao-original.json\`.

## Como rever

1. Altere \`docs/adr/radar/data.json\`. Se for um ajuste de julgamento, registre em \`adjustments\`, com o motivo.
2. Rode \`node tools/radar/generate.mjs\` e \`node tools/radar/method.mjs\`.
`;

const evaluation = [
  '# Avaliação dos radares',
  '',
  'Este documento guarda a evidência de cada nota. O método está em [método dos radares](20-metodo-dos-radares.md). Este arquivo é gerado.',
  ...adrs.flatMap((adr) => [
    '',
    `## ${adr.id}`,
    '',
    `**${adr.title}.** [Ler o ADR](adr/${adr.file}).`,
    '',
    table(['Critério', 'Peso', 'Por que este peso'], adr.criteria.map((criterion) => [criterion.name, criterion.weight, inline(criterion.why)])),
    ...adr.criteria.filter((criterion) => criterion.questions).flatMap((criterion) => ['', `Perguntas de ${criterion.name}: ${inline(JSON.stringify(criterion.questions))}`]),
    ...adr.options.flatMap((option, index) => [
      '',
      `### ${option.name}${index === 0 ? ' (escolhida)' : ''}${option.vetoes.length ? ' (eliminada)' : ''}`,
      '',
      table(['Critério', 'Nota', 'Evidência'], adr.criteria.map((criterion, i) => [
        criterion.name,
        option.vetoes.includes(criterion.name) ? 'Veto' : option.scores[i],
        inline(option.evidence[i]),
      ])),
    ]),
  ]),
  '',
].join('\n');

writeFileSync(join(docsDir, '20-metodo-dos-radares.md'), method);
writeFileSync(join(docsDir, '21-avaliacao-dos-radares.md'), evaluation);
console.log({ options, eliminated, close: list(close), withoutBusiness: list(analysis.filter((i) => !i.withoutBusiness)), equalWeights: list(analysis.filter((i) => !i.equalWeights)) });
