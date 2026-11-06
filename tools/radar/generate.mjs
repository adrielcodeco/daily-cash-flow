// Generates the ADR radar charts from docs/adr/radar/data.json.
// For each ADR, writes the SVG and refreshes the score table between the markers.
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const adrDir = join(root, 'docs', 'adr');
const { pillars, adrs } = JSON.parse(readFileSync(join(adrDir, 'radar', 'data.json'), 'utf8'));

const SURFACE = '#fcfcfb';
const TEXT = '#0b0b0b';
const TEXT_SECONDARY = '#52514e';
const GRID = '#d9d8d3';
const SERIES = [
  { color: '#2a78d6', dash: '', marker: 'circle' },
  { color: '#eb6834', dash: '7 4', marker: 'square' },
  { color: '#1baf7a', dash: '2 4', marker: 'diamond' },
];
const WIDTH = 680;
const CENTER = { x: 340, y: 235 };
const RADIUS = 165;
const MAX_SCORE = 5;

const point = (index, score) => {
  const angle = -Math.PI / 2 + (2 * Math.PI * index) / pillars.length;
  const radius = (RADIUS * score) / MAX_SCORE;
  return { x: CENTER.x + radius * Math.cos(angle), y: CENTER.y + radius * Math.sin(angle), cos: Math.cos(angle) };
};
const fixed = (n) => n.toFixed(1);
const polygon = (points) => points.map((p) => `${fixed(p.x)},${fixed(p.y)}`).join(' ');
const escapeXml = (text) => text.replaceAll('&', '&amp;').replaceAll('<', '&lt;');

function marker({ marker: shape, color }, x, y, title) {
  const shared = `fill="${color}" stroke="${SURFACE}" stroke-width="2"`;
  const tooltip = title ? `<title>${escapeXml(title)}</title>` : '';
  if (shape === 'square') return `<rect x="${fixed(x - 5)}" y="${fixed(y - 5)}" width="10" height="10" ${shared}>${tooltip}</rect>`;
  if (shape === 'diamond') {
    return `<polygon points="${fixed(x)},${fixed(y - 7)} ${fixed(x + 7)},${fixed(y)} ${fixed(x)},${fixed(y + 7)} ${fixed(x - 7)},${fixed(y)}" ${shared}>${tooltip}</polygon>`;
  }
  return `<circle cx="${fixed(x)}" cy="${fixed(y)}" r="5.5" ${shared}>${tooltip}</circle>`;
}

function renderSvg(adr) {
  const height = 450 + adr.options.length * 26;
  const parts = [
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${WIDTH} ${height}" role="img" aria-label="${escapeXml(adr.title)}" font-family="-apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif">`,
    `<title>${escapeXml(adr.id)}: ${escapeXml(adr.title)}</title>`,
    `<rect width="${WIDTH}" height="${height}" fill="${SURFACE}"/>`,
    `<text x="24" y="30" font-size="16" font-weight="600" fill="${TEXT}">${escapeXml(adr.title)}</text>`,
  ];
  for (let score = 1; score <= MAX_SCORE; score += 1) {
    parts.push(`<polygon points="${polygon(pillars.map((_, i) => point(i, score)))}" fill="none" stroke="${GRID}" stroke-width="1"/>`);
    const top = point(0, score);
    parts.push(`<text x="${fixed(top.x + 10)}" y="${fixed(top.y + 12)}" font-size="10" fill="${TEXT_SECONDARY}">${score}</text>`);
  }
  pillars.forEach((pillar, i) => {
    const edge = point(i, MAX_SCORE);
    const label = point(i, MAX_SCORE + 0.45);
    const anchor = Math.abs(label.cos) < 0.1 ? 'middle' : label.cos > 0 ? 'start' : 'end';
    parts.push(`<line x1="${CENTER.x}" y1="${CENTER.y}" x2="${fixed(edge.x)}" y2="${fixed(edge.y)}" stroke="${GRID}" stroke-width="1"/>`);
    parts.push(`<text x="${fixed(label.x)}" y="${fixed(label.y + 4)}" text-anchor="${anchor}" font-size="13" fill="${TEXT}">${escapeXml(pillar)}</text>`);
  });
  [...adr.options].reverse().forEach((option) => {
    const series = SERIES[adr.options.indexOf(option)];
    const points = option.scores.map((score, i) => point(i, score));
    const dash = series.dash ? ` stroke-dasharray="${series.dash}"` : '';
    parts.push(`<polygon points="${polygon(points)}" fill="${series.color}" fill-opacity="0.08" stroke="${series.color}" stroke-width="2" stroke-linejoin="round"${dash}/>`);
    points.forEach((p, i) => parts.push(marker(series, p.x, p.y, `${option.name}, ${pillars[i]}: ${option.scores[i]}`)));
  });
  adr.options.forEach((option, i) => {
    const series = SERIES[i];
    const y = 450 + i * 26;
    const dash = series.dash ? ` stroke-dasharray="${series.dash}"` : '';
    parts.push(`<line x1="24" y1="${y}" x2="60" y2="${y}" stroke="${series.color}" stroke-width="2"${dash}/>`);
    parts.push(marker(series, 42, y));
    const suffix = i === 0 ? ' (escolhida)' : '';
    parts.push(`<text x="70" y="${y + 4}" font-size="13" fill="${TEXT}">${escapeXml(option.name)}${suffix}</text>`);
  });
  parts.push('</svg>');
  return `${parts.join('\n')}\n`;
}

function renderTable(adr) {
  const header = `| Pilar | ${adr.options.map((o, i) => (i === 0 ? `**${o.name}**` : o.name)).join(' | ')} |`;
  const separator = `|---|${adr.options.map(() => '---:').join('|')}|`;
  const rows = pillars.map((pillar, i) => `| ${pillar} | ${adr.options.map((o) => o.scores[i]).join(' | ')} |`);
  return [header, separator, ...rows].join('\n');
}

for (const adr of adrs) {
  for (const option of adr.options) {
    if (option.scores.length !== pillars.length) throw new Error(`${adr.id}: ${option.name} must have one score per pillar`);
  }
  writeFileSync(join(adrDir, 'radar', `${adr.id}.svg`), renderSvg(adr));
  const file = join(adrDir, adr.file);
  const text = readFileSync(file, 'utf8');
  const block = `<!-- radar:start -->\n![Radar de ${adr.title}](radar/${adr.id}.svg)\n\n${renderTable(adr)}\n<!-- radar:end -->`;
  const updated = text.replace(/<!-- radar:start -->[\s\S]*?<!-- radar:end -->/, block);
  if (updated === text && !text.includes(block)) throw new Error(`${adr.file}: radar markers not found`);
  writeFileSync(file, updated);
}
console.log(`${adrs.length} radars generated`);
