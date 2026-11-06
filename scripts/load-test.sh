#!/usr/bin/env bash
# Runs the peak scenario twice: with the cache, then with the cache down.
set -euo pipefail

DATE=$(TZ=America/Sao_Paulo date +%F)
RESULTS=load-tests/results
mkdir -p "$RESULTS"

# k6 exports what setup() returned, and that includes the test tokens.
strip_setup_data() {
  python3 -c "import json,sys; p=sys.argv[1]; d=json.load(open(p)); d.pop('setup_data',None); json.dump(d,open(p,'w'),indent=2)" "$1"
}

echo '== Peak with cache'
STATUS=0
k6 run --quiet --summary-export "$RESULTS/peak-with-cache.json" -e DATE="$DATE" -e DURATION="${DURATION:-5m}" load-tests/balance-peak.js || STATUS=$?
strip_setup_data "$RESULTS/peak-with-cache.json"

echo '== Peak with cache down'
docker compose stop redis >/dev/null 2>&1
trap 'docker compose start redis >/dev/null 2>&1' EXIT
k6 run --quiet --summary-export "$RESULTS/peak-without-cache.json" -e DATE="$DATE" -e DURATION="${NO_CACHE_DURATION:-2m}" load-tests/balance-peak.js || STATUS=$?
strip_setup_data "$RESULTS/peak-without-cache.json"
exit "$STATUS"
