-- Claim-with-lease for the outbox dispatcher. A dispatcher claims a batch in one short statement (setting locked_until
-- and a fresh lock_token) and then publishes outside any transaction, so no row lock is held while a slow publisher runs.
-- If the dispatcher dies, the lease expires and another instance picks the message up. lock_token is checked when the
-- outcome is saved, so a dispatcher whose lease expired cannot overwrite the result of the one that took over.
ALTER TABLE outbox_messages
    ADD COLUMN locked_until timestamptz NULL,
    ADD COLUMN lock_token   uuid        NULL;
