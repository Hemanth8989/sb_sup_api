-- ============================================================
-- V016: Seed price lists, price list items, and connection-price-list assignments
--
-- Price lists for supplier 1 (Marble Masters â€” 11111111-â€¦):
--   pl000001: Standard list â€” all 4 variants at list price
--   pl000002: Preferred list â€” Countertop Kings (fab1) discount pricing
--
-- Connection assignments:
--   ff000001 (fab1 â†” sup1, preferred tier) â†’ pl000002
--   ff000003 (fab2 â†” sup1, standard tier)  â†’ pl000001
-- ============================================================

SET row_security = OFF;

-- â”€â”€ Price lists â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO price_lists (id, tenant_id, name, tier, currency, is_active, created_at, updated_at) VALUES
    ('f0000001-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'Marble Masters Standard 2026', 'standard', 'USD', TRUE,
     NOW() - INTERVAL '3 months', NOW()),

    ('f0000002-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'Preferred Partner Pricing 2026', 'preferred', 'USD', TRUE,
     NOW() - INTERVAL '3 months', NOW());

-- â”€â”€ Price list items â€” Standard list â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
-- Carrara White 3cm Pol (dd000001) Â· Carrara White 2cm Pol (dd000002)
-- Calacatta Gold 3cm Pol (dd000003) Â· Absolute Black 3cm Pol (dd000004)
INSERT INTO price_list_items (id, price_list_id, variant_id, unit_price, currency, created_at) VALUES
    ('0a000001-0000-0000-0000-000000000000',
     'f0000001-0000-0000-0000-000000000000',
     'dd000001-0000-0000-0000-000000000000',
     28.50, 'USD', NOW() - INTERVAL '3 months'),

    ('0a000002-0000-0000-0000-000000000000',
     'f0000001-0000-0000-0000-000000000000',
     'dd000002-0000-0000-0000-000000000000',
     22.00, 'USD', NOW() - INTERVAL '3 months'),

    ('0a000003-0000-0000-0000-000000000000',
     'f0000001-0000-0000-0000-000000000000',
     'dd000003-0000-0000-0000-000000000000',
     85.00, 'USD', NOW() - INTERVAL '3 months'),

    ('0a000004-0000-0000-0000-000000000000',
     'f0000001-0000-0000-0000-000000000000',
     'dd000004-0000-0000-0000-000000000000',
     32.00, 'USD', NOW() - INTERVAL '3 months');

-- â”€â”€ Price list items â€” Preferred list (â‰ˆ10-12% below standard) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO price_list_items (id, price_list_id, variant_id, unit_price, currency, created_at) VALUES
    ('0a000005-0000-0000-0000-000000000000',
     'f0000002-0000-0000-0000-000000000000',
     'dd000001-0000-0000-0000-000000000000',
     25.50, 'USD', NOW() - INTERVAL '3 months'),

    ('0a000006-0000-0000-0000-000000000000',
     'f0000002-0000-0000-0000-000000000000',
     'dd000002-0000-0000-0000-000000000000',
     19.50, 'USD', NOW() - INTERVAL '3 months'),

    ('0a000007-0000-0000-0000-000000000000',
     'f0000002-0000-0000-0000-000000000000',
     'dd000003-0000-0000-0000-000000000000',
     76.00, 'USD', NOW() - INTERVAL '3 months'),

    ('0a000008-0000-0000-0000-000000000000',
     'f0000002-0000-0000-0000-000000000000',
     'dd000004-0000-0000-0000-000000000000',
     28.50, 'USD', NOW() - INTERVAL '3 months');

-- â”€â”€ Assign price lists to connections â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
-- ff000001: fab1 â†” sup1, preferred tier â†’ pl000002 (preferred pricing)
-- ff000003: fab2 â†” sup1, standard tier  â†’ pl000001 (standard pricing)
INSERT INTO connection_price_lists (id, connection_id, price_list_id, assigned_by, assigned_at) VALUES
    ('0c000001-0000-0000-0000-000000000000',
     'ff000001-0000-0000-0000-000000000000',
     'f0000002-0000-0000-0000-000000000000',
     'aa000001-0000-0000-0000-000000000000',
     NOW() - INTERVAL '3 months'),

    ('0c000002-0000-0000-0000-000000000000',
     'ff000003-0000-0000-0000-000000000000',
     'f0000001-0000-0000-0000-000000000000',
     'aa000001-0000-0000-0000-000000000000',
     NOW() - INTERVAL '2 months');

RESET row_security;

