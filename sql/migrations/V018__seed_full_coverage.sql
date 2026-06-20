-- ============================================================
-- V018: Full seed-data expansion — all tenants, all page tabs
--
-- New tenants:
--   66666666-… fab3  Mountain View Countertops
--   77777777-… fab4  Prestige Stone Works
--
-- New data per supplier:
--   Sup1 (Marble Masters):  slabs in all statuses, VIP price list,
--                           pending + suspended connections,
--                           POs in all statuses (acknowledged/shipped/received/cancelled)
--   Sup2 (Stone Source):    new quartzite slab variants, non-slab products,
--                           standard + preferred price lists, more connections
--   Sup3 (Premier Granite): Black Galaxy variant, non-slab products,
--                           standard + vip price lists, more connections + POs
--
-- UUID registry (hex series):
--   Tenants    66666666, 77777777
--   Users      aa000006, aa000007
--   Products   cc000008–cc000013 (slab), cc100001–cc100003 (sup2 supply),
--              cc200001–cc200003 (sup3 supply)
--   Variants   dd000009–dd000014 (slab), vr100001–vr100004 (sup2),
--              vr200001–vr200003 (sup3)
--   Slabs sup1 ee070001–ee070004 (Statuario), ee080001–ee080003 (Nero Marquina),
--              ee090001–ee090002 (extra sold/allocated)
--   Slabs sup2 ee0a0001–ee0a0003 (Azul Macauba), ee0b0001–ee0b0002 (White Macauba new)
--   Slabs sup3 ee0c0001–ee0c0003 (Black Galaxy), ee0d0001–ee0d0002 (Colonial White new)
--   Price lists pl000003 (sup1 vip), pl100001–pl100002 (sup2), pl200001–pl200002 (sup3)
--   Connections ff000005–ff000009
--   Addresses  ad000004–ad000005
--   Jobs       jb000003–jb000005
--   POs        po000003–po000009
--   PO lines   li000003–li000014
-- ============================================================

SET row_security = OFF;

-- ══════════════════════════════════════════════════════════════
-- 1. NEW FABRICATOR TENANTS
-- ══════════════════════════════════════════════════════════════

INSERT INTO tenants (id, type, name, slug, plan, country, is_active, created_at, updated_at) VALUES
    ('66666666-6666-6666-6666-666666666666', 'fabricator', 'Mountain View Countertops', 'mountain-view',  'starter', 'US', TRUE, NOW() - INTERVAL '2 months', NOW()),
    ('77777777-7777-7777-7777-777777777777', 'fabricator', 'Prestige Stone Works',      'prestige-stone', 'pro',     'US', TRUE, NOW() - INTERVAL '1 month',  NOW());

INSERT INTO users (id, tenant_id, clerk_user_id, email, full_name, role, is_active, created_at, updated_at) VALUES
    ('aa000006-0000-0000-0000-000000000000', '66666666-6666-6666-6666-666666666666', 'user_seed_fab3_owner', 'owner@mountainview.dev',  'Kevin Park',    'owner', TRUE, NOW() - INTERVAL '2 months', NOW()),
    ('aa000007-0000-0000-0000-000000000000', '77777777-7777-7777-7777-777777777777', 'user_seed_fab4_owner', 'owner@prestigestone.dev', 'Diana Walters', 'owner', TRUE, NOW() - INTERVAL '1 month',  NOW());

INSERT INTO fabricator_profiles (
    tenant_id, display_name, description, website, phone,
    address_line1, city, state_province, postal_code, country,
    shop_size, monthly_job_volume,
    notification_prefs, created_at, updated_at
) VALUES
(
    '66666666-6666-6666-6666-666666666666',
    'Mountain View Countertops',
    'Small residential shop serving the North Atlanta suburbs. Specialises in marble and quartzite installs.',
    'https://mountainview.example.com', '+1 (678) 555-0601',
    '1122 Mountain Industrial Blvd', 'Cumming', 'GA', '30041', 'US',
    'small', 22,
    '{"new_po":{"inApp":true,"email":true,"sms":false},"po_unacked_24h":{"inApp":true,"email":true,"sms":false},"connection_requested":{"inApp":true,"email":true,"sms":false},"connection_approved":{"inApp":true,"email":true,"sms":false},"price_changed":{"inApp":true,"email":false,"sms":false},"low_stock_warning":{"inApp":false,"email":false,"sms":false}}'::jsonb,
    NOW() - INTERVAL '2 months', NOW()
),
(
    '77777777-7777-7777-7777-777777777777',
    'Prestige Stone Works',
    'High-volume production shop. 20,000 sq ft facility, 3 CNC bridges. Serves commercial and residential.',
    'https://prestigestone.example.com', '+1 (770) 555-0701',
    '3800 Commerce Drive', 'Kennesaw', 'GA', '30144', 'US',
    'large', 120,
    '{"new_po":{"inApp":true,"email":true,"sms":true},"po_unacked_24h":{"inApp":true,"email":true,"sms":true},"connection_requested":{"inApp":true,"email":true,"sms":false},"connection_approved":{"inApp":true,"email":true,"sms":false},"price_changed":{"inApp":true,"email":true,"sms":false},"low_stock_warning":{"inApp":false,"email":false,"sms":false}}'::jsonb,
    NOW() - INTERVAL '1 month', NOW()
);

INSERT INTO addresses (id, tenant_id, label, line1, city, state_province, postal_code, country, is_default, created_at) VALUES
    ('ad000004-0000-0000-0000-000000000000', '66666666-6666-6666-6666-666666666666', 'Shop', '1122 Mountain Industrial Blvd', 'Cumming',  'GA', '30041', 'US', TRUE, NOW() - INTERVAL '2 months'),
    ('ad000005-0000-0000-0000-000000000000', '77777777-7777-7777-7777-777777777777', 'Shop', '3800 Commerce Drive',           'Kennesaw', 'GA', '30144', 'US', TRUE, NOW() - INTERVAL '1 month');

-- ══════════════════════════════════════════════════════════════
-- 2. NEW SLAB PRODUCTS & VARIANTS (all three suppliers)
-- ══════════════════════════════════════════════════════════════

-- ── Sup1: Statuario Marble + Nero Marquina ────────────────────
INSERT INTO products (id, tenant_id, category_code, name, brand, short_description, is_active, created_at, updated_at) VALUES
    ('cc000008-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'slab',
     'Statuario Marble', 'Marble Masters',
     'White Italian marble with dramatic bold grey veining. Considered the rarest and most prized Carrara variant.',
     TRUE, NOW() - INTERVAL '2 months', NOW()),

    ('cc000009-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'slab',
     'Nero Marquina Marble', 'Marble Masters',
     'Jet-black Spanish marble with bright white veining. Bold, elegant, and striking in any application.',
     TRUE, NOW() - INTERVAL '2 months', NOW());

-- ── Sup2: Azul Macauba + White Macauba Quartzite ─────────────
INSERT INTO products (id, tenant_id, category_code, name, brand, short_description, is_active, created_at, updated_at) VALUES
    ('cc000010-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'slab',
     'Azul Macauba Quartzite', 'Stone Source',
     'Brazilian quartzite with deep ocean blues and silvers. Extremely hard and suitable for exterior use.',
     TRUE, NOW() - INTERVAL '2 months', NOW()),

    ('cc000011-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'slab',
     'White Macauba Quartzite', 'Stone Source',
     'Creamy white quartzite with light grey veining from Minas Gerais. Softer look than Nordic White.',
     TRUE, NOW() - INTERVAL '2 months', NOW());

-- ── Sup3: Black Galaxy Granite + Colonial White Granite ───────
INSERT INTO products (id, tenant_id, category_code, name, brand, short_description, is_active, created_at, updated_at) VALUES
    ('cc000012-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'slab',
     'Black Galaxy Granite', 'Premier Granite',
     'Black granite from Andhra Pradesh, India. Golden star-like crystals throughout. Very popular for kitchen islands.',
     TRUE, NOW() - INTERVAL '3 months', NOW()),

    ('cc000013-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'slab',
     'Colonial White Granite', 'Premier Granite',
     'White granite with grey and burgundy flecks from Brazil. Consistent movement and very reliable stock.',
     TRUE, NOW() - INTERVAL '3 months', NOW());

