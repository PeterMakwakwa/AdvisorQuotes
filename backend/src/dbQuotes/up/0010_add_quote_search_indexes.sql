-- Indexes for the remaining quote-list filters, each ending in the keyset order (created_at DESC, id DESC) so a
-- filtered page is still an index range scan.

-- Client name ignoring case (the API compares upper(client_name) with the upper-cased search), plus date of birth:
-- the "does this client already have an open quote?" check before capturing a new one.
CREATE INDEX ix_quotes_upper_client_name_date_of_birth ON quotes (upper(client_name), client_date_of_birth);

CREATE INDEX ix_quotes_product_code_created_at_id ON quotes (product_code, created_at DESC, id DESC);

-- Replaces the status-only index: a handful of statuses is too coarse on its own, but with the keyset order the
-- index answers "newest quotes in this status" directly.
DROP INDEX ix_quotes_status;

CREATE INDEX ix_quotes_status_created_at_id ON quotes (status, created_at DESC, id DESC);
