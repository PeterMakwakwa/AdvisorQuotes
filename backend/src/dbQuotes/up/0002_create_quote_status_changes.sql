CREATE TABLE quote_status_changes (
    id           uuid          NOT NULL,
    quote_id     uuid          NOT NULL,
    from_status  varchar(20)   NULL,
    to_status    varchar(20)   NOT NULL,
    reason       varchar(500)  NULL,
    changed_by   varchar(100)  NOT NULL,
    changed_at   timestamptz   NOT NULL,
    CONSTRAINT pk_quote_status_changes PRIMARY KEY (id),
    CONSTRAINT fk_quote_status_changes_quote_id FOREIGN KEY (quote_id) REFERENCES quotes (id),
    CONSTRAINT ck_quote_status_changes_from_status CHECK (from_status IN ('Draft', 'SentToClient', 'Accepted', 'Declined')),
    CONSTRAINT ck_quote_status_changes_to_status CHECK (to_status IN ('Draft', 'SentToClient', 'Accepted', 'Declined'))
);

CREATE INDEX ix_quote_status_changes_quote_id_changed_at ON quote_status_changes (quote_id, changed_at);
