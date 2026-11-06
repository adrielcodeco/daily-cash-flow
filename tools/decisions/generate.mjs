// Generates docs/16-resumo-de-decisoes.md from every source that records a decision.
// Usage: node tools/decisions/generate.mjs
import { readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const read = (path) => readFileSync(join(root, path), 'utf8');
const cells = (row) => row.split('|').slice(1, -1).map((cell) => cell.trim());
const rowsStarting = (text, prefix) => text.split('\n').filter((line) => line.startsWith(prefix)).map(cells);
const sectionOf = (text, title) => text.split(/^## /m).find((section) => section.startsWith(`${title}\n`)) ?? '';
const table = (head, rows) => [`| ${head.join(' | ')} |`, `|${head.map(() => '---').join('|')}|`, ...rows.map((row) => `| ${row.join(' | ')} |`)];
const section = (title, head, rows) => ['', `## ${title}`, '', ...table(head, rows)];

const ruleHeadings = (text) =>
  [...text.matchAll(/^### ([A-Z]{5}-R\d+) — (.+?)(?: @.*)?$/gm)].map(([, id, rule]) => [id, rule]);

const filesEndingWith = (dir, suffix) =>
  readdirSync(join(root, dir), { recursive: true })
    .filter((file) => file.endsWith(suffix) && !/(^|\/)(bin|obj)\//.test(file))
    .sort()
    .map((file) => join(dir, file));

const principles = read('docs/00-principios-e-glossario.md');
const requirements = read('docs/02-requisitos.md');
const domains = read('docs/01-dominios-e-capacidades.md');
const contracts = read('docs/contratos/README.md');

const adrs = rowsStarting(read('docs/adr/README.md'), '| [0').map(([link, topic, choice]) => {
  const [, id, file] = link.match(/\[(\d{4})\]\((.*)\)/);
  return [`[ADR-${id}](adr/${file})`, topic, choice];
});

const register = read('docs/15-registro-de-decisoes.md')
  .split(/^## /m)
  .slice(1)
  .map((text) => ({ title: text.split('\n')[0], rows: rowsStarting(text, '| D-').map(([id, decision]) => [id, decision]) }))
  .filter((group) => group.rows.length > 0);

const revised = rowsStarting(sectionOf(read('docs/15-registro-de-decisoes.md'), 'Decisões revistas'), '| 2').map(([date, from, to]) => [date, from, to]);
const doctrine = filesEndingWith('product', '.doctrine.md').flatMap((file) => ruleHeadings(read(file)));
const specRules = filesEndingWith('services', '.spec.md').flatMap((file) => ruleHeadings(read(file)));
const bullets = (text) => text.split('\n').filter((line) => line.startsWith('- ')).map((line) => [line.slice(2)]);

const groups = [
  section('Princípios', ['ID', 'Decisão'], rowsStarting(principles, '| P').filter(([id]) => /^P\d+$/.test(id)).map(([id, text]) => [id, text])),
  section('Premissas', ['ID', 'Decisão'], rowsStarting(requirements, '| PRE-').map(([id, text]) => [id, text])),
  section('Metas de qualidade', ['ID', 'Atributo', 'Meta', 'Origem'], rowsStarting(requirements, '| RNF-').map(([id, attribute, goal, , origin]) => [id, attribute, goal, origin])),
  section('Fora de escopo', ['Item'], bullets(sectionOf(requirements, 'Fora de escopo'))),
  section('Deixado para depois', ['Evolução', 'Quando fazer'], rowsStarting(read('docs/08-evolucoes-futuras.md'), '| ').slice(1).filter(([name]) => !/^-+$/.test(name))),
  section('Arquitetura', ['ADR', 'Tema', 'Decisão'], adrs),
  ...register.map((group) => section(group.title, ['ID', 'Decisão'], group.rows)),
  section('Regras de negócio', ['ID', 'Regra'], rowsStarting(domains, '| RN-').map(([id, text]) => [id, text])),
  section('Doutrina do produto', ['ID', 'Regra'], doctrine),
  section('Regras dos casos de uso', ['ID', 'Regra'], specRules),
  section('Versionamento dos contratos', ['Regra'], bullets(sectionOf(contracts, 'Regras de versionamento'))),
  section('Decisões revistas', ['Data', 'Antes', 'Depois'], revised),
];

const total = groups.reduce((sum, group) => sum + group.length - 5, 0);
const summary = [
  '# Resumo das decisões',
  '',
  `Lista de tudo o que ficou decidido, com ${total} itens, sem os trade-offs. Os motivos estão nos [ADRs](adr/README.md) e no [registro de decisões](15-registro-de-decisoes.md).`,
  '',
  'Este arquivo é gerado. Para atualizar, edite as fontes e rode `node tools/decisions/generate.mjs`.',
  ...groups.flat(),
  '',
].join('\n');

writeFileSync(join(root, 'docs', '16-resumo-de-decisoes.md'), summary);
console.log(groups.map((group) => `${group[1].slice(3)}: ${group.length - 5}`).join('\n'));
console.log(`total: ${total}`);
