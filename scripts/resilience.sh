#!/usr/bin/env bash
# Takes dependencies down during a write load and proves that no entry was lost.
set -euo pipefail

BALANCES=http://localhost:18082
DATE=$(TZ=America/Sao_Paulo date +%F)
MERCHANT=$(uuidgen | tr '[:upper:]' '[:lower:]')
RESULTS=load-tests/results
mkdir -p "$RESULTS"

ledger_count() {
  docker compose exec -T ledger-db psql -U ledger -d ledger -Atc \
    "select count(*) from ledger_entry where merchant_id = '$MERCHANT'"
}

pending_events() {
  docker compose exec -T ledger-db psql -U ledger -d ledger -Atc \
    "select count(*) from outbox where published_at is null"
}

closing() {
  curl -sf -H "X-Merchant-Id: $MERCHANT" "$BALANCES/v1/balances/period?start=$DATE&end=$DATE" |
    python3 -c "import json,sys; print(json.load(sys.stdin)['items'][0]['closingCents'])"
}

echo '== 50 writes per second for 90 seconds'
k6 run --quiet --summary-export "$RESULTS/resilience.json" \
  -e MERCHANT="$MERCHANT" -e DATE="$DATE" -e DURATION=90s load-tests/ledger-outage.js &
K6=$!

sleep 10
echo '== Consolidation down'
docker compose stop consolidation-consumer consolidation-api >/dev/null 2>&1
sleep 30
echo "== Broker frozen. Pending events before: $(pending_events)"
docker compose pause pubsub >/dev/null 2>&1
sleep 20
echo "== Broker back. Pending events now: $(pending_events)"
docker compose unpause pubsub >/dev/null 2>&1
sleep 10
echo '== Consolidation back'
docker compose start consolidation-consumer consolidation-api >/dev/null 2>&1

WRITES_OK=0
wait "$K6" || WRITES_OK=$?

ENTRIES=$(ledger_count)
EXPECTED=$((ENTRIES * 100))
echo "== Entries stored: $ENTRIES. Waiting for closing balance $EXPECTED"
for _ in $(seq 1 40); do curl -sf "$BALANCES/health/ready" >/dev/null && break; sleep 0.5; done
START=$(date +%s)
FAILED_READS=0
ACTUAL=
for _ in $(seq 1 480); do
  if ACTUAL=$(closing 2>/dev/null); then
    [ "$ACTUAL" = "$EXPECTED" ] && break
  else
    FAILED_READS=$((FAILED_READS + 1))
  fi
  sleep 0.25
done
echo "== Closing balance: $ACTUAL, after $(( $(date +%s) - START ))s. Failed reads during catch-up: $FAILED_READS. Pending events: $(pending_events)"
[ "$WRITES_OK" = 0 ] && [ "$ACTUAL" = "$EXPECTED" ] && [ "$FAILED_READS" = 0 ] || { echo 'resilience test failed' >&2; exit 1; }
echo 'resilience test passed'