-- ── Slab variants ─────────────────────────────────────────────
INSERT INTO product_variants (
    id, product_id, tenant_id, sku, variant_name, attributes,
    unit_of_measure, base_price, currency, qty_available,
    is_slab_variant, status, lead_time_days, created_at, updated_at
) VALUES
    -- Sup1: Statuario
    ('dd000009-0000-0000-0000-000000000000', 'cc000008-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'MM-ST-3CM-POL', 'Statuario 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"white","pattern":"veined"}'::jsonb,
     'sqft', 135.00, 'USD', NULL, TRUE, 'active', 7, NOW() - INTERVAL '2 months', NOW()),

    -- Sup1: Nero Marquina
    ('dd000010-0000-0000-0000-000000000000', 'cc000009-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'MM-NM-3CM-POL', 'Nero Marquina 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"black","pattern":"veined"}'::jsonb,
     'sqft', 72.00, 'USD', NULL, TRUE, 'active', 5, NOW() - INTERVAL '2 months', NOW()),

    -- Sup2: Azul Macauba
    ('dd000011-0000-0000-0000-000000000000', 'cc000010-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'SS-AM-3CM-POL', 'Azul Macauba 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"blue","pattern":"exotic"}'::jsonb,
     'sqft', 195.00, 'USD', NULL, TRUE, 'active', 10, NOW() - INTERVAL '2 months', NOW()),

    -- Sup2: White Macauba
    ('dd000012-0000-0000-0000-000000000000', 'cc000011-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'SS-WM-3CM-HON', 'White Macauba 3cm Honed',
     '{"thickness_cm":3,"finish":"honed","color_family":"white","pattern":"veined"}'::jsonb,
     'sqft', 88.00, 'USD', NULL, TRUE, 'active', 8, NOW() - INTERVAL '2 months', NOW()),

    -- Sup3: Black Galaxy
    ('dd000013-0000-0000-0000-000000000000', 'cc000012-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'PG-BG-3CM-POL', 'Black Galaxy 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"black","pattern":"speckled"}'::jsonb,
     'sqft', 24.00, 'USD', NULL, TRUE, 'active', 2, NOW() - INTERVAL '3 months', NOW()),

    -- Sup3: Colonial White
    ('dd000014-0000-0000-0000-000000000000', 'cc000013-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'PG-CW-3CM-POL', 'Colonial White 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"white","pattern":"flecked"}'::jsonb,
     'sqft', 14.00, 'USD', NULL, TRUE, 'active', 2, NOW() - INTERVAL '3 months', NOW());

-- ══════════════════════════════════════════════════════════════
-- 3. NEW SLABS — MARBLE MASTERS (sup1) covering all statuses
-- ══════════════════════════════════════════════════════════════

-- Statuario (dd000009) — available (2) + allocated (2) + hold (1)
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee070001-0000-0000-0000-000000000000', 'dd000009-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'ST3P-001', 'marble', 'Statuario', 'white', 'veined', 'IT', 'Henraux Quarry', 'LOT-2026-0022', 'BLK-ST-01A',
     3.0, 'polished', 3300, 1700, NULL, 'bb000001-0000-0000-0000-000000000000', 'F-01-L',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '7 weeks', NOW()),

    ('ee070002-0000-0000-0000-000000000000', 'dd000009-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'ST3P-002', 'marble', 'Statuario', 'white', 'veined', 'IT', 'Henraux Quarry', 'LOT-2026-0022', 'BLK-ST-01A',
     3.0, 'polished', 3250, 1680, NULL, 'bb000001-0000-0000-0000-000000000000', 'F-01-R',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '7 weeks', NOW()),

    ('ee070003-0000-0000-0000-000000000000', 'dd000009-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'ST3P-003', 'marble', 'Statuario', 'white', 'veined', 'IT', 'Henraux Quarry', 'LOT-2026-0022', 'BLK-ST-01B',
     3.0, 'polished', 3400, 1720, 140.00, 'bb000001-0000-0000-0000-000000000000', 'F-02-L',
     'A', 'allocated', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '7 weeks', NOW() - INTERVAL '10 days'),

    ('ee070004-0000-0000-0000-000000000000', 'dd000009-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'ST3P-004', 'marble', 'Statuario', 'white', 'veined', 'IT', 'Cava Michelangelo', 'LOT-2026-0031', 'BLK-ST-02A',
     3.0, 'polished', 3100, 1600, NULL, 'bb000001-0000-0000-0000-000000000000', 'F-02-R',
     'B', 'allocated', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '6 weeks', NOW() - INTERVAL '10 days'),

    ('ee070005-0000-0000-0000-000000000000', 'dd000009-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'ST3P-005', 'marble', 'Statuario', 'white', 'veined', 'IT', 'Cava Michelangelo', 'LOT-2026-0031', 'BLK-ST-02B',
     3.0, 'polished', 3050, 1580, NULL, 'bb000002-0000-0000-0000-000000000000', 'G-01-L',
     'B', 'hold', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '6 weeks', NOW() - INTERVAL '3 days');

-- Nero Marquina (dd000010) — available (2) + shipped (2) + sold (2)
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee080001-0000-0000-0000-000000000000', 'dd000010-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'NM3P-001', 'marble', 'Nero Marquina', 'black', 'veined', 'ES', 'Markina Quarry', 'LOT-2025-0770', 'BLK-NM-01A',
     3.0, 'polished', 3000, 1550, NULL, 'bb000001-0000-0000-0000-000000000000', 'G-02-L',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '5 weeks', NOW()),

    ('ee080002-0000-0000-0000-000000000000', 'dd000010-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'NM3P-002', 'marble', 'Nero Marquina', 'black', 'veined', 'ES', 'Markina Quarry', 'LOT-2025-0770', 'BLK-NM-01A',
     3.0, 'polished', 2950, 1520, NULL, 'bb000001-0000-0000-0000-000000000000', 'G-02-R',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '5 weeks', NOW()),

    ('ee080003-0000-0000-0000-000000000000', 'dd000010-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'NM3P-003', 'marble', 'Nero Marquina', 'black', 'veined', 'ES', 'Markina Quarry', 'LOT-2025-0770', 'BLK-NM-01B',
     3.0, 'polished', 3100, 1560, 75.00, 'bb000001-0000-0000-0000-000000000000', 'G-03-L',
     'A', 'shipped', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '10 weeks', NOW() - INTERVAL '2 weeks'),

    ('ee080004-0000-0000-0000-000000000000', 'dd000010-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'NM3P-004', 'marble', 'Nero Marquina', 'black', 'veined', 'ES', 'Markina Quarry', 'LOT-2025-0700', 'BLK-NM-02A',
     3.0, 'polished', 3200, 1600, 75.00, 'bb000001-0000-0000-0000-000000000000', 'G-03-R',
     'A', 'shipped', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '10 weeks', NOW() - INTERVAL '2 weeks'),

    ('ee080005-0000-0000-0000-000000000000', 'dd000010-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'NM3P-005', 'marble', 'Nero Marquina', 'black', 'veined', 'ES', 'Markina Quarry', 'LOT-2025-0600', 'BLK-NM-03A',
     3.0, 'polished', 3050, 1540, 70.00, 'bb000002-0000-0000-0000-000000000000', NULL,
     'A', 'sold', FALSE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '5 months', NOW() - INTERVAL '4 months'),

    ('ee080006-0000-0000-0000-000000000000', 'dd000010-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'NM3P-006', 'marble', 'Nero Marquina', 'black', 'veined', 'ES', 'Markina Quarry', 'LOT-2025-0600', 'BLK-NM-03B',
     3.0, 'polished', 2900, 1500, 70.00, 'bb000002-0000-0000-0000-000000000000', NULL,
     'A', 'sold', FALSE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '5 months', NOW() - INTERVAL '4 months');

-- Extra Carrara White slabs in sold + allocated statuses (to round out status coverage)
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee090001-0000-0000-0000-000000000000', 'dd000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'CW3P-006', 'marble', 'Carrara White', 'white', 'veined', 'IT', 'Fantiscritti Quarry', 'LOT-2024-0900', 'BLK-77C',
     3.0, 'polished', 3100, 1600, 28.50, 'bb000001-0000-0000-0000-000000000000', NULL,
     'A', 'sold', FALSE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '4 months', NOW() - INTERVAL '3 months'),

    ('ee090002-0000-0000-0000-000000000000', 'dd000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'CW3P-007', 'marble', 'Carrara White', 'white', 'veined', 'IT', 'Fantiscritti Quarry', 'LOT-2024-0900', 'BLK-77D',
     3.0, 'polished', 3200, 1650, 28.50, 'bb000001-0000-0000-0000-000000000000', NULL,
     'A', 'sold', FALSE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '4 months', NOW() - INTERVAL '3 months');

-- ══════════════════════════════════════════════════════════════
-- 4. NEW SLABS — STONE SOURCE (sup2)
-- ══════════════════════════════════════════════════════════════

