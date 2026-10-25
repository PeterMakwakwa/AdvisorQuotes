CREATE TABLE quotes (
    id                    uuid           NOT NULL,
    external_reference    varchar(30)    NOT NULL,
    adviser_id            varchar(50)    NOT NULL,
    client_name           varchar(200)   NOT NULL,
    client_date_of_birth  date           NOT NULL,
    client_is_smoker      boolean        NOT NULL,
    product_code          varchar(20)    NOT NULL,
    cover_amount          numeric(18,2)  NOT NULL,
    term_years            smallint       NOT NULL,
    monthly_premium       numeric(18,2)  NOT NULL,
    currency              char(3)        NOT NULL,
    status                varchar(20)    NOT NULL,
    notes                 varchar(2000)  NULL,
    pricing_version       varchar(20)    NOT NULL,
    created_at            timestamptz    NOT NULL,
    updated_at            timestamptz    NOT NULL,
    CONSTRAINT pk_quotes PRIMARY KEY (id),
    CONSTRAINT uq_quotes_external_reference UNIQUE (external_reference),
    CONSTRAINT ck_quotes_cover_amount CHECK (cover_amount > 0),
    CONSTRAINT ck_quotes_term_years CHECK (term_years > 0),
    CONSTRAINT ck_quotes_monthly_premium CHECK (monthly_premium >= 0),
    CONSTRAINT ck_quotes_status CHECK (status IN ('Draft', 'SentToClient', 'Accepted', 'Declined'))
);

CREATE INDEX ix_quotes_adviser_id_created_at ON quotes (adviser_id, created_at DESC);
CREATE INDEX ix_quotes_status ON quotes (status);
