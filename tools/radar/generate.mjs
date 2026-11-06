// Generates the ADR radar charts from docs/adr/radar/data.json.
// For each ADR, writes the SVG and refreshes the score table between the markers.
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const adrDir = join(root, 'docs', 'adr');
const { adrs } = JSON.parse(readFileSync(join(adrDir, 'radar', 'data.json'), 'utf8'));

const SURFACE = '#fcfcfb';
const TEXT = '#0b0b0b';
const TEXT_SECONDARY = '#52514e';
const GRID = '#d9d8d3';
const SERIES = [
  { color: '#2a78d6', dash: '', marker: 'circle' },
  { color: '#eb6834', dash: '7 4', marker: 'square' },
  { color: '#1baf7a', dash: '2 4', marker: 'diamond' },
  { color: '#8a5cd6', dash: '10 3 2 3', marker: 'triangle' },
];
const WIDTH = 760;
const CENTER = { x: 380, y: 245 };
const RADIUS = 150;
const MAX_SCORE = 5;

const point = (index, score, count) => {
  const angle = -Math.PI / 2 + (2 * Math.PI * index) / count;
  const radius = (RADIUS * score) / MAX_SCORE;
  return { x: CENTER.x + radius * Math.cos(angle), y: CENTER.y + radius * Math.sin(angle), cos: Math.cos(angle) };
};
const fixed = (n) => n.toFixed(1);
const polygon = (points) => points.map((p) => `${fixed(p.x)},${fixed(p.y)}`).join(' ');
const escapeXml = (text) => text.replaceAll('&', '&amp;').replaceAll('<', '&lt;');

function marker({ marker: shape, color }, x, y, title) {
  const shared = `fill="${color}" stroke="${SURFACE}" stroke-width="2"`;
  const tooltip = title ? `<title>${escapeXml(title)}</title>` : '';
  if (shape === 'triangle') {
    return `<polygon points="${fixed(x)},${fixed(y - 7)} ${fixed(x + 7)},${fixed(y + 6)} ${fixed(x - 7)},${fixed(y + 6)}" ${shared}>${tooltip}</polygon>`;
  }
  if (shape === 'square') return `<rect x="${fixed(x - 5)}" y="${fixed(y - 5)}" width="10" height="10" ${shared}>${tooltip}</rect>`;
  if (shape === 'diamond') {
    return `<polygon points="${fixed(x)},${fixed(y - 7)} ${fixed(x + 7)},${fixed(y)} ${fixed(x)},${fixed(y + 7)} ${fixed(x - 7)},${fixed(y)}" ${shared}>${tooltip}</polygon>`;
  }
  return `<circle cx="${fixed(x)}" cy="${fixed(y)}" r="5.5" ${shared}>${tooltip}</circle>`;
}

const isEliminated = (option) => option.vetoes.length > 0;

const average = (adr, option) =>
  adr.criteria.reduce((total, criterion, i) => total + criterion.weight * option.scores[i], 0) /
  adr.criteria.reduce((total, criterion) => total + criterion.weight, 0);

