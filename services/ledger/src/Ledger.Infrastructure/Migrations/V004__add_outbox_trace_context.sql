-- Publishing happens after the request ended, so the trace must travel with the event.
ALTER TABLE outbox ADD COLUMN trace_context text;