-- Azul Macauba (dd000011) — available + reserved + hold
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee0a0001-0000-0000-0000-000000000000', 'dd000011-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'AM3P-001', 'quartzite', 'Azul Macauba', 'blue', 'exotic', 'BR', 'Minas Gerais Quarry', 'LOT-2026-0044', 'BLK-AM-01',
     3.0, 'polished', 3200, 1680, NULL, 'bb000003-0000-0000-0000-000000000000', 'C-01-L',
     'A', 'available', TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '6 weeks', NOW()),

    ('ee0a0002-0000-0000-0000-000000000000', 'dd000011-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'AM3P-002', 'quartzite', 'Azul Macauba', 'blue', 'exotic', 'BR', 'Minas Gerais Quarry', 'LOT-2026-0044', 'BLK-AM-01',
     3.0, 'polished', 3100, 1620, NULL, 'bb000003-0000-0000-0000-000000000000', 'C-01-R',
     'A', 'available', TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '6 weeks', NOW()),

    ('ee0a0003-0000-0000-0000-000000000000', 'dd000011-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'AM3P-003', 'quartzite', 'Azul Macauba', 'blue', 'exotic', 'BR', 'Minas Gerais Quarry', 'LOT-2026-0044', 'BLK-AM-02',
     3.0, 'polished', 3300, 1700, 200.00, 'bb000003-0000-0000-0000-000000000000', 'C-02-L',
     'A', 'hold', TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '5 weeks', NOW() - INTERVAL '1 week');

-- White Macauba (dd000012) — available + shipped + sold
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee0b0001-0000-0000-0000-000000000000', 'dd000012-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'WM3H-001', 'quartzite', 'White Macauba', 'white', 'veined', 'BR', 'Espirito Santo Region', 'LOT-2026-0060', 'BLK-WM-01',
     3.0, 'honed', 3150, 1640, NULL, 'bb000003-0000-0000-0000-000000000000', 'D-01-L',
     'A', 'available', TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '4 weeks', NOW()),

    ('ee0b0002-0000-0000-0000-000000000000', 'dd000012-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'WM3H-002', 'quartzite', 'White Macauba', 'white', 'veined', 'BR', 'Espirito Santo Region', 'LOT-2026-0060', 'BLK-WM-01',
     3.0, 'honed', 3200, 1650, NULL, 'bb000003-0000-0000-0000-000000000000', 'D-01-R',
     'A', 'available', TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '4 weeks', NOW()),

    ('ee0b0003-0000-0000-0000-000000000000', 'dd000012-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'WM3H-003', 'quartzite', 'White Macauba', 'white', 'veined', 'BR', 'Espirito Santo Region', 'LOT-2025-0900', 'BLK-WM-02',
     3.0, 'honed', 3000, 1600, 88.00, 'bb000003-0000-0000-0000-000000000000', NULL,
     'A', 'shipped', TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '3 months', NOW() - INTERVAL '3 weeks'),

    ('ee0b0004-0000-0000-0000-000000000000', 'dd000012-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'WM3H-004', 'quartzite', 'White Macauba', 'white', 'veined', 'BR', 'Espirito Santo Region', 'LOT-2025-0800', 'BLK-WM-03',
     3.0, 'honed', 3100, 1600, 85.00, 'bb000003-0000-0000-0000-000000000000', NULL,
     'A', 'sold', FALSE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '5 months', NOW() - INTERVAL '4 months');

-- Reserve one Blue Bahia slab for a PO we'll create below
UPDATE slabs
SET    status         = 'reserved',
       reserved_for_po = 'po000008-0000-0000-0000-000000000000',
       status_changed  = NOW() - INTERVAL '1 week',
       updated_at      = NOW() - INTERVAL '1 week'
WHERE  id = 'ee030001-0000-0000-0000-000000000000';

-- ══════════════════════════════════════════════════════════════
-- 5. NEW SLABS — PREMIER GRANITE (sup3)
-- ══════════════════════════════════════════════════════════════

-- Black Galaxy (dd000013) — available + allocated + sold
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee0c0001-0000-0000-0000-000000000000', 'dd000013-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'BG3P-001', 'granite', 'Black Galaxy', 'black', 'speckled', 'IN', 'Andhra Pradesh Quarry', 'LOT-2026-0088', 'BLK-BG-01',
     3.0, 'polished', 3050, 1600, NULL, 'bb000004-0000-0000-0000-000000000000', 'C-01-L',
     'A', 'available', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '5 weeks', NOW()),

    ('ee0c0002-0000-0000-0000-000000000000', 'dd000013-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'BG3P-002', 'granite', 'Black Galaxy', 'black', 'speckled', 'IN', 'Andhra Pradesh Quarry', 'LOT-2026-0088', 'BLK-BG-01',
     3.0, 'polished', 3100, 1620, NULL, 'bb000004-0000-0000-0000-000000000000', 'C-01-R',
     'A', 'available', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '5 weeks', NOW()),

    ('ee0c0003-0000-0000-0000-000000000000', 'dd000013-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'BG3P-003', 'granite', 'Black Galaxy', 'black', 'speckled', 'IN', 'Andhra Pradesh Quarry', 'LOT-2026-0088', 'BLK-BG-02',
     3.0, 'polished', 3200, 1650, NULL, 'bb000004-0000-0000-0000-000000000000', 'C-02-L',
     'A', 'available', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '5 weeks', NOW()),

    ('ee0c0004-0000-0000-0000-000000000000', 'dd000013-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'BG3P-004', 'granite', 'Black Galaxy', 'black', 'speckled', 'IN', 'Andhra Pradesh Quarry', 'LOT-2025-0700', 'BLK-BG-03',
     3.0, 'polished', 3000, 1580, 25.00, 'bb000004-0000-0000-0000-000000000000', NULL,
     'A', 'shipped', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '3 months', NOW() - INTERVAL '3 weeks'),

    ('ee0c0005-0000-0000-0000-000000000000', 'dd000013-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'BG3P-005', 'granite', 'Black Galaxy', 'black', 'speckled', 'IN', 'Andhra Pradesh Quarry', 'LOT-2025-0600', 'BLK-BG-04',
     3.0, 'polished', 3100, 1600, 22.00, 'bb000005-0000-0000-0000-000000000000', NULL,
     'A', 'sold', FALSE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '6 months', NOW() - INTERVAL '5 months');

-- Colonial White (dd000014) — available + hold + reserved
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee0d0001-0000-0000-0000-000000000000', 'dd000014-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'COW3P-001', 'granite', 'Colonial White', 'white', 'flecked', 'BR', 'Espirito Santo Mine', 'LOT-2026-0099', 'BLK-CW-01',
     3.0, 'polished', 3050, 1600, NULL, 'bb000004-0000-0000-0000-000000000000', 'D-01-L',
     'A', 'available', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '4 weeks', NOW()),

    ('ee0d0002-0000-0000-0000-000000000000', 'dd000014-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'COW3P-002', 'granite', 'Colonial White', 'white', 'flecked', 'BR', 'Espirito Santo Mine', 'LOT-2026-0099', 'BLK-CW-01',
     3.0, 'polished', 3100, 1620, NULL, 'bb000004-0000-0000-0000-000000000000', 'D-01-R',
     'A', 'available', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '4 weeks', NOW()),

    ('ee0d0003-0000-0000-0000-000000000000', 'dd000014-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'COW3P-003', 'granite', 'Colonial White', 'white', 'flecked', 'BR', 'Espirito Santo Mine', 'LOT-2026-0099', 'BLK-CW-02',
     3.0, 'polished', 3000, 1580, NULL, 'bb000004-0000-0000-0000-000000000000', 'D-02-L',
     'B', 'hold', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '3 weeks', NOW() - INTERVAL '2 days'),

    ('ee0d0004-0000-0000-0000-000000000000', 'dd000014-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'COW3P-004', 'granite', 'Colonial White', 'white', 'flecked', 'BR', 'Espirito Santo Mine', 'LOT-2025-0800', 'BLK-CW-03',
     3.0, 'polished', 3200, 1650, 14.50, 'bb000005-0000-0000-0000-000000000000', NULL,
     'A', 'sold', FALSE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '7 months', NOW() - INTERVAL '6 months');

-- ══════════════════════════════════════════════════════════════
-- 6. NON-SLAB PRODUCTS — STONE SOURCE (sup2)
-- ══════════════════════════════════════════════════════════════

INSERT INTO products (id, tenant_id, category_code, name, brand, short_description, is_active, created_at, updated_at) VALUES
    ('cc100001-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'sealer', 'Bulletproof Stone Sealer', 'StoneTech',
     'Water and oil-based stain protection for quartzite and natural stone. Quart covers up to 600 sqft.',
     TRUE, NOW() - INTERVAL '6 weeks', NOW()),

    ('cc100002-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'polishing_compound', 'Diamond Polishing Powder', 'Tenax',
     '3,000 grit diamond powder for restoring high-gloss finish on marble and quartzite. 250g tin.',
     TRUE, NOW() - INTERVAL '6 weeks', NOW()),

    ('cc100003-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'adhesive', 'Akemi Color Fast Adhesive', 'Akemi',
     'Polyester adhesive with color-matching granules for transparent, beige, and black stone repairs.',
     TRUE, NOW() - INTERVAL '5 weeks', NOW());

