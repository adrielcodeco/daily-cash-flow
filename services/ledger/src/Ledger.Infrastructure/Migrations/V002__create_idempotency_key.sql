CREATE TABLE idempotency_key (
    merchant_id uuid NOT NULL,
    key text NOT NULL,
    request_hash text NOT NULL,
    entry_id uuid NOT NULL REFERENCES ledger_entry (id),
    created_at timestamptz NOT NULL,
    PRIMARY KEY (merchant_id, key)
);
