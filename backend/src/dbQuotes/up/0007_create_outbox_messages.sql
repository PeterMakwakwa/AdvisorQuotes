-- Transactional outbox: domain events are written here in the same transaction as the quote change, then published
-- by a background dispatcher. A change and its event are therefore stored together or not at all, and an event is
-- published at least once even if the process stops between saving and publishing.
CREATE TABLE outbox_messages (
    id            uuid           NOT NULL,
    event_type    varchar(100)   NOT NULL,
    payload       jsonb          NOT NULL,
    occurred_at   timestamptz    NOT NULL,
    trace_parent  varchar(55)    NULL,
    attempts      integer        NOT NULL DEFAULT 0,
    next_attempt_at timestamptz  NULL,
    processed_at  timestamptz    NULL,
    last_error    varchar(2000)  NULL,
    CONSTRAINT pk_outbox_messages PRIMARY KEY (id),
    CONSTRAINT ck_outbox_messages_attempts CHECK (attempts >= 0)
);

-- The dispatcher's query: oldest pending messages first.
CREATE INDEX ix_outbox_messages_pending ON outbox_messages (occurred_at, id) WHERE processed_at IS NULL;

-- The maintenance job's query: published messages past their retention.
CREATE INDEX ix_outbox_messages_processed_at ON outbox_messages (processed_at) WHERE processed_at IS NOT NULL;
