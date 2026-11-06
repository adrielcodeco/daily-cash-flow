-- Logins and passwords are environment concerns. Here the roles only receive their permissions.
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'consolidation_api') THEN
        CREATE ROLE consolidation_api NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'consolidation_worker') THEN
        CREATE ROLE consolidation_worker NOLOGIN BYPASSRLS;
    END IF;
END
$$;

-- The API only reads.
GRANT SELECT ON daily_balance TO consolidation_api;

-- The consumer works across merchants, so it bypasses row-level security.
GRANT SELECT, INSERT, UPDATE, TRUNCATE ON daily_balance TO consolidation_worker;
GRANT SELECT, INSERT, TRUNCATE ON processed_event TO consolidation_worker;

-- Without a merchant in the transaction, current_setting returns NULL and no row matches.
ALTER TABLE daily_balance ENABLE ROW LEVEL SECURITY;
ALTER TABLE daily_balance FORCE ROW LEVEL SECURITY;
CREATE POLICY merchant_isolation ON daily_balance
    USING (merchant_id = NULLIF(current_setting('app.merchant_id', true), '')::uuid)
    WITH CHECK (merchant_id = NULLIF(current_setting('app.merchant_id', true), '')::uuid);
