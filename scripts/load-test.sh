#!/usr/bin/env bash
# Runs the peak scenario in four ways: without the cache, with it, with it enabled but down,
# and without it for merchants with two years of daily balances, the case the opening sum depends on.
set -euo pipefail

DATE=$(TZ=America/Sao_Paulo date +%F)
RESULTS=load-tests/results
mkdir -p "$RESULTS"

# k6 exports what setup() returned, and that includes the test tokens.
strip_setup_data() {
  python3 -c "import json,sys; p=sys.argv[1]; d=json.load(open(p)); d.pop('setup_data',None); json.dump(d,open(p,'w'),indent=2)" "$1"
}

restart_apis() { # cache targets
  CACHE_TARGETS="$1" docker compose up -d --wait --wait-timeout 120 ledger-api consolidation-api consolidation-consumer >/dev/null 2>&1
}

echo '== Peak without cache, the default'
STATUS=0
restart_apis ''
k6 run --quiet --summary-export "$RESULTS/peak-default.json" -e DATE="$DATE" -e DURATION="${DURATION:-5m}" load-tests/balance-peak.js || STATUS=$?
strip_setup_data "$RESULTS/peak-default.json"

echo '== Peak with cache'
restart_apis daily_balance
k6 run --quiet --summary-export "$RESULTS/peak-with-cache.json" -e DATE="$DATE" -e DURATION="${DURATION:-5m}" load-tests/balance-peak.js || STATUS=$?
strip_setup_data "$RESULTS/peak-with-cache.json"

echo '== Peak with cache down'
docker compose stop redis >/dev/null 2>&1
trap 'docker compose start redis >/dev/null 2>&1; restart_apis ""' EXIT
k6 run --quiet --summary-export "$RESULTS/peak-without-cache.json" -e DATE="$DATE" -e DURATION="${NO_CACHE_DURATION:-2m}" load-tests/balance-peak.js || STATUS=$?
strip_setup_data "$RESULTS/peak-without-cache.json"

echo '== Peak with two years of history'
docker compose start redis >/dev/null 2>&1
restart_apis ''
MERCHANT_IDS=$(python3 -c "import uuid; print(','.join(str(uuid.uuid4()) for _ in range(50)))")
docker compose exec -T consolidation-db psql -q -U consolidation -d consolidation -c "
  INSERT INTO daily_balance (merchant_id, date, credits_cents, debits_cents, updated_at)
  SELECT m::uuid, d::date, 50000, 20000, now()
  FROM unnest(string_to_array('$MERCHANT_IDS', ',')) AS m,
       generate_series(DATE '$DATE' - 730, DATE '$DATE' - 1, interval '1 day') AS d"
k6 run --quiet --summary-export "$RESULTS/peak-long-history.json" -e DATE="$DATE" -e MERCHANT_IDS="$MERCHANT_IDS" \
  -e DURATION="${HISTORY_DURATION:-2m}" load-tests/balance-peak.js || STATUS=$?
strip_setup_data "$RESULTS/peak-long-history.json"
exit "$STATUS"
