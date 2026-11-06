#!/usr/bin/env bash
# End-to-end checks against the running stack. Requires `docker compose up -d --build --wait`.
set -euo pipefail

LEDGER=http://localhost:18081
BALANCES=http://localhost:18082
MERCHANT=$(uuidgen | tr '[:upper:]' '[:lower:]')
TODAY=$(TZ=America/Sao_Paulo date +%F)
YESTERDAY=$(TZ=America/Sao_Paulo date -v-1d +%F 2>/dev/null || TZ=America/Sao_Paulo date -d yesterday +%F)

new_key() { uuidgen; }

. scripts/auth.sh
TOKEN=$(new_client "$MERCHANT")

# Prints the HTTP status. The response body goes to $BODY_FILE.
BODY_FILE=$(mktemp)
trap 'rm -f "$BODY_FILE"' EXIT

post_entry() { # type amount [date] [key]
  curl -s -o "$BODY_FILE" -w '%{http_code}' -X POST "$LEDGER/v1/entries" \
    -H 'Content-Type: application/json' -H "Authorization: Bearer $TOKEN" -H "Idempotency-Key: ${4:-$(new_key)}" \
    -d "{\"entryType\":\"$1\",\"amountCents\":$2,\"accountingDate\":\"${3:-$TODAY}\"}"
}

reverse_entry() { # id key
  curl -s -o /dev/null -w '%{http_code}' -X POST "$LEDGER/v1/entries/$1/reversal" \
    -H "Authorization: Bearer $TOKEN" -H "Idempotency-Key: $2"
}

body_field() { python3 -c "import json; print(json.load(open('$BODY_FILE'))['$1'])"; }

balance_field() { # field
  curl -sf -H "Authorization: Bearer $TOKEN" "$BALANCES/v1/balances/daily?date=$TODAY" |
    python3 -c "import json,sys; print(json.load(sys.stdin)['$1'])"
}

period_closing() {
  curl -sf -H "Authorization: Bearer $TOKEN" "$BALANCES/v1/balances/period?start=$TODAY&end=$TODAY" |
    python3 -c "import json,sys; print(json.load(sys.stdin)['items'][0]['closingCents'])"
}

wait_for_closing() {
  for _ in $(seq 1 120); do
    if [ "$(balance_field closingCents 2>/dev/null || true)" = "$1" ]; then
      echo "   closing balance is $1"
      return 0
    fi
    sleep 0.5
  done
  echo "closing balance did not reach $1, got $(balance_field closingCents)" >&2
  return 1
}

expect() { # expected actual label
  [ "$1" = "$2" ] || { echo "$3: expected $1, got $2" >&2; exit 1; }
  echo "   $3: $2"
}

echo '1. Entries reach the balance'
post_entry CREDIT 35000 >/dev/null
FIRST_ID=$(body_field id)
post_entry DEBIT 12000 >/dev/null
wait_for_closing 23000

echo '2. A repeated key does not create another entry'
KEY=$(new_key)
post_entry CREDIT 500 "$TODAY" "$KEY" >/dev/null
ORIGINAL=$(body_field id)
expect 201 "$(post_entry CREDIT 500 "$TODAY" "$KEY")" 'repeated key status'
expect "$ORIGINAL" "$(body_field id)" 'repeated key entry'
expect 409 "$(post_entry CREDIT 501 "$TODAY" "$KEY")" 'key reused with other content'
wait_for_closing 23500

echo '3. Concurrent calls with the same key create one entry'
KEY=$(new_key)
for _ in 1 2 3 4 5 6 7 8; do
  curl -s -o /dev/null -X POST "$LEDGER/v1/entries" -H 'Content-Type: application/json' \
    -H "Authorization: Bearer $TOKEN" -H "Idempotency-Key: $KEY" \
    -d "{\"entryType\":\"CREDIT\",\"amountCents\":100,\"accountingDate\":\"$TODAY\"}" &
done
wait
wait_for_closing 23600

echo '4. A reversal happens only once, even with concurrent calls'
for _ in 1 2 3 4 5 6; do reverse_entry "$FIRST_ID" "$(new_key)" >/dev/null & done
wait
wait_for_closing -11400
expect 409 "$(reverse_entry "$FIRST_ID" "$(new_key)")" 'second reversal'

echo '5. A backdated entry changes the opening'
post_entry CREDIT 7000 "$YESTERDAY" >/dev/null
wait_for_closing -4400
expect 7000 "$(balance_field openingCents)" 'opening'

echo '6. The query survives a cache outage'
docker compose stop redis >/dev/null 2>&1
expect -4400 "$(balance_field closingCents)" 'closing without cache'
docker compose start redis >/dev/null 2>&1

echo '7. Ledger keeps accepting entries while consolidation is down'
docker compose stop consolidation-consumer consolidation-api >/dev/null 2>&1
expect 201 "$(post_entry CREDIT 10000)" 'ledger status'

echo '8. Consolidation catches up after it returns'
docker compose start consolidation-consumer consolidation-api >/dev/null 2>&1
for _ in $(seq 1 40); do curl -sf "$BALANCES/health/ready" >/dev/null && break; sleep 0.5; done
wait_for_closing 5600

echo '9. Rebuilding from the ledger restores the same balance'
./scripts/rebuild-consolidation.sh >/dev/null 2>&1
for _ in $(seq 1 60); do
  REBUILT=$(period_closing)
  [ "$REBUILT" = 5600 ] && break
  sleep 0.5