INSERT INTO product_variants (
    id, product_id, tenant_id, sku, variant_name, attributes,
    unit_of_measure, base_price, currency, qty_available, qty_reserved,
    is_slab_variant, status, lead_time_days, created_at, updated_at
) VALUES
    ('vr100001-0000-0000-0000-000000000000',
     'cc100001-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'ST-BP-QT', 'StoneTech Bulletproof Quart',
     '{"volume_oz":32,"coverage_sqft":600}'::jsonb,
     'each', 32.00, 'USD', 18, 0, FALSE, 'active', 3,
     NOW() - INTERVAL '6 weeks', NOW()),

    ('vr100002-0000-0000-0000-000000000000',
     'cc100002-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'TX-DP-3K-250G', 'Tenax Diamond Powder 3000 Grit 250g',
     '{"grit":3000,"weight_g":250}'::jsonb,
     'each', 48.00, 'USD', 12, 0, FALSE, 'active', 5,
     NOW() - INTERVAL '6 weeks', NOW()),

    ('vr100003-0000-0000-0000-000000000000',
     'cc100002-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'TX-DP-1K-250G', 'Tenax Diamond Powder 1000 Grit 250g',
     '{"grit":1000,"weight_g":250}'::jsonb,
     'each', 38.00, 'USD', 8, 0, FALSE, 'active', 5,
     NOW() - INTERVAL '6 weeks', NOW()),

    ('vr100004-0000-0000-0000-000000000000',
     'cc100003-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'AK-CF-BEIGE-1L', 'Akemi Color Fast Beige 1L',
     '{"color":"beige","base":"polyester","volume_ml":1000}'::jsonb,
     'each', 42.00, 'USD', 2, 0, FALSE, 'active', 4,
     NOW() - INTERVAL '5 weeks', NOW());

-- ══════════════════════════════════════════════════════════════
-- 7. NON-SLAB PRODUCTS — PREMIER GRANITE (sup3)
-- ══════════════════════════════════════════════════════════════

INSERT INTO products (id, tenant_id, category_code, name, brand, short_description, is_active, created_at, updated_at) VALUES
    ('cc200001-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'blade', 'Granite Cutting Diamond Blade 14"', 'Husqvarna',
     'Professional-grade segmented diamond blade for granite and quartzite. 14" wet-cut.',
     TRUE, NOW() - INTERVAL '8 weeks', NOW()),

    ('cc200002-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'polishing_compound', 'Supreme Granite Polish', 'Faber',
     'Liquid polishing compound for granite and engineered stone. Restores mirror finish. 1L bottle.',
     TRUE, NOW() - INTERVAL '8 weeks', NOW()),

    ('cc200003-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'sealer', 'SCI Ultra Premium Sealer', 'SCI',
     'Solvent-based penetrating sealer offering 15-year protection on granite and quartzite. Gallon.',
     TRUE, NOW() - INTERVAL '7 weeks', NOW());

INSERT INTO product_variants (
    id, product_id, tenant_id, sku, variant_name, attributes,
    unit_of_measure, base_price, currency, qty_available, qty_reserved,
    is_slab_variant, status, lead_time_days, created_at, updated_at
) VALUES
    ('vr200001-0000-0000-0000-000000000000',
     'cc200001-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'HU-DB-14-SEG', 'Husqvarna 14" Segmented Diamond Blade',
     '{"diameter_in":14,"arbor_in":1,"segment":"standard","cut_type":"wet"}'::jsonb,
     'each', 165.00, 'USD', 14, 0, FALSE, 'active', 3,
     NOW() - INTERVAL '8 weeks', NOW()),

    ('vr200002-0000-0000-0000-000000000000',
     'cc200002-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'FB-SGP-1L', 'Faber Supreme Granite Polish 1L',
     '{"volume_ml":1000,"application":"spray_and_buff"}'::jsonb,
     'each', 26.00, 'USD', 30, 0, FALSE, 'active', 2,
     NOW() - INTERVAL '8 weeks', NOW()),

    ('vr200003-0000-0000-0000-000000000000',
     'cc200003-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'SCI-UP-GAL', 'SCI Ultra Premium Sealer Gallon',
     '{"volume_oz":128,"coverage_sqft":2500,"protection_years":15}'::jsonb,
     'gallon', 110.00, 'USD', 0, 0, FALSE, 'out_of_stock', 5,
     NOW() - INTERVAL '7 weeks', NOW());

-- ══════════════════════════════════════════════════════════════
-- 8. PRICE LISTS
-- ══════════════════════════════════════════════════════════════

-- Sup1: add VIP tier
INSERT INTO price_lists (id, tenant_id, name, tier, currency, is_active, created_at, updated_at) VALUES
    ('pl000003-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'VIP Accounts 2026', 'vip', 'USD', TRUE,
     NOW() - INTERVAL '2 months', NOW());

INSERT INTO price_list_items (id, price_list_id, variant_id, unit_price, currency, created_at) VALUES
    ('pi000009-0000-0000-0000-000000000000',  'pl000003-0000-0000-0000-000000000000', 'dd000001-0000-0000-0000-000000000000', 23.00, 'USD', NOW() - INTERVAL '2 months'),
    ('pi000010-0000-0000-0000-000000000000',  'pl000003-0000-0000-0000-000000000000', 'dd000002-0000-0000-0000-000000000000', 18.00, 'USD', NOW() - INTERVAL '2 months'),
    ('pi000011-0000-0000-0000-000000000000',  'pl000003-0000-0000-0000-000000000000', 'dd000003-0000-0000-0000-000000000000', 70.00, 'USD', NOW() - INTERVAL '2 months'),
    ('pi000012-0000-0000-0000-000000000000',  'pl000003-0000-0000-0000-000000000000', 'dd000004-0000-0000-0000-000000000000', 26.00, 'USD', NOW() - INTERVAL '2 months'),
    ('pi000013-0000-0000-0000-000000000000',  'pl000003-0000-0000-0000-000000000000', 'dd000009-0000-0000-0000-000000000000', 118.00,'USD', NOW() - INTERVAL '2 months'),
    ('pi000014-0000-0000-0000-000000000000',  'pl000003-0000-0000-0000-000000000000', 'dd000010-0000-0000-0000-000000000000', 62.00, 'USD', NOW() - INTERVAL '2 months');

-- Sup2: standard + preferred
INSERT INTO price_lists (id, tenant_id, name, tier, currency, is_active, created_at, updated_at) VALUES
    ('pl100001-0000-0000-0000-000000000000',
     '22222222-2222-2222-2222-222222222222',
     'Stone Source Standard 2026', 'standard', 'USD', TRUE,
     NOW() - INTERVAL '4 months', NOW()),

    ('pl100002-0000-0000-0000-000000000000',
     '22222222-2222-2222-2222-222222222222',
     'Stone Source Preferred 2026', 'preferred', 'USD', TRUE,
     NOW() - INTERVAL '4 months', NOW());

INSERT INTO price_list_items (id, price_list_id, variant_id, unit_price, currency, created_at) VALUES
    -- Standard
    ('pi100001-0000-0000-0000-000000000000', 'pl100001-0000-0000-0000-000000000000', 'dd000005-0000-0000-0000-000000000000', 145.00, 'USD', NOW() - INTERVAL '4 months'),
    ('pi100002-0000-0000-0000-000000000000', 'pl100001-0000-0000-0000-000000000000', 'dd000006-0000-0000-0000-000000000000', 68.00,  'USD', NOW() - INTERVAL '4 months'),
    ('pi100003-0000-0000-0000-000000000000', 'pl100001-0000-0000-0000-000000000000', 'dd000011-0000-0000-0000-000000000000', 195.00, 'USD', NOW() - INTERVAL '4 months'),
    ('pi100004-0000-0000-0000-000000000000', 'pl100001-0000-0000-0000-000000000000', 'dd000012-0000-0000-0000-000000000000', 88.00,  'USD', NOW() - INTERVAL '4 months'),
    -- Preferred (≈10% off)
    ('pi100005-0000-0000-0000-000000000000', 'pl100002-0000-0000-0000-000000000000', 'dd000005-0000-0000-0000-000000000000', 130.00, 'USD', NOW() - INTERVAL '4 months'),
    ('pi100006-0000-0000-0000-000000000000', 'pl100002-0000-0000-0000-000000000000', 'dd000006-0000-0000-0000-000000000000', 61.00,  'USD', NOW() - INTERVAL '4 months'),
    ('pi100007-0000-0000-0000-000000000000', 'pl100002-0000-0000-0000-000000000000', 'dd000011-0000-0000-0000-000000000000', 175.00, 'USD', NOW() - INTERVAL '4 months'),
    ('pi100008-0000-0000-0000-000000000000', 'pl100002-0000-0000-0000-000000000000', 'dd000012-0000-0000-0000-000000000000', 79.00,  'USD', NOW() - INTERVAL '4 months');

