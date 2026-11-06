CREATE TABLE daily_balance (
    merchant_id uuid NOT NULL,
    date date NOT NULL,
    credits_cents bigint NOT NULL DEFAULT 0,
    debits_cents bigint NOT NULL DEFAULT 0,
    updated_at timestamptz NOT NULL,
    PRIMARY KEY (merchant_id, date)
);

CREATE TABLE processed_event (
    event_id uuid PRIMARY KEY,
    processed_at timestamptz NOT NULL
);
