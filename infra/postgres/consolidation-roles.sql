-- Local logins. Runs once, when the volume is created. In the cloud, identities replace passwords.
CREATE ROLE consolidation_api LOGIN PASSWORD 'local-only';
CREATE ROLE consolidation_worker LOGIN PASSWORD 'local-only';