done
expect 5600 "$REBUILT" 'rebuilt closing, without cache'

echo '10. Invalid entries are refused and never reach the balance'
post_body() { # json
  curl -s -o /dev/null -w '%{http_code}' -X POST "$LEDGER/v1/entries" \
    -H 'Content-Type: application/json' -H "Authorization: Bearer $TOKEN" -H "Idempotency-Key: $(new_key)" -d "$1"
}
NUMERIC_TYPE="{\"entryType\":7,\"amountCents\":500,\"accountingDate\":\"$TODAY\"}"
MISSING_TYPE="{\"amountCents\":500,\"accountingDate\":\"$TODAY\"}"
expect 400 "$(post_body "$NUMERIC_TYPE")" 'numeric type'
expect 422 "$(post_body "$MISSING_TYPE")" 'missing type'
expect 422 "$(post_entry CREDIT 100000000001)" 'amount above the limit'
expect 5600 "$(period_closing)" 'closing after refused entries'

echo '11. Access depends on the token'
OTHER=$(uuidgen | tr '[:upper:]' '[:lower:]')
OTHER_TOKEN=$(new_client "$OTHER")
READ_ONLY=$(uuidgen | tr '[:upper:]' '[:lower:]')
READ_ONLY_TOKEN=$(new_client "$READ_ONLY" '"api-audience","entries.read"')
get_status() { # url [token]
  curl -s -o /dev/null -w '%{http_code}' ${2:+-H "Authorization: Bearer $2"} "$1"
}
post_with_token() { # token
  curl -s -o /dev/null -w '%{http_code}' -X POST "$LEDGER/v1/entries" -H 'Content-Type: application/json' \
    -H "Authorization: Bearer $1" -H "Idempotency-Key: $(new_key)" -d "$NEW_ENTRY"
}
NEW_ENTRY="{\"entryType\":\"CREDIT\",\"amountCents\":100,\"accountingDate\":\"$TODAY\"}"
expect 401 "$(get_status "$BALANCES/v1/balances/daily?date=$TODAY")" 'no token'
expect 401 "$(get_status "$BALANCES/v1/balances/daily?date=$TODAY" 'not-a-token')" 'invalid token'
expect 401 "$(curl -s -o /dev/null -w '%{http_code}' -H "X-Merchant-Id: $MERCHANT" "$BALANCES/v1/balances/daily?date=$TODAY")" 'merchant header alone'
expect 403 "$(post_with_token "$READ_ONLY_TOKEN")" 'write without the write scope'
expect 403 "$(get_status "$BALANCES/v1/balances/daily?date=$TODAY" "$READ_ONLY_TOKEN")" 'balance without the balance scope'
expect 404 "$(get_status "$LEDGER/v1/entries/$FIRST_ID" "$OTHER_TOKEN")" 'entry of another merchant'
OTHER_CLOSING=$(curl -sf -H "Authorization: Bearer $OTHER_TOKEN" -H "X-Merchant-Id: $MERCHANT" "$BALANCES/v1/balances/period?start=$TODAY&end=$TODAY" |
  python3 -c "import json,sys; print(json.load(sys.stdin)['items'][0]['closingCents'])")
expect 0 "$OTHER_CLOSING" 'the merchant header is ignored'

echo '12. Excess requests get 429 without affecting other clients'
STATUSES=$(seq 1 400 | xargs -P 40 -I{} curl -s -o /dev/null -w '%{http_code}\n' -H "Authorization: Bearer $TOKEN" "$BALANCES/v1/balances/daily?date=$TODAY" | sort | uniq -c)
echo "$STATUSES" | grep -q ' 429$' || { echo "no 429 in: $STATUSES" >&2; exit 1; }
expect 200 "$(get_status "$BALANCES/v1/balances/daily?date=$TODAY" "$OTHER_TOKEN")" 'another client during the burst'

echo '13. A collector outage does not affect writes'
docker compose stop otel-collector >/dev/null 2>&1
sleep 1
expect 201 "$(post_entry CREDIT 100)" 'write without the collector'
docker compose start otel-collector >/dev/null 2>&1

echo '14. One trace follows an entry from the API to the balance'
sleep 5
TRACE=$(python3 -c "import secrets; print(secrets.token_hex(16))")
curl -s -o /dev/null -X POST "$LEDGER/v1/entries" -H 'Content-Type: application/json' -H "Authorization: Bearer $TOKEN" \
  -H "Idempotency-Key: $(new_key)" -H "traceparent: 00-$TRACE-$(python3 -c "import secrets; print(secrets.token_hex(8))")-01" -d "$NEW_ENTRY"
SERVICES=0
for _ in $(seq 1 40); do
  SERVICES=$(curl -s "http://localhost:18086/api/traces/$TRACE" | python3 -c "
import json,sys
try:
    data=json.load(sys.stdin)
except ValueError:
    print(0); raise SystemExit
names={a['value'].get('stringValue') for b in data.get('batches',[]) for a in b['resource']['attributes'] if a['key']=='service.name'}
print(len(names & {'ledger-api','outbox-publisher','consolidation-consumer'}))")
  [ "$SERVICES" = 3 ] && break
  sleep 1
done
expect 3 "$SERVICES" 'services in the trace'

echo 'smoke test passed'