-- Sup3: standard + vip
INSERT INTO price_lists (id, tenant_id, name, tier, currency, is_active, created_at, updated_at) VALUES
    ('pl200001-0000-0000-0000-000000000000',
     '33333333-3333-3333-3333-333333333333',
     'Premier Granite Standard 2026', 'standard', 'USD', TRUE,
     NOW() - INTERVAL '6 months', NOW()),

    ('pl200002-0000-0000-0000-000000000000',
     '33333333-3333-3333-3333-333333333333',
     'Premier Granite VIP 2026', 'vip', 'USD', TRUE,
     NOW() - INTERVAL '6 months', NOW());

INSERT INTO price_list_items (id, price_list_id, variant_id, unit_price, currency, created_at) VALUES
    -- Standard
    ('pi200001-0000-0000-0000-000000000000', 'pl200001-0000-0000-0000-000000000000', 'dd000007-0000-0000-0000-000000000000', 18.50, 'USD', NOW() - INTERVAL '6 months'),
    ('pi200002-0000-0000-0000-000000000000', 'pl200001-0000-0000-0000-000000000000', 'dd000008-0000-0000-0000-000000000000', 16.00, 'USD', NOW() - INTERVAL '6 months'),
    ('pi200003-0000-0000-0000-000000000000', 'pl200001-0000-0000-0000-000000000000', 'dd000013-0000-0000-0000-000000000000', 24.00, 'USD', NOW() - INTERVAL '6 months'),
    ('pi200004-0000-0000-0000-000000000000', 'pl200001-0000-0000-0000-000000000000', 'dd000014-0000-0000-0000-000000000000', 14.00, 'USD', NOW() - INTERVAL '6 months'),
    -- VIP (≈15% off)
    ('pi200005-0000-0000-0000-000000000000', 'pl200002-0000-0000-0000-000000000000', 'dd000007-0000-0000-0000-000000000000', 15.75, 'USD', NOW() - INTERVAL '6 months'),
    ('pi200006-0000-0000-0000-000000000000', 'pl200002-0000-0000-0000-000000000000', 'dd000008-0000-0000-0000-000000000000', 13.50, 'USD', NOW() - INTERVAL '6 months'),
    ('pi200007-0000-0000-0000-000000000000', 'pl200002-0000-0000-0000-000000000000', 'dd000013-0000-0000-0000-000000000000', 20.00, 'USD', NOW() - INTERVAL '6 months'),
    ('pi200008-0000-0000-0000-000000000000', 'pl200002-0000-0000-0000-000000000000', 'dd000014-0000-0000-0000-000000000000', 11.75, 'USD', NOW() - INTERVAL '6 months');

-- ══════════════════════════════════════════════════════════════
-- 9. CONNECTIONS — cover all statuses for sup1 + link new tenants
-- ══════════════════════════════════════════════════════════════

INSERT INTO connections (
    id, fabricator_id, supplier_id, status, pricing_tier,
    initiated_by, approved_by, request_message,
    requested_at, connected_at, suspended_at,
    created_at, updated_at
) VALUES
    -- fab1 ↔ sup3: active standard — Countertop Kings expands to Premier Granite
    ('ff000005-0000-0000-0000-000000000000',
     '44444444-4444-4444-4444-444444444444', '33333333-3333-3333-3333-333333333333',
     'active', 'standard',
     'aa000004-0000-0000-0000-000000000000', 'aa000003-0000-0000-0000-000000000000',
     'Looking for a reliable granite source for volume commercial work. Your stock depth looks great.',
     NOW() - INTERVAL '3 months', NOW() - INTERVAL '3 months' + INTERVAL '2 days',
     NULL,
     NOW() - INTERVAL '3 months', NOW()),

    -- fab2 ↔ sup2: active preferred — Elite Surfaces sources exotic quartzite from Stone Source
    ('ff000006-0000-0000-0000-000000000000',
     '55555555-5555-5555-5555-555555555555', '22222222-2222-2222-2222-222222222222',
     'active', 'preferred',
     'aa000005-0000-0000-0000-000000000000', 'aa000002-0000-0000-0000-000000000000',
     'We do high-end installs and need access to your Blue Bahia and Azul Macauba lots.',
     NOW() - INTERVAL '5 weeks', NOW() - INTERVAL '5 weeks' + INTERVAL '1 day',
     NULL,
     NOW() - INTERVAL '5 weeks', NOW()),

    -- fab3 (Mountain View) ↔ sup1: PENDING — new shop requesting access to Marble Masters
    ('ff000007-0000-0000-0000-000000000000',
     '66666666-6666-6666-6666-666666666666', '11111111-1111-1111-1111-111111111111',
     'pending', 'standard',
     'aa000006-0000-0000-0000-000000000000', NULL,
     'We recently opened and specialize in marble installs. Your Italian inventory is exactly what our clients want. Would love to be a partner.',
     NOW() - INTERVAL '4 days', NULL,
     NULL,
     NOW() - INTERVAL '4 days', NOW()),

    -- fab4 (Prestige Stone Works) ↔ sup1: SUSPENDED — was active, now suspended for late payment
    ('ff000008-0000-0000-0000-000000000000',
     '77777777-7777-7777-7777-777777777777', '11111111-1111-1111-1111-111111111111',
     'suspended', 'preferred',
     'aa000007-0000-0000-0000-000000000000', 'aa000001-0000-0000-0000-000000000000',
     'High-volume shop looking for preferred tier access to your full marble catalogue.',
     NOW() - INTERVAL '5 months', NOW() - INTERVAL '5 months' + INTERVAL '1 day',
     NOW() - INTERVAL '2 weeks',
     NOW() - INTERVAL '5 months', NOW()),

    -- fab4 (Prestige) ↔ sup3: active vip — large shop gets VIP with Premier Granite
    ('ff000009-0000-0000-0000-000000000000',
     '77777777-7777-7777-7777-777777777777', '33333333-3333-3333-3333-333333333333',
     'active', 'vip',
     'aa000007-0000-0000-0000-000000000000', 'aa000003-0000-0000-0000-000000000000',
     'High-volume production shop, 120 jobs/month. Requesting VIP pricing for long-term commitment.',
     NOW() - INTERVAL '3 months', NOW() - INTERVAL '3 months' + INTERVAL '1 day',
     NULL,
     NOW() - INTERVAL '3 months', NOW());

-- Connection history
INSERT INTO connection_history (id, connection_id, from_status, to_status, changed_by, reason, changed_at) VALUES
    ('cf000004-0000-0000-0000-000000000000', 'ff000005-0000-0000-0000-000000000000', 'pending', 'active',    'aa000003-0000-0000-0000-000000000000', NULL,                           NOW() - INTERVAL '3 months' + INTERVAL '2 days'),
    ('cf000005-0000-0000-0000-000000000000', 'ff000006-0000-0000-0000-000000000000', 'pending', 'active',    'aa000002-0000-0000-0000-000000000000', NULL,                           NOW() - INTERVAL '5 weeks' + INTERVAL '1 day'),
    ('cf000006-0000-0000-0000-000000000000', 'ff000008-0000-0000-0000-000000000000', 'pending', 'active',    'aa000001-0000-0000-0000-000000000000', NULL,                           NOW() - INTERVAL '5 months' + INTERVAL '1 day'),
    ('cf000007-0000-0000-0000-000000000000', 'ff000008-0000-0000-0000-000000000000', 'active',  'suspended', 'aa000001-0000-0000-0000-000000000000', 'Account past due — 60+ days.', NOW() - INTERVAL '2 weeks'),
    ('cf000008-0000-0000-0000-000000000000', 'ff000009-0000-0000-0000-000000000000', 'pending', 'active',    'aa000003-0000-0000-0000-000000000000', NULL,                           NOW() - INTERVAL '3 months' + INTERVAL '1 day');

-- Assign price lists to the new active connections
INSERT INTO connection_price_lists (id, connection_id, price_list_id, assigned_by, assigned_at) VALUES
    ('cp000003-0000-0000-0000-000000000000', 'ff000005-0000-0000-0000-000000000000', 'pl200001-0000-0000-0000-000000000000', 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '3 months'),
    ('cp000004-0000-0000-0000-000000000000', 'ff000006-0000-0000-0000-000000000000', 'pl100002-0000-0000-0000-000000000000', 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '5 weeks'),
    ('cp000005-0000-0000-0000-000000000000', 'ff000009-0000-0000-0000-000000000000', 'pl200002-0000-0000-0000-000000000000', 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '3 months');

