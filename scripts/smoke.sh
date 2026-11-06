#!/usr/bin/env bash
# End-to-end check: an entry reaches the daily balance, even when consolidation was down.
set -euo pipefail

LEDGER=http://localhost:8081
BALANCES=http://localhost:8082
MERCHANT=$(uuidgen | tr '[:upper:]' '[:lower:]')
TODAY=$(TZ=America/Sao_Paulo date +%F)

post_entry() {
  curl -sf -o /dev/null -w '%{http_code}' -X POST "$LEDGER/v1/entries" \
    -H 'Content-Type: application/json' -H "X-Merchant-Id: $MERCHANT" \
    -d "{\"entryType\":\"$1\",\"amountCents\":$2,\"accountingDate\":\"$TODAY\"}"
}

closing_cents() {
  curl -sf -H "X-Merchant-Id: $MERCHANT" "$BALANCES/v1/balances/daily?date=$TODAY" |
    python3 -c 'import json,sys; print(json.load(sys.stdin)["closingCents"])'
}

wait_for_closing() {
  for _ in $(seq 1 60); do
    if [ "$(closing_cents 2>/dev/null || true)" = "$1" ]; then
      echo "closing balance is $1"
      return 0
    fi
    sleep 0.5
  done
  echo "closing balance did not reach $1" >&2
  return 1
}

echo '1. Entries reach the balance'
post_entry CREDIT 35000 >/dev/null
post_entry DEBIT 12000 >/dev/null
wait_for_closing 23000

echo '2. Ledger keeps accepting entries while consolidation is down'
docker compose stop consolidation-consumer consolidation-api >/dev/null 2>&1
status=$(post_entry CREDIT 10000)
[ "$status" = 201 ] || { echo "expected 201, got $status" >&2; exit 1; }
echo "ledger answered $status"

echo '3. Consolidation catches up after it returns'
docker compose start consolidation-consumer consolidation-api >/dev/null 2>&1
wait_for_closing 33000
echo 'smoke test passed'
