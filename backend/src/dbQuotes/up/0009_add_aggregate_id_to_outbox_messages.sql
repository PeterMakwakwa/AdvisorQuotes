-- Per-quote ordering for the outbox: a message is only published once every earlier message for the same aggregate
-- (quote) has been published, so a consumer never sees a status change before the quote was created.
ALTER TABLE outbox_messages ADD COLUMN aggregate_id uuid NULL;

UPDATE outbox_messages SET aggregate_id = (payload->>'quoteId')::uuid WHERE aggregate_id IS NULL;

ALTER TABLE outbox_messages ALTER COLUMN aggregate_id SET NOT NULL;

-- The dispatcher's "is an earlier message for this aggregate still unpublished?" check.
CREATE INDEX ix_outbox_messages_pending_aggregate ON outbox_messages (aggregate_id, occurred_at, id) WHERE processed_at IS NULL;
