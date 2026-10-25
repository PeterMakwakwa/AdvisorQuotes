-- One row per (adviser, key). The primary key is what stops two concurrent identical submissions
-- from both creating a quote: the second insert fails and the API returns the first quote instead.
CREATE TABLE idempotency_keys (
    adviser_id       varchar(50)   NOT NULL,
    idempotency_key  varchar(100)  NOT NULL,
    request_hash     char(64)      NOT NULL,
    quote_id         uuid          NOT NULL,
    created_at       timestamptz   NOT NULL,
    expires_at       timestamptz   NOT NULL,
    CONSTRAINT pk_idempotency_keys PRIMARY KEY (adviser_id, idempotency_key),
    CONSTRAINT fk_idempotency_keys_quote_id FOREIGN KEY (quote_id) REFERENCES quotes (id)
);

CREATE INDEX ix_idempotency_keys_expires_at ON idempotency_keys (expires_at);
