-- The adviser's own reference for the quote (for example their CRM case number), supplied by the client.
-- external_reference stays the reference this service generates (Q-<year>-<number>).
-- Optional and not unique: an adviser can quote the same case more than once.
ALTER TABLE quotes ADD COLUMN adviser_reference varchar(50) NULL;

CREATE INDEX ix_quotes_adviser_id_adviser_reference
    ON quotes (adviser_id, adviser_reference)
    WHERE adviser_reference IS NOT NULL;
