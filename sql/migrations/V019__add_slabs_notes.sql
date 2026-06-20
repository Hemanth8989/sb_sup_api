-- V019: Add missing notes column to slabs table
-- The column is defined in V005 schema but was absent from the live DB.

ALTER TABLE slabs ADD COLUMN IF NOT EXISTS notes TEXT;
