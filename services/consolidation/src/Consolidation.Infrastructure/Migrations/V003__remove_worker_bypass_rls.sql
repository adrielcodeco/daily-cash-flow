-- The consumer sets the event's merchant in each transaction, so it no longer needs to bypass row-level security.
-- TRUNCATE, used by the rebuild, is not subject to row-level security.
ALTER ROLE consolidation_worker NOBYPASSRLS;
