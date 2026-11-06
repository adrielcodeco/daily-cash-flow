// Writes at a steady rate. The resilience script takes dependencies down while this runs.
import http from 'k6/http';
import { check } from 'k6';

const LEDGER = __ENV.LEDGER_URL || 'http://localhost:18081';
const MERCHANT = __ENV.MERCHANT;
const DATE = __ENV.DATE;

export const options = {
  scenarios: {
    writes: {
      executor: 'constant-arrival-rate',
      rate: 50,
      timeUnit: '1s',
      duration: __ENV.DURATION || '60s',
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  thresholds: {
    http_req_failed: ['rate==0'],
  },
};

function uuid() {
  return 'xxxxxxxx-xxxx-4xxx-8xxx-xxxxxxxxxxxx'.replace(/x/g, () => Math.floor(Math.random() * 16).toString(16));
}

export default function () {
  const response = http.post(
    `${LEDGER}/v1/entries`,
    JSON.stringify({ entryType: 'CREDIT', amountCents: 100, accountingDate: DATE }),
    { headers: { 'Content-Type': 'application/json', 'X-Merchant-Id': MERCHANT, 'Idempotency-Key': uuid() } },
  );
  check(response, { 'entry recorded': (r) => r.status === 201 });
}
