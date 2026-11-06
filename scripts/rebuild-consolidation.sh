#!/usr/bin/env bash
# Rebuilds the consolidated balances from the ledger entries.
set -euo pipefail

docker compose stop consolidation-consumer
docker compose run --rm --no-deps consolidation-consumer --reset
docker compose run --rm --no-deps outbox-publisher --republish
docker compose start consolidation-consumer