function renderSvg(adr) {
  const names = adr.criteria.map((criterion) => criterion.name);
  const count = names.length;
  const plotted = adr.options.filter((option) => !isEliminated(option));
  const eliminated = adr.options.filter(isEliminated);
  const legendTop = 465;
  const height = legendTop + plotted.length * 26 + (eliminated.length ? 26 : 0) + 8;
  const parts = [
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${WIDTH} ${height}" role="img" aria-label="${escapeXml(adr.title)}" font-family="-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif">`,
    `<title>${escapeXml(adr.id)}: ${escapeXml(adr.title)}</title>`,
    `<rect width="${WIDTH}" height="${height}" fill="${SURFACE}"/>`,
    `<text x="24" y="30" font-size="16" font-weight="600" fill="${TEXT}">${escapeXml(adr.title)}</text>`,
    `<text x="24" y="50" font-size="12" fill="${TEXT_SECONDARY}">Quanto mais longe do centro, melhor</text>`,
  ];
  for (let score = 1; score <= MAX_SCORE; score += 1) {
    parts.push(`<polygon points="${polygon(names.map((_, i) => point(i, score, count)))}" fill="none" stroke="${GRID}" stroke-width="1"/>`);
    const top = point(0, score, count);
    parts.push(`<text x="${fixed(top.x + 10)}" y="${fixed(top.y + 12)}" font-size="10" fill="${TEXT_SECONDARY}">${score}</text>`);
  }
  names.forEach((name, i) => {
    const edge = point(i, MAX_SCORE, count);
    const label = point(i, MAX_SCORE + 0.4, count);
    const anchor = Math.abs(label.cos) < 0.1 ? 'middle' : label.cos > 0 ? 'start' : 'end';
    const weight = adr.criteria[i].weight;
    parts.push(`<line x1="${CENTER.x}" y1="${CENTER.y}" x2="${fixed(edge.x)}" y2="${fixed(edge.y)}" stroke="${GRID}" stroke-width="1"/>`);
    parts.push(`<text x="${fixed(label.x)}" y="${fixed(label.y + 4)}" text-anchor="${anchor}" font-size="12" fill="${TEXT}">${escapeXml(name)} (${weight})</text>`);
  });
  [...plotted].reverse().forEach((option) => {
    const series = SERIES[plotted.indexOf(option)];
    const points = option.scores.map((score, i) => point(i, score, count));
    const dash = series.dash ? ` stroke-dasharray="${series.dash}"` : '';
    parts.push(`<polygon points="${polygon(points)}" fill="${series.color}" fill-opacity="0.08" stroke="${series.color}" stroke-width="2" stroke-linejoin="round"${dash}/>`);
    points.forEach((p, i) => parts.push(marker(series, p.x, p.y, `${option.name}, ${names[i]}: ${option.scores[i]}`)));
  });
  plotted.forEach((option, i) => {
    const series = SERIES[i];
    const y = legendTop + i * 26;
    const dash = series.dash ? ` stroke-dasharray="${series.dash}"` : '';
    parts.push(`<line x1="24" y1="${y}" x2="60" y2="${y}" stroke="${series.color}" stroke-width="2"${dash}/>`);
    parts.push(marker(series, 42, y));
    const suffix = i === 0 ? ' (escolhida)' : '';
    parts.push(`<text x="70" y="${y + 4}" font-size="13" fill="${TEXT}">${escapeXml(option.name)}${suffix}: ${average(adr, option).toFixed(2).replace('.', ',')}</text>`);
  });
  if (eliminated.length) {
    const y = legendTop + plotted.length * 26;
    parts.push(`<text x="24" y="${y + 4}" font-size="12" fill="${TEXT_SECONDARY}">Eliminadas por veto: ${escapeXml(eliminated.map((option) => option.name).join(', '))}</text>`);
  }
  parts.push('</svg>');
  return `${parts.join('\n')}\n`;
}

function renderTable(adr) {
  const header = `| Critério | Peso | ${adr.options.map((o, i) => (i === 0 ? `**${o.name}**` : o.name)).join(' | ')} |`;
  const separator = `|---|---:|${adr.options.map(() => '---:').join('|')}|`;
  const rows = adr.criteria.map((criterion, i) => {
    const cells = adr.options.map((o) => (o.vetoes.includes(criterion.name) ? 'Veto' : o.scores[i]));
    return `| ${criterion.name} | ${criterion.weight} | ${cells.join(' | ')} |`;
  });
  const result = adr.options.map((o) => (isEliminated(o) ? 'Eliminada' : `**${average(adr, o).toFixed(2).replace('.', ',')}**`));
  return [header, separator, ...rows, `| **Resultado** | | ${result.join(' | ')} |`].join('\n');
}

for (const adr of adrs) {
  for (const option of adr.options) {
    if (option.scores.length !== adr.criteria.length) throw new Error(`${adr.id}: ${option.name} must have one score per criterion`);
  }
  writeFileSync(join(adrDir, 'radar', `${adr.id}.svg`), renderSvg(adr));
  const file = join(adrDir, adr.file);
  const text = readFileSync(file, 'utf8');
  const evidence = `As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#${adr.id.toLowerCase()}).`;
  const block = `<!-- radar:start -->\n![Radar de ${adr.title}](radar/${adr.id}.svg)\n\n${renderTable(adr)}\n\n${evidence}\n<!-- radar:end -->`;
  const updated = text.replace(/<!-- radar:start -->[\s\S]*?<!-- radar:end -->/, block);
  if (updated === text && !text.includes(block)) throw new Error(`${adr.file}: radar markers not found`);
  writeFileSync(file, updated);
}
console.log(`${adrs.length} radars generated`);
