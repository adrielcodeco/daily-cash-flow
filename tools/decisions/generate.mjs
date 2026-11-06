// Generates docs/16-resumo-de-decisoes.md from the ADR index and the decision register.
// Usage: node tools/decisions/generate.mjs
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const docsDir = join(dirname(fileURLToPath(import.meta.url)), '..', '..', 'docs');
const read = (path) => readFileSync(join(docsDir, path), 'utf8');
const cells = (row) => row.split('|').slice(1, -1).map((cell) => cell.trim());

const adrs = read('adr/README.md')
  .split('\n')
  .filter((line) => /^\| \[\d{4}\]/.test(line))
  .map((line) => {
    const [link, topic, choice] = cells(line);
    const [, id, file] = link.match(/\[(\d{4})\]\((.*)\)/);
    return `| [ADR-${id}](adr/${file}) | ${topic} | ${choice} |`;
  });

const groups = read('15-registro-de-decisoes.md')
  .split(/^## /m)
  .slice(1)
  .map((section) => {
    const [title, ...lines] = section.split('\n');
    const rows = lines.filter((line) => line.startsWith('| D-')).map((line) => {
      const [id, decision] = cells(line);
      return `| ${id} | ${decision} |`;
    });
    return { title, rows };
  })
  .filter((group) => group.rows.length > 0);

const summary = [
  '# Resumo das decisões',
  '',
  'Lista do que ficou decidido, sem os trade-offs. Os motivos estão nos [ADRs](adr/README.md) e no [registro de decisões](15-registro-de-decisoes.md).',
  '',
  'Este arquivo é gerado. Para atualizar, edite as fontes e rode `node tools/decisions/generate.mjs`.',
  '',
  '## Arquitetura',
  '',
  '| ADR | Tema | Decisão |',
  '|---|---|---|',
  ...adrs,
  ...groups.flatMap((group) => ['', `## ${group.title}`, '', '| ID | Decisão |', '|---|---|', ...group.rows]),
  '',
].join('\n');

writeFileSync(join(docsDir, '16-resumo-de-decisoes.md'), summary);
console.log(`${adrs.length} ADRs and ${groups.reduce((total, group) => total + group.rows.length, 0)} register decisions`);
