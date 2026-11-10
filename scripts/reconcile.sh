#!/usr/bin/env bash
# Compares the daily totals in the ledger with the consolidated balances, merchant by merchant.
# Exits 1 when they differ. Events still in flight show up as differences, so run it after the backlog drains.
set -euo pipefail

totals() { # service database user query
  docker compose exec -T "$1" psql -U "$3" -d "$2" -At -F, -c "$4"
}

LEDGER=$(totals ledger-db ledger ledger "
  SELECT merchant_id, accounting_date,
         COALESCE(SUM(amount_cents) FILTER (WHERE entry_type = 'CREDIT'), 0),
         COALESCE(SUM(amount_cents) FILTER (WHERE entry_type = 'DEBIT'), 0)
  FROM ledger_entry GROUP BY 1, 2")
BALANCES=$(totals consolidation-db consolidation consolidation "
  SELECT merchant_id, date, credits_cents, debits_cents FROM daily_balance")

python3 - "$LEDGER" "$BALANCES" <<'PY'
import sys

def read(text):
    rows = {}
    for line in filter(None, text.splitlines()):
        merchant, day, credits, debits = line.split(',')
        rows[(merchant, day)] = (int(credits), int(debits))
    return rows

ledger, balances = read(sys.argv[1]), read(sys.argv[2])
differences = [
    (key, ledger.get(key, (0, 0)), balances.get(key, (0, 0)))
    for key in sorted(ledger.keys() | balances.keys())
    if ledger.get(key, (0, 0)) != balances.get(key, (0, 0))
]
print(f'{len(ledger)} merchant-days in the ledger, {len(balances)} in the balances, {len(differences)} different')
for (merchant, day), expected, actual in differences[:20]:
    print(f'  {merchant} {day}: ledger {expected}, balance {actual}')
sys.exit(1 if differences else 0)
PY
