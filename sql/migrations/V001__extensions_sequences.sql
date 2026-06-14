-- ============================================================
-- V001: Extensions and sequences
-- PostgreSQL 16 — StoneBridge schema
-- ============================================================

-- pgcrypto: gen_random_uuid() (redundant in PG 13+ but explicit is better)
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- pg_trgm: accelerates ILIKE/LIKE searches and trigram similarity
-- Used for supplier directory name search and slab material search
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- btree_gin: allows GIN indexes on regular btypes combined with tsvector
-- Needed for composite full-text + filter indexes
CREATE EXTENSION IF NOT EXISTS btree_gin;

-- unaccent: strip accents from search strings (Italian quarry names, Spanish suppliers)
CREATE EXTENSION IF NOT EXISTS unaccent;

-- ── PO Number sequence ────────────────────────────────────────────────────────
-- Format: PO-{YYYY}-{LPAD(nextval, 6, '0')}
-- Application reads NEXTVAL and formats the string
CREATE SEQUENCE IF NOT EXISTS seq_po_number
    START     WITH 1
    INCREMENT BY    1
    NO MAXVALUE
    NO CYCLE
    CACHE     20;

COMMENT ON SEQUENCE seq_po_number
    IS 'Sequential PO number generator. App formats as PO-{YYYY}-{LPAD(val,6,''0'')}.';