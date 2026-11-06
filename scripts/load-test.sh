#!/usr/bin/env bash
# Runs the peak scenario twice: with the cache, then with the cache down.
set -euo pipefail

DATE=$(TZ=America/Sao_Paulo date +%F)
RESULTS=load-tests/results
mkdir -p "$RESULTS"

echo '== Peak with cache'
STATUS=0
k6 run --quiet --summary-export "$RESULTS/peak-with-cache.json" -e DATE="$DATE" -e DURATION="${DURATION:-5m}" load-tests/balance-peak.js || STATUS=$?

echo '== Peak with cache down'
docker compose stop redis >/dev/null 2>&1
trap 'docker compose start redis >/dev/null 2>&1' EXIT
k6 run --quiet --summary-export "$RESULTS/peak-without-cache.json" -e DATE="$DATE" -e DURATION="${NO_CACHE_DURATION:-2m}" load-tests/balance-peak.js || STATUS=$?
exit "$STATUS"
