// Peak scenario from the challenge: 50 balance requests per second, with at most 5% lost.
// Entries keep arriving during the test, so the cache is invalidated as in real traffic.
import http from 'k6/http';
import { check } from 'k6';

const LEDGER = __ENV.LEDGER_URL || 'http://localhost:18081';
const BALANCES = __ENV.BALANCES_URL || 'http://localhost:18082';
const KEYCLOAK = __ENV.KEYCLOAK_URL || 'http://localhost:18083';
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

function adminToken() {
  const response = http.post(`${KEYCLOAK}/realms/master/protocol/openid-connect/token`, {
    grant_type: 'password', client_id: 'admin-cli', username: 'admin', password: 'local-only',
  });
  return response.json('access_token');
}

function newMerchantToken(admin) {
  const merchant = uuid();
  const clientId = `merchant-${merchant}`;
  http.post(
    `${KEYCLOAK}/admin/realms/daily-cash-flow/clients`,
    JSON.stringify({
      clientId,
      secret: 'local-only',
      serviceAccountsEnabled: true,
      publicClient: false,
      standardFlowEnabled: false,
      defaultClientScopes: ['api-audience', 'entries.read', 'entries.write', 'balances.read'],
      protocolMappers: [{
        name: 'merchant',
        protocol: 'openid-connect',
        protocolMapper: 'oidc-hardcoded-claim-mapper',
        config: { 'claim.name': 'merchant_id', 'claim.value': merchant, 'jsonType.label': 'String', 'access.token.claim': 'true' },
      }],
    }),
    { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${admin}` } },
  );
  const response = http.post(`${KEYCLOAK}/realms/daily-cash-flow/protocol/openid-connect/token`, {
    grant_type: 'client_credentials', client_id: clientId, client_secret: 'local-only',
  });
  return response.json('access_token');
}

function postEntry(token) {
  return http.post(
    `${LEDGER}/v1/entries`,
    JSON.stringify({ entryType: 'CREDIT', amountCents: 100, accountingDate: DATE }),
    { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}`, 'Idempotency-Key': uuid() } },
  );
}

export function setup() {
  const admin = adminToken();
  const tokens = Array.from({ length: MERCHANTS }, () => newMerchantToken(admin));
  tokens.forEach((token) => postEntry(token));
  return { tokens };
}

export function readBalance({ tokens }) {
  const token = tokens[Math.floor(Math.random() * tokens.length)];
  const response = http.get(`${BALANCES}/v1/balances/daily?date=${DATE}`, { headers: { Authorization: `Bearer ${token}` } });
  check(response, { 'balance answered': (r) => r.status === 200 });
}

export function writeEntry({ tokens }) {
  const response = postEntry(tokens[Math.floor(Math.random() * tokens.length)]);
  check(response, { 'entry recorded': (r) => r.status === 201 });
}
