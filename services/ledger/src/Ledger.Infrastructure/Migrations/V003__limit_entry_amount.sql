-- NOT VALID applies the limit to new rows without scanning or locking existing ones.
ALTER TABLE ledger_entry
    ADD CONSTRAINT ck_ledger_entry_amount_limit CHECK (amount_cents <= 100000000000) NOT VALID;
