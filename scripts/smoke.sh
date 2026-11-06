#!/usr/bin/env bash
# End-to-end checks against the running stack. Requires `docker compose up -d --build --wait`.
set -euo pipefail

LEDGER=http://localhost:18081
BALANCES=http://localhost:18082
MERCHANT=$(uuidgen | tr '[:upper:]' '[:lower:]')
TODAY=$(TZ=America/Sao_Paulo date +%F)
YESTERDAY=$(TZ=America/Sao_Paulo date -v-1d +%F 2>/dev/null || TZ=America/Sao_Paulo date -d yesterday +%F)

new_key() { uuidgen; }

# Prints the HTTP status. The response body goes to $BODY_FILE.
BODY_FILE=$(mktemp)
trap 'rm -f "$BODY_FILE"' EXIT

post_entry() { # type amount [date] [key]
  curl -s -o "$BODY_FILE" -w '%{http_code}' -X POST "$LEDGER/v1/entries" \
    -H 'Content-Type: application/json' -H "X-Merchant-Id: $MERCHANT" -H "Idempotency-Key: ${4:-$(new_key)}" \
    -d "{\"entryType\":\"$1\",\"amountCents\":$2,\"accountingDate\":\"${3:-$TODAY}\"}"
}

reverse_entry() { # id key
  curl -s -o /dev/null -w '%{http_code}' -X POST "$LEDGER/v1/entries/$1/reversal" \
    -H "X-Merchant-Id: $MERCHANT" -H "Idempotency-Key: $2"
}

body_field() { python3 -c "import json; print(json.load(open('$BODY_FILE'))['$1'])"; }

balance_field() { # field
  curl -sf -H "X-Merchant-Id: $MERCHANT" "$BALANCES/v1/balances/daily?date=$TODAY" |
    python3 -c "import json,sys; print(json.load(sys.stdin)['$1'])"
}

period_closing() {
  curl -sf -H "X-Merchant-Id: $MERCHANT" "$BALANCES/v1/balances/period?start=$TODAY&end=$TODAY" |
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
    -H "X-Merchant-Id: $MERCHANT" -H "Idempotency-Key: $KEY" \
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
# The period query skips the cache, so it proves the rebuilt data.
for _ in $(seq 1 60); do
  REBUILT=$(period_closing)
  [ "$REBUILT" = 5600 ] && break
  sleep 0.5
done
expect 5600 "$REBUILT" 'rebuilt closing, without cache'

echo 'smoke test passed'
