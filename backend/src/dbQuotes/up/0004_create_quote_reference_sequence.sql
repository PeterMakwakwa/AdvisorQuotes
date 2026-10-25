-- Source of the human-readable external reference (Q-<year>-<number>). Sequences never hand out
-- the same value twice, even under concurrency; gaps after a rolled-back insert are acceptable.
CREATE SEQUENCE quote_reference_seq AS bigint START WITH 1 INCREMENT BY 1;
