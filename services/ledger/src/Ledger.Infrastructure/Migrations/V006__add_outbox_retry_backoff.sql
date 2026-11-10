-- A message that keeps failing waits longer each time, so it cannot block the ones created after it.
ALTER TABLE outbox
    ADD COLUMN attempts integer NOT NULL DEFAULT 0,
    ADD COLUMN next_attempt_at timestamptz NOT NULL DEFAULT '-infinity';
