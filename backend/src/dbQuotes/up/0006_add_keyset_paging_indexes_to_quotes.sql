-- Keyset paging for the quote list: newest first, ties broken by id, so every page is an index range scan
-- however deep it is. One index for the adviser-filtered list, one for the unfiltered list.
DROP INDEX ix_quotes_adviser_id_created_at;

CREATE INDEX ix_quotes_adviser_id_created_at_id ON quotes (adviser_id, created_at DESC, id DESC);
CREATE INDEX ix_quotes_created_at_id ON quotes (created_at DESC, id DESC);