-- ══════════════════════════════════════════════════════════════
-- 10. JOBS — fill out fab2, fab3, fab4
-- ══════════════════════════════════════════════════════════════

INSERT INTO jobs (
    id, tenant_id, job_number, job_name,
    customer_name, customer_email, customer_phone,
    status, template_date, fabrication_date, install_date,
    material_budget, notes, created_by, created_at, updated_at
) VALUES
    -- fab2 (Elite Surfaces)
    ('jb000003-0000-0000-0000-000000000000',
     '55555555-5555-5555-5555-555555555555',
     'ESS-2026-0001', 'Williams Luxury Kitchen',
     'Patricia Williams', 'pat.williams@email.example.com', '+1 (678) 555-7701',
     'fabricating', '2026-06-08', '2026-06-18', '2026-06-26',
     12000.00,
     'Full Blue Bahia waterfall island + perimeter Calacatta. Very high-end project — handle with care.',
     'aa000005-0000-0000-0000-000000000000',
     NOW() - INTERVAL '4 weeks', NOW()),

    ('jb000004-0000-0000-0000-000000000000',
     '55555555-5555-5555-5555-555555555555',
     'ESS-2026-0002', 'Chen Master Bath & Shower',
     'Michael Chen', 'mchen@email.example.com', '+1 (404) 555-8833',
     'quoted', NULL, NULL, NULL,
     4200.00,
     'Floating vanity top + shower niche + bench. Client wants White Macauba with sharp eased edge.',
     'aa000005-0000-0000-0000-000000000000',
     NOW() - INTERVAL '1 week', NOW()),

    -- fab3 (Mountain View Countertops)
    ('jb000005-0000-0000-0000-000000000000',
     '66666666-6666-6666-6666-666666666666',
     'MVC-2026-0001', 'Patel Kitchen Countertop',
     'Ravi Patel', 'ravi.patel@email.example.com', '+1 (770) 555-9922',
     'scheduled', '2026-07-02', '2026-07-10', '2026-07-15',
     2800.00,
     'Standard kitchen with Statuario island top and Carrara White perimeter. First project with MM material.',
     'aa000006-0000-0000-0000-000000000000',
     NOW() - INTERVAL '2 weeks', NOW());

-- ══════════════════════════════════════════════════════════════
-- 11. PURCHASE ORDERS — cover all status tabs for sup1,
--     plus POs for sup2 and sup3
-- ══════════════════════════════════════════════════════════════

-- PO-2026-000003: fab1→sup1, ACKNOWLEDGED
--   2 Statuario slabs (ee070003, ee070004) → 65 sqft each × $135
INSERT INTO purchase_orders (
    id, po_number, fabricator_id, supplier_id, job_id,
    status, status_changed,
    subtotal, total_amount, currency,
    delivery_address_id, requested_delivery, confirmed_delivery,
    fabricator_notes, supplier_notes,
    sent_at, acked_at, created_by, created_at, updated_at
) VALUES (
    'po000003-0000-0000-0000-000000000000',
    'PO-2026-000003',
    '44444444-4444-4444-4444-444444444444',
    '11111111-1111-1111-1111-111111111111',
    'jb000001-0000-0000-0000-000000000000',
    'acknowledged', NOW() - INTERVAL '10 days',
    17550.00, 17550.00, 'USD',
    'ad000001-0000-0000-0000-000000000000', '2026-06-22', '2026-06-23',
    'Two Statuario slabs for the Johnson renovation waterfall island.',
    'Acknowledged. Slabs are allocated and ready. Will ship on confirmed date.',
    NOW() - INTERVAL '12 days', NOW() - INTERVAL '10 days',
    'aa000004-0000-0000-0000-000000000000',
    NOW() - INTERVAL '12 days', NOW()
);

-- PO-2026-000004: fab2→sup1, SHIPPED
--   Nero Marquina NM3P-003 and NM3P-004 → ~50 sqft each × $72
INSERT INTO purchase_orders (
    id, po_number, fabricator_id, supplier_id, job_id,
    status, status_changed,
    subtotal, total_amount, currency,
    delivery_address_id,
    tracking_number, carrier,
    fabricator_notes, supplier_notes,
    sent_at, acked_at, shipped_at, created_by, created_at, updated_at
) VALUES (
    'po000004-0000-0000-0000-000000000000',
    'PO-2026-000004',
    '55555555-5555-5555-5555-555555555555',
    '11111111-1111-1111-1111-111111111111',
    'jb000003-0000-0000-0000-000000000000',
    'shipped', NOW() - INTERVAL '14 days',
    7200.00, 7200.00, 'USD',
    'ad000003-0000-0000-0000-000000000000',
    'ATL-2026-77814', 'XPO Logistics',
    'Nero Marquina for Williams Kitchen island — both slabs from same lot please.',
    'Confirmed lot match. Both slabs from LOT-2025-0770, shipped together on one pallet.',
    NOW() - INTERVAL '20 days', NOW() - INTERVAL '18 days', NOW() - INTERVAL '14 days',
    'aa000005-0000-0000-0000-000000000000',
    NOW() - INTERVAL '20 days', NOW()
);

-- PO-2026-000005: fab1→sup1, RECEIVED
--   Calacatta Gold CAL3P-002 → 55 sqft × $85
INSERT INTO purchase_orders (
    id, po_number, fabricator_id, supplier_id, job_id,
    status, status_changed,
    subtotal, total_amount, currency,
    delivery_address_id,
    fabricator_notes,
    sent_at, acked_at, shipped_at, received_at, created_by, created_at, updated_at
) VALUES (
    'po000005-0000-0000-0000-000000000000',
    'PO-2026-000005',
    '44444444-4444-4444-4444-444444444444',
    '11111111-1111-1111-1111-111111111111',
    'jb000002-0000-0000-0000-000000000000',
    'received', NOW() - INTERVAL '30 days',
    4675.00, 4675.00, 'USD',
    'ad000001-0000-0000-0000-000000000000',
    'Calacatta Gold for Johnson Bath vanity top.',
    NOW() - INTERVAL '45 days', NOW() - INTERVAL '43 days', NOW() - INTERVAL '38 days', NOW() - INTERVAL '30 days',
    'aa000004-0000-0000-0000-000000000000',
    NOW() - INTERVAL '45 days', NOW()
);

-- PO-2026-000006: fab1→sup1, CANCELLED
INSERT INTO purchase_orders (
    id, po_number, fabricator_id, supplier_id,
    status, status_changed,
    subtotal, total_amount, currency,
    delivery_address_id,
    fabricator_notes,
    sent_at, created_by, created_at, updated_at
) VALUES (
    'po000006-0000-0000-0000-000000000000',
    'PO-2026-000006',
    '44444444-4444-4444-4444-444444444444',
    '11111111-1111-1111-1111-111111111111',
    'cancelled', NOW() - INTERVAL '6 weeks',
    2850.00, 2850.00, 'USD',
    'ad000002-0000-0000-0000-000000000000',
    'Carrara White order for Lakeview job. Client changed to engineered stone — cancelling.',
    NOW() - INTERVAL '7 weeks',
    'aa000004-0000-0000-0000-000000000000',
    NOW() - INTERVAL '7 weeks', NOW()
);

-- PO-2026-000007: fab3(Mountain View)→sup1, SENT — first PO from the new fab
INSERT INTO purchase_orders (
    id, po_number, fabricator_id, supplier_id, job_id,
    status, status_changed,
    subtotal, total_amount, currency,
    delivery_address_id, requested_delivery,
    fabricator_notes,
    sent_at, created_by, created_at, updated_at
) VALUES (
    'po000007-0000-0000-0000-000000000000',
    'PO-2026-000007',
    '66666666-6666-6666-6666-666666666666',
    '11111111-1111-1111-1111-111111111111',
    'jb000005-0000-0000-0000-000000000000',
    'sent', NOW() - INTERVAL '2 days',
    9855.00, 9855.00, 'USD',
    'ad000004-0000-0000-0000-000000000000', '2026-07-08',
    'First order with Marble Masters. Statuario island + Carrara White perimeter for Patel project.',
    NOW() - INTERVAL '2 days',
    'aa000006-0000-0000-0000-000000000000',
    NOW() - INTERVAL '2 days', NOW()
);

