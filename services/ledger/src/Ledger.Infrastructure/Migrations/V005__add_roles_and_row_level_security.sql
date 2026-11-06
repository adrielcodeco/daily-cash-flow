-- Logins and passwords are environment concerns. Here the roles only receive their permissions.
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'ledger_api') THEN
        CREATE ROLE ledger_api NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'ledger_worker') THEN
        CREATE ROLE ledger_worker NOLOGIN BYPASSRLS;
    END IF;
END
$$;

-- The API can only read and add: entries are immutable at the database level.
GRANT SELECT, INSERT ON ledger_entry, idempotency_key TO ledger_api;
GRANT INSERT ON outbox TO ledger_api;

-- The publisher works across merchants, so it bypasses row-level security.
GRANT SELECT ON ledger_entry TO ledger_worker;
GRANT SELECT, INSERT, UPDATE ON outbox TO ledger_worker;

-- Without a merchant in the transaction, current_setting returns NULL and no row matches.
ALTER TABLE ledger_entry ENABLE ROW LEVEL SECURITY;
ALTER TABLE ledger_entry FORCE ROW LEVEL SECURITY;
CREATE POLICY merchant_isolation ON ledger_entry
    USING (merchant_id = NULLIF(current_setting('app.merchant_id', true), '')::uuid)
    WITH CHECK (merchant_id = NULLIF(current_setting('app.merchant_id', true), '')::uuid);

ALTER TABLE idempotency_key ENABLE ROW LEVEL SECURITY;
ALTER TABLE idempotency_key FORCE ROW LEVEL SECURITY;
CREATE POLICY merchant_isolation ON idempotency_key
    USING (merchant_id = NULLIF(current_setting('app.merchant_id', true), '')::uuid)
    WITH CHECK (merchant_id = NULLIF(current_setting('app.merchant_id', true), '')::uuid);
