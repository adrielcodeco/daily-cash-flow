#!/usr/bin/env bash
# Rebuilds the consolidated balances from the ledger entries, then waits until they match.
# The reset runs with the same cache targets as the APIs, so it can invalidate their cache.
set -euo pipefail

docker compose stop consolidation-consumer
docker compose run --rm --no-deps consolidation-consumer --reset
docker compose run --rm --no-deps outbox-publisher --republish
docker compose start consolidation-consumer

for _ in $(seq 1 "${RECONCILE_TIMEOUT:-600}"); do
  ./scripts/reconcile.sh >/dev/null 2>&1 && break
  sleep 1
done
./scripts/reconcile.sh