-- PO-2026-000008: fab1→sup2 (Stone Source), SENT
INSERT INTO purchase_orders (
    id, po_number, fabricator_id, supplier_id, job_id,
    status, status_changed,
    subtotal, total_amount, currency,
    delivery_address_id, requested_delivery,
    fabricator_notes,
    sent_at, created_by, created_at, updated_at
) VALUES (
    'po000008-0000-0000-0000-000000000000',
    'PO-2026-000008',
    '44444444-4444-4444-4444-444444444444',
    '22222222-2222-2222-2222-222222222222',
    NULL,
    'sent', NOW() - INTERVAL '1 week',
    14500.00, 14500.00, 'USD',
    'ad000001-0000-0000-0000-000000000000', '2026-07-01',
    'Blue Bahia for a designer spec project. Lot AA-3X preferred — already saw it in the yard.',
    NOW() - INTERVAL '1 week',
    'aa000004-0000-0000-0000-000000000000',
    NOW() - INTERVAL '1 week', NOW()
);

-- PO-2026-000009: fab2→sup3 (Premier Granite), RECEIVED
INSERT INTO purchase_orders (
    id, po_number, fabricator_id, supplier_id, job_id,
    status, status_changed,
    subtotal, total_amount, currency,
    delivery_address_id,
    fabricator_notes,
    sent_at, acked_at, shipped_at, received_at, created_by, created_at, updated_at
) VALUES (
    'po000009-0000-0000-0000-000000000000',
    'PO-2026-000009',
    '55555555-5555-5555-5555-555555555555',
    '33333333-3333-3333-3333-333333333333',
    NULL,
    'received', NOW() - INTERVAL '20 days',
    2400.00, 2400.00, 'USD',
    'ad000003-0000-0000-0000-000000000000',
    'Black Galaxy for a basement bar countertop. 3 slabs from same lot please.',
    NOW() - INTERVAL '5 weeks', NOW() - INTERVAL '4 weeks' + INTERVAL '2 days', NOW() - INTERVAL '3 weeks', NOW() - INTERVAL '20 days',
    'aa000005-0000-0000-0000-000000000000',
    NOW() - INTERVAL '5 weeks', NOW()
);

-- ── PO status history ─────────────────────────────────────────
INSERT INTO po_status_history (id, po_id, from_status, to_status, changed_by, note, changed_at) VALUES
    ('ph000002-0000-0000-0000-000000000000', 'po000003-0000-0000-0000-000000000000', 'draft', 'sent',         'aa000004-0000-0000-0000-000000000000', NULL,                          NOW() - INTERVAL '12 days'),
    ('ph000003-0000-0000-0000-000000000000', 'po000003-0000-0000-0000-000000000000', 'sent',  'acknowledged', 'aa000001-0000-0000-0000-000000000000', 'Slabs allocated and ready.',  NOW() - INTERVAL '10 days'),
    ('ph000004-0000-0000-0000-000000000000', 'po000004-0000-0000-0000-000000000000', 'draft', 'sent',         'aa000005-0000-0000-0000-000000000000', NULL,                          NOW() - INTERVAL '20 days'),
    ('ph000005-0000-0000-0000-000000000000', 'po000004-0000-0000-0000-000000000000', 'sent',  'acknowledged', 'aa000001-0000-0000-0000-000000000000', 'Confirmed lot match.',        NOW() - INTERVAL '18 days'),
    ('ph000006-0000-0000-0000-000000000000', 'po000004-0000-0000-0000-000000000000', 'acknowledged', 'shipped', 'aa000001-0000-0000-0000-000000000000', 'Shipped on pallet via XPO.', NOW() - INTERVAL '14 days'),
    ('ph000007-0000-0000-0000-000000000000', 'po000005-0000-0000-0000-000000000000', 'draft', 'sent',         'aa000004-0000-0000-0000-000000000000', NULL,                          NOW() - INTERVAL '45 days'),
    ('ph000008-0000-0000-0000-000000000000', 'po000005-0000-0000-0000-000000000000', 'sent',  'acknowledged', 'aa000001-0000-0000-0000-000000000000', NULL,                          NOW() - INTERVAL '43 days'),
    ('ph000009-0000-0000-0000-000000000000', 'po000005-0000-0000-0000-000000000000', 'acknowledged', 'shipped', 'aa000001-0000-0000-0000-000000000000', NULL,                        NOW() - INTERVAL '38 days'),
    ('ph000010-0000-0000-0000-000000000000', 'po000005-0000-0000-0000-000000000000', 'shipped', 'received',   'aa000004-0000-0000-0000-000000000000', 'Received in good condition.', NOW() - INTERVAL '30 days'),
    ('ph000011-0000-0000-0000-000000000000', 'po000006-0000-0000-0000-000000000000', 'draft', 'sent',         'aa000004-0000-0000-0000-000000000000', NULL,                          NOW() - INTERVAL '7 weeks'),
    ('ph000012-0000-0000-0000-000000000000', 'po000006-0000-0000-0000-000000000000', 'sent',  'cancelled',    'aa000004-0000-0000-0000-000000000000', 'Client switched to quartz.',  NOW() - INTERVAL '6 weeks'),
    ('ph000013-0000-0000-0000-000000000000', 'po000007-0000-0000-0000-000000000000', 'draft', 'sent',         'aa000006-0000-0000-0000-000000000000', NULL,                          NOW() - INTERVAL '2 days'),
    ('ph000014-0000-0000-0000-000000000000', 'po000008-0000-0000-0000-000000000000', 'draft', 'sent',         'aa000004-0000-0000-0000-000000000000', NULL,                          NOW() - INTERVAL '1 week'),
    ('ph000015-0000-0000-0000-000000000000', 'po000009-0000-0000-0000-000000000000', 'draft', 'sent',         'aa000005-0000-0000-0000-000000000000', NULL,                          NOW() - INTERVAL '5 weeks'),
    ('ph000016-0000-0000-0000-000000000000', 'po000009-0000-0000-0000-000000000000', 'sent',  'acknowledged', 'aa000003-0000-0000-0000-000000000000', NULL,                          NOW() - INTERVAL '4 weeks' + INTERVAL '2 days'),
    ('ph000017-0000-0000-0000-000000000000', 'po000009-0000-0000-0000-000000000000', 'acknowledged', 'shipped', 'aa000003-0000-0000-0000-000000000000', NULL,                        NOW() - INTERVAL '3 weeks'),
    ('ph000018-0000-0000-0000-000000000000', 'po000009-0000-0000-0000-000000000000', 'shipped', 'received',   'aa000005-0000-0000-0000-000000000000', 'All slabs received, no damage.', NOW() - INTERVAL '20 days');

-- ── PO Line items ─────────────────────────────────────────────

-- po000003 lines: 2 Statuario slabs
INSERT INTO po_line_items (id, po_id, variant_id, slab_id, item_snapshot, quantity, unit_of_measure, unit_price, line_total, currency, status, created_at, updated_at) VALUES
(
    'li000003-0000-0000-0000-000000000000',
    'po000003-0000-0000-0000-000000000000',
    'dd000009-0000-0000-0000-000000000000',
    'ee070003-0000-0000-0000-000000000000',
    '{"material_name":"Statuario","variant_name":"Statuario 3cm Polished","sku":"MM-ST-3CM-POL","internal_ref":"ST3P-003","gross_length_mm":3400,"gross_width_mm":1720,"thickness_cm":3,"finish":"polished","quality_grade":"A","warehouse":"Norcross Main Yard","rack_location":"F-02-L"}'::jsonb,
    64.80, 'sqft', 135.00, 8748.00, 'USD', 'pending', NOW() - INTERVAL '12 days', NOW()
),
(
    'li000004-0000-0000-0000-000000000000',
    'po000003-0000-0000-0000-000000000000',
    'dd000009-0000-0000-0000-000000000000',
    'ee070004-0000-0000-0000-000000000000',
    '{"material_name":"Statuario","variant_name":"Statuario 3cm Polished","sku":"MM-ST-3CM-POL","internal_ref":"ST3P-004","gross_length_mm":3100,"gross_width_mm":1600,"thickness_cm":3,"finish":"polished","quality_grade":"B","warehouse":"Norcross Main Yard","rack_location":"F-02-R"}'::jsonb,
    65.50, 'sqft', 135.00, 8842.50, 'USD', 'pending', NOW() - INTERVAL '12 days', NOW()
);

