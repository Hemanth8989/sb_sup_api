-- ============================================================
-- V016: Seed price lists, price list items, and connection-price-list assignments
--
-- Price lists for supplier 1 (Marble Masters — 11111111-…):
--   pl000001: Standard list — all 4 variants at list price
--   pl000002: Preferred list — Countertop Kings (fab1) discount pricing
--
-- Connection assignments:
--   ff000001 (fab1 ↔ sup1, preferred tier) → pl000002
--   ff000003 (fab2 ↔ sup1, standard tier)  → pl000001
-- ============================================================

SET row_security = OFF;

-- ── Price lists ───────────────────────────────────────────────────────────────
INSERT INTO price_lists (id, tenant_id, name, tier, currency, is_active, created_at, updated_at) VALUES
    ('pl000001-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'Marble Masters Standard 2026', 'standard', 'USD', TRUE,
     NOW() - INTERVAL '3 months', NOW()),

    ('pl000002-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'Preferred Partner Pricing 2026', 'preferred', 'USD', TRUE,
     NOW() - INTERVAL '3 months', NOW());

-- ── Price list items — Standard list ─────────────────────────────────────────
-- Carrara White 3cm Pol (dd000001) · Carrara White 2cm Pol (dd000002)
-- Calacatta Gold 3cm Pol (dd000003) · Absolute Black 3cm Pol (dd000004)
INSERT INTO price_list_items (id, price_list_id, variant_id, unit_price, currency, created_at) VALUES
    ('pi000001-0000-0000-0000-000000000000',
     'pl000001-0000-0000-0000-000000000000',
     'dd000001-0000-0000-0000-000000000000',
     28.50, 'USD', NOW() - INTERVAL '3 months'),

    ('pi000002-0000-0000-0000-000000000000',
     'pl000001-0000-0000-0000-000000000000',
     'dd000002-0000-0000-0000-000000000000',
     22.00, 'USD', NOW() - INTERVAL '3 months'),

    ('pi000003-0000-0000-0000-000000000000',
     'pl000001-0000-0000-0000-000000000000',
     'dd000003-0000-0000-0000-000000000000',
     85.00, 'USD', NOW() - INTERVAL '3 months'),

    ('pi000004-0000-0000-0000-000000000000',
     'pl000001-0000-0000-0000-000000000000',
     'dd000004-0000-0000-0000-000000000000',
     32.00, 'USD', NOW() - INTERVAL '3 months');

-- ── Price list items — Preferred list (≈10-12% below standard) ───────────────
INSERT INTO price_list_items (id, price_list_id, variant_id, unit_price, currency, created_at) VALUES
    ('pi000005-0000-0000-0000-000000000000',
     'pl000002-0000-0000-0000-000000000000',
     'dd000001-0000-0000-0000-000000000000',
     25.50, 'USD', NOW() - INTERVAL '3 months'),

    ('pi000006-0000-0000-0000-000000000000',
     'pl000002-0000-0000-0000-000000000000',
     'dd000002-0000-0000-0000-000000000000',
     19.50, 'USD', NOW() - INTERVAL '3 months'),

    ('pi000007-0000-0000-0000-000000000000',
     'pl000002-0000-0000-0000-000000000000',
     'dd000003-0000-0000-0000-000000000000',
     76.00, 'USD', NOW() - INTERVAL '3 months'),

    ('pi000008-0000-0000-0000-000000000000',
     'pl000002-0000-0000-0000-000000000000',
     'dd000004-0000-0000-0000-000000000000',
     28.50, 'USD', NOW() - INTERVAL '3 months');

-- ── Assign price lists to connections ─────────────────────────────────────────
-- ff000001: fab1 ↔ sup1, preferred tier → pl000002 (preferred pricing)
-- ff000003: fab2 ↔ sup1, standard tier  → pl000001 (standard pricing)
INSERT INTO connection_price_lists (id, connection_id, price_list_id, assigned_by, assigned_at) VALUES
    ('cp000001-0000-0000-0000-000000000000',
     'ff000001-0000-0000-0000-000000000000',
     'pl000002-0000-0000-0000-000000000000',
     'aa000001-0000-0000-0000-000000000000',
     NOW() - INTERVAL '3 months'),

    ('cp000002-0000-0000-0000-000000000000',
     'ff000003-0000-0000-0000-000000000000',
     'pl000001-0000-0000-0000-000000000000',
     'aa000001-0000-0000-0000-000000000000',
     NOW() - INTERVAL '2 months');

RESET row_security;
