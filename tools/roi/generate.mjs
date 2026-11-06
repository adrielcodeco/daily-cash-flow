// Generates docs/19-roi-e-custo-operacional.md from the assumptions below.
// Usage: node tools/roi/generate.mjs
import { writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const target = join(dirname(fileURLToPath(import.meta.url)), '..', '..', 'docs', '19-roi-e-custo-operacional.md');

// Infrastructure totals come from docs/05-custos.md.
const INFRA_START_USD = 1254.20;
const INFRA_GROWTH_USD = 3556.07;
const REQUESTS_START = 13_000_000;
const REQUESTS_GROWTH = 130_000_000;

const assumptions = {
  usdBrl: 5.5,
  requestsPerMerchant: 1_000,
  personMonth: 30_000,
  buildPeople: 5.5,
  buildMonths: 4,
  launchCosts: 100_000,
  nonProductionShare: 0.4,
  supportShare: 0.1,
  teamPeople: 2,
  onCallMonthly: 10_000,
  discountYearly: 0.15,
  horizonMonths: 36,
};

const scenarios = [
  { name: 'Conservador', fee: 9.9, merchants: 6_000 },
  { name: 'Base', fee: 19.9, merchants: 13_000 },
  { name: 'Otimista', fee: 29.9, merchants: 30_000 },
];
const RAMP_MONTHS = 24;

const a = assumptions;
const brl = (value, digits = 0) =>
  `R$ ${value.toLocaleString('pt-BR', { minimumFractionDigits: digits, maximumFractionDigits: digits })}`;
const count = (value) => Math.round(value).toLocaleString('pt-BR');
const percent = (value) => `${(value * 100).toLocaleString('pt-BR', { maximumFractionDigits: 0 })}%`;
const table = (head, rows, align) => [
  `| ${head.join(' | ')} |`,
  `|${head.map((_, index) => (align?.[index] ? '---:' : '---')).join('|')}|`,
  ...rows.map((row) => `| ${row.join(' | ')} |`),
].join('\n');

const merchantsStart = REQUESTS_START / a.requestsPerMerchant;
const merchantsGrowth = REQUESTS_GROWTH / a.requestsPerMerchant;
const variableUsd = (INFRA_GROWTH_USD - INFRA_START_USD) / (merchantsGrowth - merchantsStart);
const variablePerMerchant = variableUsd * a.usdBrl;
const fixedInfraUsd = INFRA_START_USD - variableUsd * merchantsStart;

const buildTeam = a.buildPeople * a.buildMonths * a.personMonth;
const buildInfra = INFRA_START_USD * a.usdBrl * a.nonProductionShare * a.buildMonths;
const investment = buildTeam + buildInfra + a.launchCosts;

// Infrastructure scales with merchants; the extra environment and support follow it.
const infraMonthly = (merchants) => (fixedInfraUsd + variableUsd * merchants) * a.usdBrl;
const operating = (merchants) => {
  const production = infraMonthly(merchants);
  const nonProduction = INFRA_START_USD * a.usdBrl * a.nonProductionShare;
  const support = (production + nonProduction) * a.supportShare;
  const team = a.teamPeople * a.personMonth;
  return { production, nonProduction, support, team, onCall: a.onCallMonthly, total: production + nonProduction + support + team + a.onCallMonthly };
};

const fixedMonthly = operating(0).total;
const variableWithSupport = variablePerMerchant * (1 + a.supportShare);
const breakEven = (fee) => fixedMonthly / (fee - variableWithSupport);

const simulate = ({ fee, merchants }) => {
  const monthlyRate = (1 + a.discountYearly) ** (1 / 12) - 1;
  let cash = -investment;
  let presentValue = -investment;
  let totalCost = investment;
  let totalRevenue = 0;
  let payback = null;
  let firstProfit = null;
  for (let month = 1; month <= a.horizonMonths; month += 1) {
    const active = merchants * Math.min(month / RAMP_MONTHS, 1);
    const revenue = active * fee;
    const cost = operating(active).total;
    const result = revenue - cost;
    cash += result;
    presentValue += result / (1 + monthlyRate) ** month;
    totalCost += cost;
    totalRevenue += revenue;
    if (firstProfit === null && result >= 0) firstProfit = month;
    if (payback === null && cash >= 0) payback = month;
  }
  return { cash, presentValue, payback, firstProfit, roi: (totalRevenue - totalCost) / totalCost };
};

const start = operating(merchantsStart);
const growth = operating(merchantsGrowth);
const costRow = (label, key) => [label, brl(start[key]), brl(growth[key]), percent(start[key] / start.total)];
const results = scenarios.map((scenario) => ({ ...scenario, ...simulate(scenario) }));
const noPayback = 'Não ocorre em 36 meses';
const monthLabel = (value) => (value === null ? noPayback : `Mês ${value}`);

const sensitivity = [
  ['Equipe de operação com 1 pessoa', { teamPeople: 1 }],
  ['Equipe de operação com 4 pessoas', { teamPeople: 4 }],
  ['Custo por pessoa de R$ 20.000', { personMonth: 20_000 }],
  ['Custo por pessoa de R$ 45.000', { personMonth: 45_000 }],
  ['Infraestrutura com o dobro do custo', { infraFactor: 2 }],
].map(([label, change]) => {
  const team = (change.teamPeople ?? a.teamPeople) * (change.personMonth ?? a.personMonth);
  const fixed = fixedMonthly - a.teamPeople * a.personMonth + team + (change.infraFactor ? operating(0).production + operating(0).nonProduction : 0) * (1 + a.supportShare);
  return [label, count(fixed / (19.9 - variableWithSupport))];
});

const doc = `# ROI, ponto de equilíbrio e custo operacional

Este documento estima quanto custa construir e operar a solução, quantos comerciantes pagam essa conta e em quanto tempo o investimento volta.

**Os resultados são um modelo, e não uma previsão.** Só o custo de infraestrutura vem de preços pesquisados, nos [custos](05-custos.md). Equipe, câmbio, preço por comerciante e adoção são premissas sem dados do cliente, escritas para serem trocadas. Este arquivo é gerado: edite as premissas em \`tools/roi/generate.mjs\` e rode \`node tools/roi/generate.mjs\`.

## Conclusões

- **A equipe é o principal custo, e não a nuvem.** No cenário Início, ela representa ${percent(start.team / start.total)} do custo mensal. Otimizar a infraestrutura muda pouco o resultado.
- **Ponto de equilíbrio mensal:** cerca de ${count(breakEven(19.9))} comerciantes, a ${brl(19.9, 2)} por mês.
- **Custo adicional por comerciante:** cerca de ${brl(variablePerMerchant, 2)} por mês. Depois do ponto de equilíbrio, quase toda a receita vira margem.
- **Retorno no cenário Base:** ${monthLabel(results[1].payback).toLowerCase()}, com ROI de ${percent(results[1].roi)} em ${a.horizonMonths} meses.
- **O cenário Conservador não se paga em ${a.horizonMonths} meses.** O projeto depende de adoção e preço, e não de tecnologia.

## Premissas

${table(['Premissa', 'Valor', 'Base'], [
  ['Câmbio', `${brl(a.usdBrl, 2)} por dólar`, 'Premissa. Atualizar com a cotação do dia'],
  ['Requisições por comerciante por mês', count(a.requestsPerMerchant), 'Premissa. Liga os cenários de custo ao número de comerciantes'],
  ['Custo mensal por pessoa, com encargos', brl(a.personMonth), 'Premissa'],
  ['Equipe de construção', `${a.buildPeople.toLocaleString('pt-BR')} pessoas por ${a.buildMonths} meses`, 'Premissa, para levar o projeto a produção'],
  ['Lançamento', brl(a.launchCosts), 'Premissa: pentest, conformidade e migração'],
  ['Ambiente de não produção', `${percent(a.nonProductionShare)} do cenário Início`, 'Premissa'],
  ['Suporte do provedor', `${percent(a.supportShare)} da infraestrutura`, 'Premissa. Confirmar o plano contratado'],
  ['Equipe de operação', `${a.teamPeople} pessoas`, 'Premissa'],
  ['Plantão', `${brl(a.onCallMonthly)} por mês`, 'Premissa'],
  ['Adoção', `Linear por ${RAMP_MONTHS} meses, depois estável`, 'Premissa'],
  ['Taxa de desconto', `${percent(a.discountYearly)} ao ano`, 'Premissa. Usar a taxa do cliente'],
])}

A receita pode ser uma mensalidade ou um valor equivalente. Se o serviço for gratuito, o mesmo cálculo vale com o benefício mensal por comerciante, como retenção de clientes ou redução de atendimento.

## Investimento inicial

${table(['Item', 'Valor'], [
  ['Equipe de construção', brl(buildTeam)],
  ['Ambiente durante a construção', brl(buildInfra)],
  ['Lançamento', brl(a.launchCosts)],
  ['**Total**', `**${brl(investment)}**`],
], [false, true])}

## Custo operacional mensal

${table(['Item', `Início: ${count(merchantsStart)} comerciantes`, `Crescimento: ${count(merchantsGrowth)} comerciantes`, 'Peso no Início'], [
  costRow('Infraestrutura de produção', 'production'),
  costRow('Ambiente de não produção', 'nonProduction'),
  costRow('Suporte do provedor', 'support'),
  costRow('Equipe de operação', 'team'),
  costRow('Plantão', 'onCall'),
  ['**Total**', `**${brl(start.total)}**`, `**${brl(growth.total)}**`, '**100%**'],
], [false, true, true, true])}

${table(['Custo unitário', 'Início', 'Crescimento'], [
  ['Por comerciante, por mês', brl(start.total / merchantsStart, 2), brl(growth.total / merchantsGrowth, 2)],
  ['Só infraestrutura, por mil requisições', brl(start.production / (REQUESTS_START / 1000), 2), brl(growth.production / (REQUESTS_GROWTH / 1000), 2)],
], [false, true, true])}

O volume cresce 10 vezes, e o custo total cresce bem menos, porque a equipe fica igual. Esse modelo supõe que a mesma equipe sustente o cenário Crescimento, o que precisa ser validado.

## Ponto de equilíbrio

O ponto de equilíbrio mensal é o número de comerciantes em que a receita cobre o custo do mês:

\`\`\`text
comerciantes = custo fixo mensal / (preço - custo variável por comerciante)
\`\`\`

Custo fixo mensal: ${brl(fixedMonthly)}. Custo variável por comerciante, com suporte: ${brl(variableWithSupport, 2)}.

${table(['Preço mensal', 'Comerciantes no equilíbrio'], [9.9, 19.9, 29.9, 49.9].map((fee) => [brl(fee, 2), count(breakEven(fee))]), [false, true])}

### Sensibilidade

Comerciantes necessários a ${brl(19.9, 2)}, mudando uma premissa de cada vez:

${table(['Mudança', 'Comerciantes no equilíbrio'], [['Modelo base', count(breakEven(19.9))], ...sensitivity], [false, true])}

Dobrar a infraestrutura pesa menos que acrescentar pessoas à operação.

## ROI

\`\`\`text
ROI = (receita acumulada - custo acumulado) / custo acumulado
\`\`\`

O custo acumulado inclui o investimento inicial. O período é de ${a.horizonMonths} meses depois do lançamento.

${table(['Cenário', 'Preço', 'Comerciantes ao fim da adoção', 'Mês com lucro', 'Retorno do investimento', 'Saldo em 36 meses', 'VPL', 'ROI'], results.map((result) => [
  result.name, brl(result.fee, 2), count(result.merchants), monthLabel(result.firstProfit), monthLabel(result.payback), brl(result.cash), brl(result.presentValue), percent(result.roi),
]), [false, true, true, false, false, true, true, true])}

## O que fora do modelo pode mudar o resultado

| Item | Efeito |
|---|---|
| Impostos e custo de aquisição de clientes | Reduzem a margem |
| Cancelamento de clientes | A adoção linear ignora a saída de comerciantes |
| Descontos por compromisso no GCP | Reduzem a infraestrutura |
| Incidentes e multas | Não estão no custo. As vulnerabilidades abertas aumentam esse risco |
| Fim do legado, em uma migração | Pode gerar economia, se a solução substituir um sistema |

## Como reduzir o custo

| Ação | Efeito estimado | Trade-off |
|---|---|---|
| Automatizar a operação | Maior alavanca, porque ataca a equipe | Exige investimento em plataforma |
| Reduzir o Cloud SQL do consolidado | Menor custo de infraestrutura | Recuperação mais lenta |
| Descontos por compromisso | Menor custo de infraestrutura | Compromisso de uso |
| Remover o Redis | Cerca de US$ 64 por mês no Início | Menos proteção do banco em volumes maiores |
`;

writeFileSync(target, doc);
console.log({ investment, fixedMonthly, startTotal: start.total, growthTotal: growth.total, variablePerMerchant, breakEven: breakEven(19.9) });
console.log(results.map(({ name, payback, firstProfit, cash, presentValue, roi }) => ({ name, payback, firstProfit, cash: Math.round(cash), npv: Math.round(presentValue), roi: +roi.toFixed(2) })));
console.log(sensitivity);
