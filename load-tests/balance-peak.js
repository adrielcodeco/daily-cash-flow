// Peak scenario from the challenge: 50 balance requests per second, with at most 5% lost.
// Entries keep arriving during the test, so the cache is invalidated as in real traffic.
import http from 'k6/http';
import { check } from 'k6';

const LEDGER = __ENV.LEDGER_URL || 'http://localhost:18081';
const BALANCES = __ENV.BALANCES_URL || 'http://localhost:18082';
const DATE = __ENV.DATE;
const DURATION = __ENV.DURATION || '5m';
const MERCHANTS = 50;
const READS_PER_SECOND = 50;

export const options = {
  scenarios: {
    reads: {
      executor: 'constant-arrival-rate',
      exec: 'readBalance',
      rate: READS_PER_SECOND,
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: 50,
      maxVUs: 300,
    },
    writes: {
      executor: 'constant-arrival-rate',
      exec: 'writeEntry',
      rate: 10,
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: 10,
      maxVUs: 50,
    },
  },
  thresholds: {
    'http_req_failed{scenario:reads}': ['rate<0.05'],
    'http_req_duration{scenario:reads}': ['p(95)<200'],
    'http_req_failed{scenario:writes}': ['rate<0.01'],
    'http_req_duration{scenario:writes}': ['p(95)<300'],
  },
};

function uuid() {
  return 'xxxxxxxx-xxxx-4xxx-8xxx-xxxxxxxxxxxx'.replace(/x/g, () => Math.floor(Math.random() * 16).toString(16));
}

function postEntry(merchant) {
  return http.post(
    `${LEDGER}/v1/entries`,
    JSON.stringify({ entryType: 'CREDIT', amountCents: 100, accountingDate: DATE }),
    { headers: { 'Content-Type': 'application/json', 'X-Merchant-Id': merchant, 'Idempotency-Key': uuid() } },
  );
}

export function setup() {
  const merchants = Array.from({ length: MERCHANTS }, uuid);
  merchants.forEach((merchant) => postEntry(merchant));
  return { merchants };
}

export function readBalance({ merchants }) {
  const merchant = merchants[Math.floor(Math.random() * merchants.length)];
  const response = http.get(`${BALANCES}/v1/balances/daily?date=${DATE}`, { headers: { 'X-Merchant-Id': merchant } });
  check(response, { 'balance answered': (r) => r.status === 200 });
}

export function writeEntry({ merchants }) {
  const response = postEntry(merchants[Math.floor(Math.random() * merchants.length)]);
  check(response, { 'entry recorded': (r) => r.status === 201 });
}
