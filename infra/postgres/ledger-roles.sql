-- Local logins. Runs once, when the volume is created. In the cloud, identities replace passwords.
CREATE ROLE ledger_api LOGIN PASSWORD 'local-only';
CREATE ROLE ledger_worker LOGIN BYPASSRLS PASSWORD 'local-only';