-- po000004 lines: 2 Nero Marquina slabs (shipped)
INSERT INTO po_line_items (id, po_id, variant_id, slab_id, item_snapshot, quantity, unit_of_measure, unit_price, line_total, currency, status, created_at, updated_at) VALUES
(
    'li000005-0000-0000-0000-000000000000',
    'po000004-0000-0000-0000-000000000000',
    'dd000010-0000-0000-0000-000000000000',
    'ee080003-0000-0000-0000-000000000000',
    '{"material_name":"Nero Marquina","variant_name":"Nero Marquina 3cm Polished","sku":"MM-NM-3CM-POL","internal_ref":"NM3P-003","gross_length_mm":3100,"gross_width_mm":1560,"thickness_cm":3,"finish":"polished","quality_grade":"A","warehouse":"Norcross Main Yard","rack_location":"G-03-L"}'::jsonb,
    53.65, 'sqft', 72.00, 3862.80, 'USD', 'pending', NOW() - INTERVAL '20 days', NOW()
),
(
    'li000006-0000-0000-0000-000000000000',
    'po000004-0000-0000-0000-000000000000',
    'dd000010-0000-0000-0000-000000000000',
    'ee080004-0000-0000-0000-000000000000',
    '{"material_name":"Nero Marquina","variant_name":"Nero Marquina 3cm Polished","sku":"MM-NM-3CM-POL","internal_ref":"NM3P-004","gross_length_mm":3200,"gross_width_mm":1600,"thickness_cm":3,"finish":"polished","quality_grade":"A","warehouse":"Norcross Main Yard","rack_location":"G-03-R"}'::jsonb,
    56.89, 'sqft', 72.00, 4096.08, 'USD', 'pending', NOW() - INTERVAL '20 days', NOW()
);

-- po000005 lines: 1 Calacatta Gold slab (received)
INSERT INTO po_line_items (id, po_id, variant_id, slab_id, item_snapshot, quantity, unit_of_measure, unit_price, line_total, currency, status, created_at, updated_at) VALUES
(
    'li000007-0000-0000-0000-000000000000',
    'po000005-0000-0000-0000-000000000000',
    'dd000003-0000-0000-0000-000000000000',
    'ee010002-0000-0000-0000-000000000000',
    '{"material_name":"Calacatta Gold","variant_name":"Calacatta Gold 3cm Polished","sku":"MM-CAL-3CM-POL","internal_ref":"CAL3P-002","gross_length_mm":3100,"gross_width_mm":1580,"thickness_cm":3,"finish":"polished","quality_grade":"A","warehouse":"Norcross Main Yard","rack_location":"B-01-R"}'::jsonb,
    55.00, 'sqft', 85.00, 4675.00, 'USD', 'pending', NOW() - INTERVAL '45 days', NOW()
);

-- po000006 lines: Carrara White (cancelled)
INSERT INTO po_line_items (id, po_id, variant_id, slab_id, item_snapshot, quantity, unit_of_measure, unit_price, line_total, currency, status, created_at, updated_at) VALUES
(
    'li000008-0000-0000-0000-000000000000',
    'po000006-0000-0000-0000-000000000000',
    'dd000001-0000-0000-0000-000000000000',
    NULL,
    '{"material_name":"Carrara White","variant_name":"Carrara White 3cm Polished","sku":"MM-CW-3CM-POL","internal_ref":null}'::jsonb,
    100.00, 'sqft', 28.50, 2850.00, 'USD', 'cancelled', NOW() - INTERVAL '7 weeks', NOW()
);

-- po000007 lines: Mountain View first order (Statuario + Carrara White)
INSERT INTO po_line_items (id, po_id, variant_id, slab_id, item_snapshot, quantity, unit_of_measure, unit_price, line_total, currency, status, created_at, updated_at) VALUES
(
    'li000009-0000-0000-0000-000000000000',
    'po000007-0000-0000-0000-000000000000',
    'dd000009-0000-0000-0000-000000000000',
    NULL,
    '{"material_name":"Statuario","variant_name":"Statuario 3cm Polished","sku":"MM-ST-3CM-POL"}'::jsonb,
    58.00, 'sqft', 135.00, 7830.00, 'USD', 'pending', NOW() - INTERVAL '2 days', NOW()
),
(
    'li000010-0000-0000-0000-000000000000',
    'po000007-0000-0000-0000-000000000000',
    'dd000001-0000-0000-0000-000000000000',
    NULL,
    '{"material_name":"Carrara White","variant_name":"Carrara White 3cm Polished","sku":"MM-CW-3CM-POL"}'::jsonb,
    70.00, 'sqft', 28.50, 1995.00, 'USD', 'pending', NOW() - INTERVAL '2 days', NOW()
);

-- po000008 lines: Blue Bahia for Stone Source
INSERT INTO po_line_items (id, po_id, variant_id, slab_id, item_snapshot, quantity, unit_of_measure, unit_price, line_total, currency, status, created_at, updated_at) VALUES
(
    'li000011-0000-0000-0000-000000000000',
    'po000008-0000-0000-0000-000000000000',
    'dd000005-0000-0000-0000-000000000000',
    'ee030001-0000-0000-0000-000000000000',
    '{"material_name":"Blue Bahia","variant_name":"Blue Bahia 3cm Polished","sku":"SS-BB-3CM-POL","internal_ref":"BB3P-001","gross_length_mm":3000,"gross_width_mm":1600}'::jsonb,
    100.00, 'sqft', 145.00, 14500.00, 'USD', 'pending', NOW() - INTERVAL '1 week', NOW()
);

-- po000009 lines: Black Galaxy for Premier Granite (received)
INSERT INTO po_line_items (id, po_id, variant_id, slab_id, item_snapshot, quantity, unit_of_measure, unit_price, line_total, currency, status, created_at, updated_at) VALUES
(
    'li000012-0000-0000-0000-000000000000',
    'po000009-0000-0000-0000-000000000000',
    'dd000013-0000-0000-0000-000000000000',
    'ee0c0004-0000-0000-0000-000000000000',
    '{"material_name":"Black Galaxy","variant_name":"Black Galaxy 3cm Polished","sku":"PG-BG-3CM-POL","internal_ref":"BG3P-004","gross_length_mm":3000,"gross_width_mm":1580}'::jsonb,
    53.00, 'sqft', 24.00, 1272.00, 'USD', 'pending', NOW() - INTERVAL '5 weeks', NOW()
),
(
    'li000013-0000-0000-0000-000000000000',
    'po000009-0000-0000-0000-000000000000',
    'dd000013-0000-0000-0000-000000000000',
    'ee0c0005-0000-0000-0000-000000000000',
    '{"material_name":"Black Galaxy","variant_name":"Black Galaxy 3cm Polished","sku":"PG-BG-3CM-POL","internal_ref":"BG3P-005","gross_length_mm":3100,"gross_width_mm":1600}'::jsonb,
    55.00, 'sqft', 24.00, 1320.00, 'USD', 'pending', NOW() - INTERVAL '5 weeks', NOW()
),
(
    'li000014-0000-0000-0000-000000000000',
    'po000009-0000-0000-0000-000000000000',
    'dd000013-0000-0000-0000-000000000000',
    NULL,
    '{"material_name":"Black Galaxy","variant_name":"Black Galaxy 3cm Polished","sku":"PG-BG-3CM-POL"}'::jsonb,
    35.25, 'sqft', 24.00, 846.00, 'USD', 'pending', NOW() - INTERVAL '5 weeks', NOW()
);

-- ══════════════════════════════════════════════════════════════
-- 12. SLAB STATUS UPDATES — sync with PO state
-- ══════════════════════════════════════════════════════════════

-- po000003 (acknowledged) → slabs become allocated
UPDATE slabs
SET    status         = 'allocated',
       reserved_for_po = 'po000003-0000-0000-0000-000000000000',
       status_changed  = NOW() - INTERVAL '10 days',
       updated_at      = NOW() - INTERVAL '10 days'
WHERE  id IN (
    'ee070003-0000-0000-0000-000000000000',
    'ee070004-0000-0000-0000-000000000000'
);

-- po000004 (shipped) → slabs are shipped (already set to 'shipped' in INSERT above)
UPDATE slabs
SET    reserved_for_po = 'po000004-0000-0000-0000-000000000000',
       status_changed   = NOW() - INTERVAL '14 days',
       updated_at       = NOW() - INTERVAL '14 days'
WHERE  id IN (
    'ee080003-0000-0000-0000-000000000000',
    'ee080004-0000-0000-0000-000000000000'
);

-- po000005 (received) → Calacatta Gold slab sold
UPDATE slabs
SET    status          = 'sold',
       is_active       = FALSE,
       reserved_for_po = 'po000005-0000-0000-0000-000000000000',
       status_changed  = NOW() - INTERVAL '30 days',
       updated_at      = NOW() - INTERVAL '30 days'
WHERE  id = 'ee010002-0000-0000-0000-000000000000';

-- po000009 (received) → Black Galaxy slabs for Premier Granite sold
UPDATE slabs
SET    status          = 'sold',
       is_active       = FALSE,
       reserved_for_po = 'po000009-0000-0000-0000-000000000000',
       status_changed  = NOW() - INTERVAL '20 days',
       updated_at      = NOW() - INTERVAL '20 days'
WHERE  id IN (
    'ee0c0004-0000-0000-0000-000000000000',
    'ee0c0005-0000-0000-0000-000000000000'
);

RESET row_security;
