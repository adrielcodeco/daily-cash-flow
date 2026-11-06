CREATE TABLE ledger_entry (
    id uuid PRIMARY KEY,
    merchant_id uuid NOT NULL,
    entry_type text NOT NULL CHECK (entry_type IN ('CREDIT', 'DEBIT')),
    amount_cents bigint NOT NULL CHECK (amount_cents > 0),
    accounting_date date NOT NULL,
    description text,
    reversal_of uuid REFERENCES ledger_entry (id),
    created_at timestamptz NOT NULL
);

CREATE INDEX ix_ledger_entry_merchant_date ON ledger_entry (merchant_id, accounting_date, id);
CREATE UNIQUE INDEX ux_ledger_entry_reversal_of ON ledger_entry (reversal_of) WHERE reversal_of IS NOT NULL;

CREATE TABLE outbox (
    id uuid PRIMARY KEY,
    type text NOT NULL,
    payload jsonb NOT NULL,
    created_at timestamptz NOT NULL,
    published_at timestamptz
);

CREATE INDEX ix_outbox_pending ON outbox (created_at) WHERE published_at IS NULL;
