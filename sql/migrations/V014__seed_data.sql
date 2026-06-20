-- ============================================================
-- V014: Seed data â€” development & testing
--
-- Creates a realistic multi-tenant dataset:
--   3 suppliers Â· 2 fabricators Â· 7 products Â· 8 variants
--   20 slabs Â· 4 connections Â· 2 jobs Â· 2 purchase orders
--
-- UUID key (all hex-valid):
--   Tenants : 11111111-â€¦ sup1  22222222-â€¦ sup2  33333333-â€¦ sup3
--             44444444-â€¦ fab1  55555555-â€¦ fab2
--   Users   : aa000001-â€¦ â†’ aa000005-â€¦
--   Warehouses : bb000001-â€¦ â†’ bb000005-â€¦
--   Products   : cc000001-â€¦ â†’ cc000007-â€¦
--   Variants   : dd000001-â€¦ â†’ dd000008-â€¦
--   Slabs sup1 : ee000001-â€¦ CW  ee010001-â€¦ CAL  ee020001-â€¦ AB
--   Slabs sup2 : ee030001-â€¦ BB  ee040001-â€¦ NW
--   Slabs sup3 : ee050001-â€¦ KW  ee060001-â€¦ UG
--   Connections : ff000001-â€¦ â†’ ff000004-â€¦
--   Addresses   : ad000001-â€¦ â†’ ad000003-â€¦
--   Jobs        : jb000001-â€¦ â†’ jb000002-â€¦
--   POs         : po000001-â€¦ â†’ po000002-â€¦
--   Line items  : li000001-â€¦ â†’ li000002-â€¦
--
-- Must run as PostgreSQL superuser (RLS FORCE on tenants / profiles).
-- ============================================================

SET row_security = OFF;

-- â”€â”€ Tenants â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO tenants (id, type, name, slug, plan, country, is_active, created_at, updated_at) VALUES
    ('11111111-1111-1111-1111-111111111111', 'supplier',   'Marble Masters Inc',      'marble-masters',    'pro',        'US', TRUE, NOW() - INTERVAL '6 months', NOW()),
    ('22222222-2222-2222-2222-222222222222', 'supplier',   'Stone Source LLC',         'stone-source',      'starter',    'US', TRUE, NOW() - INTERVAL '4 months', NOW()),
    ('33333333-3333-3333-3333-333333333333', 'supplier',   'Premier Granite Co',       'premier-granite',   'enterprise', 'US', TRUE, NOW() - INTERVAL '8 months', NOW()),
    ('44444444-4444-4444-4444-444444444444', 'fabricator', 'Countertop Kings',         'countertop-kings',  'pro',        'US', TRUE, NOW() - INTERVAL '5 months', NOW()),
    ('55555555-5555-5555-5555-555555555555', 'fabricator', 'Elite Surfaces & Stone',   'elite-surfaces',    'starter',    'US', TRUE, NOW() - INTERVAL '3 months', NOW());

-- â”€â”€ Users â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO users (id, tenant_id, clerk_user_id, email, full_name, role, is_active, created_at, updated_at) VALUES
    ('aa000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'user_seed_sup1_owner', 'owner@marblemasters.dev',  'Marco Rossi',     'owner', TRUE, NOW() - INTERVAL '6 months', NOW()),
    ('aa000002-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'user_seed_sup2_owner', 'owner@stonesource.dev',    'Sarah Mitchell',  'owner', TRUE, NOW() - INTERVAL '4 months', NOW()),
    ('aa000003-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'user_seed_sup3_owner', 'owner@premiergranite.dev', 'David Chen',      'owner', TRUE, NOW() - INTERVAL '8 months', NOW()),
    ('aa000004-0000-0000-0000-000000000000', '44444444-4444-4444-4444-444444444444', 'user_seed_fab1_owner', 'owner@countertopkings.dev','James Hawkins',   'owner', TRUE, NOW() - INTERVAL '5 months', NOW()),
    ('aa000005-0000-0000-0000-000000000000', '55555555-5555-5555-5555-555555555555', 'user_seed_fab2_owner', 'owner@elitesurfaces.dev',  'Angela Torres',   'owner', TRUE, NOW() - INTERVAL '3 months', NOW());

-- â”€â”€ Supplier profiles â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO supplier_profiles (
    tenant_id, display_name, description, website, phone,
    address_line1, address_line2, city, state_province, postal_code, country,
    established_year, verified, verified_at,
    avg_lead_days, fulfillment_rate, avg_response_hrs,
    total_slabs_sold, warehouse_count,
    notification_prefs, created_at, updated_at
) VALUES
(
    '11111111-1111-1111-1111-111111111111',
    'Marble Masters Inc',
    'Family-owned importer of premium Italian and Brazilian marble with 20+ years supplying the countertop industry. We carry exclusive lots of Calacatta, Carrara, and exotic quartzites direct from the quarry.',
    'https://marblemasters.example.com', '+1 (404) 555-0101',
    '1400 Peachtree Industrial Blvd', 'Suite 200', 'Norcross', 'GA', '30071', 'US',
    2004, TRUE, NOW() - INTERVAL '2 months',
    4.2, 96.5, 2.8, 1240, 2,
    '{"new_po":{"inApp":true,"email":true,"sms":false},"po_unacked_24h":{"inApp":true,"email":true,"sms":true},"connection_requested":{"inApp":true,"email":true,"sms":false},"connection_approved":{"inApp":true,"email":false,"sms":false},"price_changed":{"inApp":false,"email":true,"sms":false},"low_stock_warning":{"inApp":true,"email":true,"sms":false}}'::jsonb,
    NOW() - INTERVAL '6 months', NOW()
),
(
    '22222222-2222-2222-2222-222222222222',
    'Stone Source LLC',
    'Specialty importer of quartzite and exotic stone sourced directly from Brazil, India, and Norway. We focus on rare, high-movement material that designers love.',
    'https://stonesource.example.com', '+1 (972) 555-0202',
    '8700 Research Drive', NULL, 'Frisco', 'TX', '75034', 'US',
    2018, FALSE, NULL,
    6.1, 91.2, 4.5, 380, 1,
    '{"new_po":{"inApp":true,"email":true,"sms":false},"po_unacked_24h":{"inApp":true,"email":true,"sms":false},"connection_requested":{"inApp":true,"email":true,"sms":false},"connection_approved":{"inApp":true,"email":false,"sms":false},"price_changed":{"inApp":false,"email":true,"sms":false},"low_stock_warning":{"inApp":true,"email":true,"sms":false}}'::jsonb,
    NOW() - INTERVAL '4 months', NOW()
),
(
    '33333333-3333-3333-3333-333333333333',
    'Premier Granite Co',
    'The Southeast''s largest granite and engineered stone distributor. 30+ years serving fabricators with reliable stock, competitive pricing, and same-day will-call from our 50,000 sq ft Atlanta warehouse.',
    'https://premiergranite.example.com', '+1 (770) 555-0303',
    '3250 Buford Highway NE', 'Building A', 'Atlanta', 'GA', '30329', 'US',
    1993, TRUE, NOW() - INTERVAL '6 months',
    3.5, 98.1, 1.9, 4800, 2,
    '{"new_po":{"inApp":true,"email":true,"sms":true},"po_unacked_24h":{"inApp":true,"email":true,"sms":true},"connection_requested":{"inApp":true,"email":true,"sms":false},"connection_approved":{"inApp":true,"email":true,"sms":false},"price_changed":{"inApp":true,"email":true,"sms":false},"low_stock_warning":{"inApp":true,"email":true,"sms":true}}'::jsonb,
    NOW() - INTERVAL '8 months', NOW()
);

-- â”€â”€ Fabricator profiles â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO fabricator_profiles (
    tenant_id, display_name, description, website, phone,
    address_line1, city, state_province, postal_code, country,
    shop_size, monthly_job_volume,
    notification_prefs, created_at, updated_at
) VALUES
(
    '44444444-4444-4444-4444-444444444444',
    'Countertop Kings',
    'Mid-size fabrication shop serving residential and light commercial clients across metro Atlanta. CNC-equipped with a 10,000 sq ft shop floor.',
    'https://countertopkings.example.com', '+1 (678) 555-0401',
    '4455 Industrial Way', 'Duluth', 'GA', '30096', 'US',
    'medium', 45,
    '{"new_po":{"inApp":true,"email":true,"sms":false},"po_unacked_24h":{"inApp":true,"email":false,"sms":false},"connection_requested":{"inApp":true,"email":false,"sms":false},"connection_approved":{"inApp":true,"email":true,"sms":false},"price_changed":{"inApp":true,"email":true,"sms":false},"low_stock_warning":{"inApp":false,"email":false,"sms":false}}'::jsonb,
    NOW() - INTERVAL '5 months', NOW()
),
(
    '55555555-5555-5555-5555-555555555555',
    'Elite Surfaces & Stone',
    'Boutique shop specializing in high-end residential installs. Known for precision and white-glove service.',
    'https://elitesurfaces.example.com', '+1 (404) 555-0502',
    '720 Ponce De Leon Ave NE', 'Atlanta', 'GA', '30306', 'US',
    'small', 18,
    '{"new_po":{"inApp":true,"email":true,"sms":false},"po_unacked_24h":{"inApp":true,"email":true,"sms":false},"connection_requested":{"inApp":true,"email":true,"sms":false},"connection_approved":{"inApp":true,"email":true,"sms":false},"price_changed":{"inApp":true,"email":false,"sms":false},"low_stock_warning":{"inApp":false,"email":false,"sms":false}}'::jsonb,
    NOW() - INTERVAL '3 months', NOW()
);

-- â”€â”€ Warehouses â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO warehouses (id, tenant_id, name, address_line1, city, state_province, postal_code, country, phone, is_primary, is_active, created_at, updated_at) VALUES
    ('bb000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'Norcross Main Yard',    '1400 Peachtree Industrial Blvd', 'Norcross', 'GA', '30071', 'US', '+1 (404) 555-0101', TRUE,  TRUE, NOW() - INTERVAL '6 months', NOW()),
    ('bb000002-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'Smyrna Overflow Yard',  '600 Cobb Pkwy N',               'Smyrna',   'GA', '30080', 'US', '+1 (404) 555-0102', FALSE, TRUE, NOW() - INTERVAL '3 months', NOW()),
    ('bb000003-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'Frisco Distribution',   '8700 Research Drive',           'Frisco',   'TX', '75034', 'US', '+1 (972) 555-0202', TRUE,  TRUE, NOW() - INTERVAL '4 months', NOW()),
    ('bb000004-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'Atlanta Showroom Yard', '3250 Buford Highway NE',        'Atlanta',  'GA', '30329', 'US', '+1 (770) 555-0303', TRUE,  TRUE, NOW() - INTERVAL '8 months', NOW()),
    ('bb000005-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'Marietta Secondary',    '2900 Canton Rd',                'Marietta', 'GA', '30066', 'US', '+1 (770) 555-0304', FALSE, TRUE, NOW() - INTERVAL '6 months', NOW());

-- â”€â”€ Products â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
-- Marble Masters (sup1)
INSERT INTO products (id, tenant_id, category_code, name, brand, short_description, is_active, created_by, created_at, updated_at) VALUES
    ('cc000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'slab',
     'Carrara White Marble',   'Marble Masters',
     'Classic Italian white marble with soft grey veining. Timeless choice for kitchen countertops and bathroom vanities.',
     TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '5 months', NOW()),

    ('cc000002-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'slab',
     'Calacatta Gold Marble',  'Marble Masters',
     'Premium Italian marble with bold gold and grey veining on a bright white background. Statement material for luxury projects.',
     TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '5 months', NOW()),

    ('cc000003-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'slab',
     'Absolute Black Granite', 'Marble Masters',
     'Jet-black South African granite. Zero movement, ultra-uniform appearance. Popular for modern and contemporary kitchens.',
     TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '5 months', NOW());

-- Stone Source (sup2)
INSERT INTO products (id, tenant_id, category_code, name, brand, short_description, is_active, created_by, created_at, updated_at) VALUES
    ('cc000004-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'slab',
     'Blue Bahia Quartzite',   'Stone Source',
     'Vivid cobalt and indigo quartzite from Bahia, Brazil. Dramatic movement and natural crystal clusters. Statement piece material.',
     TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '3 months', NOW()),

    ('cc000005-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'slab',
     'Nordic White Quartzite', 'Stone Source',
     'Clean white quartzite with minimal grey veining from Norway. Excellent hardness, ideal for high-traffic kitchens.',
     TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '3 months', NOW());

-- Premier Granite (sup3)
INSERT INTO products (id, tenant_id, category_code, name, brand, short_description, is_active, created_by, created_at, updated_at) VALUES
    ('cc000006-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'slab',
     'Kashmir White Granite',  'Premier Granite',
     'Soft white granite with burgundy and black flecks from India. Reliable stock and excellent value.',
     TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '7 months', NOW()),

    ('cc000007-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'slab',
     'Ubatuba Green Granite',  'Premier Granite',
     'Deep forest-green Brazilian granite with golden flecks. A classic choice that never goes out of style.',
     TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '7 months', NOW());

-- â”€â”€ Product variants (all slab variants â€” qty_available must be NULL) â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO product_variants (
    id, product_id, tenant_id, sku, variant_name, attributes,
    unit_of_measure, base_price, currency, qty_available,
    is_slab_variant, status, lead_time_days, created_at, updated_at
) VALUES
    -- Marble Masters
    ('dd000001-0000-0000-0000-000000000000', 'cc000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'MM-CW-3CM-POL',  'Carrara White 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"white","pattern":"veined"}'::jsonb,
     'sqft', 28.50, 'USD', NULL, TRUE, 'active', 3, NOW() - INTERVAL '5 months', NOW()),

    ('dd000002-0000-0000-0000-000000000000', 'cc000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'MM-CW-2CM-POL',  'Carrara White 2cm Polished',
     '{"thickness_cm":2,"finish":"polished","color_family":"white","pattern":"veined"}'::jsonb,
     'sqft', 22.00, 'USD', NULL, TRUE, 'active', 3, NOW() - INTERVAL '5 months', NOW()),

    ('dd000003-0000-0000-0000-000000000000', 'cc000002-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'MM-CAL-3CM-POL', 'Calacatta Gold 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"white","pattern":"veined"}'::jsonb,
     'sqft', 85.00, 'USD', NULL, TRUE, 'active', 5, NOW() - INTERVAL '5 months', NOW()),

    ('dd000004-0000-0000-0000-000000000000', 'cc000003-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'MM-AB-3CM-POL',  'Absolute Black 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"black","pattern":"solid"}'::jsonb,
     'sqft', 32.00, 'USD', NULL, TRUE, 'active', 2, NOW() - INTERVAL '5 months', NOW()),

    -- Stone Source
    ('dd000005-0000-0000-0000-000000000000', 'cc000004-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'SS-BB-3CM-POL',  'Blue Bahia 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"blue","pattern":"exotic"}'::jsonb,
     'sqft', 145.00, 'USD', NULL, TRUE, 'active', 7, NOW() - INTERVAL '3 months', NOW()),

    ('dd000006-0000-0000-0000-000000000000', 'cc000005-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'SS-NW-3CM-HON',  'Nordic White 3cm Honed',
     '{"thickness_cm":3,"finish":"honed","color_family":"white","pattern":"veined"}'::jsonb,
     'sqft', 68.00, 'USD', NULL, TRUE, 'active', 5, NOW() - INTERVAL '3 months', NOW()),

    -- Premier Granite
    ('dd000007-0000-0000-0000-000000000000', 'cc000006-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'PG-KW-3CM-POL',  'Kashmir White 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"white","pattern":"flecked"}'::jsonb,
     'sqft', 18.50, 'USD', NULL, TRUE, 'active', 2, NOW() - INTERVAL '7 months', NOW()),

    ('dd000008-0000-0000-0000-000000000000', 'cc000007-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'PG-UG-3CM-POL',  'Ubatuba Green 3cm Polished',
     '{"thickness_cm":3,"finish":"polished","color_family":"green","pattern":"flecked"}'::jsonb,
     'sqft', 16.00, 'USD', NULL, TRUE, 'active', 2, NOW() - INTERVAL '7 months', NOW());

-- â”€â”€ Slabs â€” Marble Masters (sup1) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
-- net_sqft, net_sqm, search_vector are GENERATED ALWAYS AS STORED â€” omit from INSERT

-- Carrara White 3cm Polished (variant dd000001)
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee000001-0000-0000-0000-000000000000', 'dd000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'CW3P-001', 'marble', 'Carrara White', 'white', 'veined', 'IT', 'Fantiscritti Quarry', 'LOT-2025-0142', 'BLK-88A',
     3.0, 'polished', 3200, 1650, 28.50, 'bb000001-0000-0000-0000-000000000000', 'A-01-L',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '4 months', NOW()),

    ('ee000002-0000-0000-0000-000000000000', 'dd000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'CW3P-002', 'marble', 'Carrara White', 'white', 'veined', 'IT', 'Fantiscritti Quarry', 'LOT-2025-0142', 'BLK-88A',
     3.0, 'polished', 3050, 1600, 28.50, 'bb000001-0000-0000-0000-000000000000', 'A-01-L',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '4 months', NOW()),

    ('ee000003-0000-0000-0000-000000000000', 'dd000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'CW3P-003', 'marble', 'Carrara White', 'white', 'veined', 'IT', 'Fantiscritti Quarry', 'LOT-2025-0142', 'BLK-88B',
     3.0, 'polished', 3150, 1620, 28.50, 'bb000001-0000-0000-0000-000000000000', 'A-01-R',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '4 months', NOW()),

    ('ee000004-0000-0000-0000-000000000000', 'dd000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'CW3P-004', 'marble', 'Carrara White', 'white', 'veined', 'IT', 'Fantiscritti Quarry', 'LOT-2025-0142', 'BLK-88B',
     3.0, 'polished', 3200, 1650, 28.50, 'bb000001-0000-0000-0000-000000000000', 'A-01-R',
     'B', 'hold',      TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '4 months', NOW()),

    ('ee000005-0000-0000-0000-000000000000', 'dd000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'CW3P-005', 'marble', 'Carrara White', 'white', 'veined', 'IT', 'Cava Michelangelo',   'LOT-2025-0199', 'BLK-91C',
     3.0, 'polished', 3300, 1700, 31.00, 'bb000001-0000-0000-0000-000000000000', 'A-02-L',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '2 months', NOW());

-- Calacatta Gold 3cm Polished (variant dd000003)
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee010001-0000-0000-0000-000000000000', 'dd000003-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'CAL3P-001', 'marble', 'Calacatta Gold', 'white', 'veined', 'IT', 'Henraux Quarry',      'LOT-2025-0088', 'BLK-22A',
     3.0, 'polished', 3200, 1600, 85.00, 'bb000001-0000-0000-0000-000000000000', 'B-01-L',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '3 months', NOW()),

    ('ee010002-0000-0000-0000-000000000000', 'dd000003-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'CAL3P-002', 'marble', 'Calacatta Gold', 'white', 'veined', 'IT', 'Henraux Quarry',      'LOT-2025-0088', 'BLK-22A',
     3.0, 'polished', 3100, 1580, 85.00, 'bb000001-0000-0000-0000-000000000000', 'B-01-R',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '3 months', NOW()),

    ('ee010003-0000-0000-0000-000000000000', 'dd000003-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'CAL3P-003', 'marble', 'Calacatta Gold', 'white', 'veined', 'IT', 'Fantiscritti Quarry', 'LOT-2026-0011', 'BLK-55D',
     3.0, 'polished', 3250, 1620, 90.00, 'bb000002-0000-0000-0000-000000000000', 'C-01-L',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '1 month',  NOW());

-- Absolute Black 3cm Polished (variant dd000004)
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee020001-0000-0000-0000-000000000000', 'dd000004-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'AB3P-001', 'granite', 'Absolute Black', 'black', 'solid', 'ZA', 'Northern Cape',       'LOT-2025-0301', 'BLK-7A',
     3.0, 'polished', 3000, 1500, 32.00, 'bb000001-0000-0000-0000-000000000000', 'D-01-L',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '3 months', NOW()),

    ('ee020002-0000-0000-0000-000000000000', 'dd000004-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'AB3P-002', 'granite', 'Absolute Black', 'black', 'solid', 'ZA', 'Northern Cape',       'LOT-2025-0301', 'BLK-7B',
     3.0, 'polished', 3050, 1520, 32.00, 'bb000001-0000-0000-0000-000000000000', 'D-01-R',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '3 months', NOW()),

    ('ee020003-0000-0000-0000-000000000000', 'dd000004-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111',
     'AB3P-003', 'granite', 'Absolute Black', 'black', 'solid', 'ZA', 'Mpumalanga Province', 'LOT-2026-0055', 'BLK-12A',
     3.0, 'polished', 3200, 1600, 32.00, 'bb000002-0000-0000-0000-000000000000', 'E-01-L',
     'A', 'available', TRUE, 'aa000001-0000-0000-0000-000000000000', NOW() - INTERVAL '1 month',  NOW());

-- â”€â”€ Slabs â€” Stone Source (sup2) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
-- Blue Bahia 3cm Polished (variant dd000005)
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee030001-0000-0000-0000-000000000000', 'dd000005-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'BB3P-001', 'quartzite', 'Blue Bahia', 'blue', 'exotic', 'BR', 'Bahia Mining Group', 'LOT-2025-0410', 'BLK-3X',
     3.0, 'polished', 3000, 1600, 145.00, 'bb000003-0000-0000-0000-000000000000', 'A-01-L',
     'A', 'available', TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '2 months', NOW()),

    ('ee030002-0000-0000-0000-000000000000', 'dd000005-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'BB3P-002', 'quartzite', 'Blue Bahia', 'blue', 'exotic', 'BR', 'Bahia Mining Group', 'LOT-2025-0410', 'BLK-3X',
     3.0, 'polished', 2900, 1580, 145.00, 'bb000003-0000-0000-0000-000000000000', 'A-01-R',
     'A', 'available', TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '2 months', NOW());

-- Nordic White 3cm Honed (variant dd000006)
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee040001-0000-0000-0000-000000000000', 'dd000006-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222',
     'NW3H-001', 'quartzite', 'Nordic White', 'white', 'veined', 'NO', 'Oppdal Quarry', 'LOT-2025-0500', 'BLK-11B',
     3.0, 'honed', 3100, 1600, 68.00, 'bb000003-0000-0000-0000-000000000000', 'B-01-L',
     'A', 'available', TRUE, 'aa000002-0000-0000-0000-000000000000', NOW() - INTERVAL '2 months', NOW());

-- â”€â”€ Slabs â€” Premier Granite (sup3) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
-- Kashmir White 3cm Polished (variant dd000007)
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee050001-0000-0000-0000-000000000000', 'dd000007-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'KW3P-001', 'granite', 'Kashmir White', 'white', 'flecked', 'IN', 'Tamil Nadu Granite', 'LOT-2025-0610', 'BLK-55A',
     3.0, 'polished', 3050, 1600, 18.50, 'bb000004-0000-0000-0000-000000000000', 'A-01-L',
     'A', 'available', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '6 months', NOW()),

    ('ee050002-0000-0000-0000-000000000000', 'dd000007-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'KW3P-002', 'granite', 'Kashmir White', 'white', 'flecked', 'IN', 'Tamil Nadu Granite', 'LOT-2025-0610', 'BLK-55A',
     3.0, 'polished', 3100, 1620, 18.50, 'bb000004-0000-0000-0000-000000000000', 'A-01-R',
     'A', 'available', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '6 months', NOW()),

    ('ee050003-0000-0000-0000-000000000000', 'dd000007-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'KW3P-003', 'granite', 'Kashmir White', 'white', 'flecked', 'IN', 'Tamil Nadu Granite', 'LOT-2025-0610', 'BLK-55B',
     3.0, 'polished', 3000, 1580, 18.50, 'bb000004-0000-0000-0000-000000000000', 'A-02-L',
     'B', 'available', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '6 months', NOW());

-- Ubatuba Green 3cm Polished (variant dd000008)
INSERT INTO slabs (
    id, variant_id, tenant_id, internal_ref,
    material_type, material_name, color_family, pattern,
    origin_country, quarry_name, lot_number, block_number,
    thickness_cm, finish, gross_length_mm, gross_width_mm,
    price_override, warehouse_id, rack_location,
    quality_grade, status, is_active, created_by, created_at, updated_at
) VALUES
    ('ee060001-0000-0000-0000-000000000000', 'dd000008-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'UG3P-001', 'granite', 'Ubatuba Green', 'green', 'flecked', 'BR', 'Minas Gerais Region', 'LOT-2025-0701', 'BLK-9G',
     3.0, 'polished', 3200, 1650, 16.00, 'bb000004-0000-0000-0000-000000000000', 'B-01-L',
     'A', 'available', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '6 months', NOW()),

    ('ee060002-0000-0000-0000-000000000000', 'dd000008-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333',
     'UG3P-002', 'granite', 'Ubatuba Green', 'green', 'flecked', 'BR', 'Minas Gerais Region', 'LOT-2025-0701', 'BLK-9G',
     3.0, 'polished', 3150, 1600, 16.00, 'bb000004-0000-0000-0000-000000000000', 'B-01-R',
     'A', 'available', TRUE, 'aa000003-0000-0000-0000-000000000000', NOW() - INTERVAL '6 months', NOW());

-- â”€â”€ Connections â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO connections (
    id, fabricator_id, supplier_id, status, pricing_tier,
    initiated_by, approved_by, request_message,
    requested_at, connected_at, created_at, updated_at
) VALUES
    -- fab1 â†” sup1: active preferred â€” the main trading relationship
    ('ff000001-0000-0000-0000-000000000000',
     '44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111',
     'active', 'preferred',
     'aa000004-0000-0000-0000-000000000000', 'aa000001-0000-0000-0000-000000000000',
     'We specialize in high-end residential and would love access to your Italian marble inventory.',
     NOW() - INTERVAL '4 months', NOW() - INTERVAL '4 months' + INTERVAL '2 days',
     NOW() - INTERVAL '4 months', NOW()),

    -- fab1 â†” sup2: active standard
    ('ff000002-0000-0000-0000-000000000000',
     '44444444-4444-4444-4444-444444444444', '22222222-2222-2222-2222-222222222222',
     'active', 'standard',
     'aa000004-0000-0000-0000-000000000000', 'aa000002-0000-0000-0000-000000000000',
     'Looking for exotic quartzite sources for our designer clients.',
     NOW() - INTERVAL '2 months', NOW() - INTERVAL '2 months' + INTERVAL '1 day',
     NOW() - INTERVAL '2 months', NOW()),

    -- fab2 â†” sup1: active standard
    ('ff000003-0000-0000-0000-000000000000',
     '55555555-5555-5555-5555-555555555555', '11111111-1111-1111-1111-111111111111',
     'active', 'standard',
     'aa000005-0000-0000-0000-000000000000', 'aa000001-0000-0000-0000-000000000000',
     'Elite boutique shop interested in your Calacatta and exotic offerings.',
     NOW() - INTERVAL '2 months', NOW() - INTERVAL '2 months' + INTERVAL '3 days',
     NOW() - INTERVAL '2 months', NOW()),

    -- fab2 â†” sup3: pending (no approved_by yet)
    ('ff000004-0000-0000-0000-000000000000',
     '55555555-5555-5555-5555-555555555555', '33333333-3333-3333-3333-333333333333',
     'pending', 'standard',
     'aa000005-0000-0000-0000-000000000000', NULL,
     'We do high-end residential work and would like to add Premier Granite to our supplier network.',
     NOW() - INTERVAL '3 days', NULL,
     NOW() - INTERVAL '3 days', NOW());

-- Connection history â€” manual because trigger only fires on UPDATE, not INSERT
INSERT INTO connection_history (id, connection_id, from_status, to_status, changed_by, reason, changed_at) VALUES
    ('cf000001-0000-0000-0000-000000000000', 'ff000001-0000-0000-0000-000000000000', 'pending', 'active',  'aa000001-0000-0000-0000-000000000000', NULL, NOW() - INTERVAL '4 months' + INTERVAL '2 days'),
    ('cf000002-0000-0000-0000-000000000000', 'ff000002-0000-0000-0000-000000000000', 'pending', 'active',  'aa000002-0000-0000-0000-000000000000', NULL, NOW() - INTERVAL '2 months' + INTERVAL '1 day'),
    ('cf000003-0000-0000-0000-000000000000', 'ff000003-0000-0000-0000-000000000000', 'pending', 'active',  'aa000001-0000-0000-0000-000000000000', NULL, NOW() - INTERVAL '2 months' + INTERVAL '3 days');

-- â”€â”€ Addresses â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO addresses (id, tenant_id, label, line1, line2, city, state_province, postal_code, country, is_default, created_at) VALUES
    ('ad000001-0000-0000-0000-000000000000', '44444444-4444-4444-4444-444444444444', 'Shop',     '4455 Industrial Way',      NULL,        'Duluth',  'GA', '30096', 'US', TRUE,  NOW() - INTERVAL '5 months'),
    ('ad000002-0000-0000-0000-000000000000', '44444444-4444-4444-4444-444444444444', 'Job Site', '140 Peachtree St NW',      'Suite 100', 'Atlanta', 'GA', '30303', 'US', FALSE, NOW() - INTERVAL '1 month'),
    ('ad000003-0000-0000-0000-000000000000', '55555555-5555-5555-5555-555555555555', 'Shop',     '720 Ponce De Leon Ave NE', NULL,        'Atlanta', 'GA', '30306', 'US', TRUE,  NOW() - INTERVAL '3 months');

-- â”€â”€ Jobs (fab1 = Countertop Kings) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO jobs (
    id, tenant_id, job_number, job_name,
    customer_name, customer_email, customer_phone,
    status, template_date, fabrication_date, install_date,
    material_budget, notes, created_by, created_at, updated_at
) VALUES
    ('0b000001-0000-0000-0000-000000000000',
     '44444444-4444-4444-4444-444444444444',
     'JOB-2026-0001', 'Smith Kitchen Renovation',
     'Robert & Linda Smith', 'linda.smith@email.example.com', '+1 (770) 555-9901',
     'fabricating', '2026-06-10', '2026-06-20', '2026-06-28',
     3500.00,
     'Full kitchen countertop replacement. 3cm Calacatta Gold island top + Carrara White perimeter.',
     'aa000004-0000-0000-0000-000000000000',
     NOW() - INTERVAL '3 weeks', NOW()),

    ('0b000002-0000-0000-0000-000000000000',
     '44444444-4444-4444-4444-444444444444',
     'JOB-2026-0002', 'Johnson Master Bath',
     'Thomas Johnson', 'tom.johnson@email.example.com', '+1 (404) 555-8822',
     'quoted', NULL, NULL, NULL,
     1800.00,
     'Master bath vanity top, shower bench, and niche. Client leaning toward Absolute Black.',
     'aa000004-0000-0000-0000-000000000000',
     NOW() - INTERVAL '1 week', NOW());

-- â”€â”€ Purchase Orders â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
-- PO-2026-000001: sent (fab1 â†’ sup1, Smith Kitchen)
--   Line 1: Calacatta Gold 3cm Pol â€” slab CAL3P-001 (ee010001), 55.11 sqft Ã— $85 = $4,684.35
--   Line 2: Carrara White 3cm Pol  â€” slab CW3P-001  (ee000001), 56.83 sqft Ã— $28.50 = $1,619.66
--   Total: $6,304.01
INSERT INTO purchase_orders (
    id, po_number, fabricator_id, supplier_id, job_id,
    status, status_changed,
    subtotal, total_amount, currency,
    delivery_address_id, requested_delivery,
    fabricator_notes,
    sent_at, created_by, created_at, updated_at
) VALUES (
    'c0000001-0000-0000-0000-000000000000',
    'PO-2026-000001',
    '44444444-4444-4444-4444-444444444444',
    '11111111-1111-1111-1111-111111111111',
    '0b000001-0000-0000-0000-000000000000',
    'sent', NOW() - INTERVAL '5 days',
    6304.01, 6304.01, 'USD',
    'ad000001-0000-0000-0000-000000000000', '2026-06-18',
    'Please pack securely â€” these are going straight to the shop for fabrication next week.',
    NOW() - INTERVAL '5 days',
    'aa000004-0000-0000-0000-000000000000',
    NOW() - INTERVAL '5 days', NOW()
);

-- PO-2026-000002: draft (fab1 â†’ sup1, Johnson Bath â€” not sent yet)
INSERT INTO purchase_orders (
    id, po_number, fabricator_id, supplier_id, job_id,
    status, status_changed,
    subtotal, total_amount, currency,
    delivery_address_id,
    fabricator_notes,
    created_by, created_at, updated_at
) VALUES (
    'c0000002-0000-0000-0000-000000000000',
    'PO-2026-000002',
    '44444444-4444-4444-4444-444444444444',
    '11111111-1111-1111-1111-111111111111',
    '0b000002-0000-0000-0000-000000000000',
    'draft', NOW() - INTERVAL '2 days',
    0.00, 0.00, 'USD',
    'ad000001-0000-0000-0000-000000000000',
    'Waiting for client confirmation on Absolute Black before sending.',
    'aa000004-0000-0000-0000-000000000000',
    NOW() - INTERVAL '2 days', NOW()
);

-- PO status history (trigger fires on UPDATE, not INSERT â€” seed manually)
INSERT INTO po_status_history (id, po_id, from_status, to_status, changed_by, note, changed_at) VALUES
    ('e0000001-0000-0000-0000-000000000000',
     'c0000001-0000-0000-0000-000000000000',
     'draft', 'sent', 'aa000004-0000-0000-0000-000000000000',
     'Sent to Marble Masters for Smith Kitchen project.',
     NOW() - INTERVAL '5 days');

-- â”€â”€ PO line items â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
INSERT INTO po_line_items (
    id, po_id, variant_id, slab_id,
    item_snapshot,
    quantity, unit_of_measure, unit_price, line_total, currency,
    status, created_at, updated_at
) VALUES
(
    'd0000001-0000-0000-0000-000000000000',
    'c0000001-0000-0000-0000-000000000000',
    'dd000003-0000-0000-0000-000000000000',
    'ee010001-0000-0000-0000-000000000000',
    '{"material_name":"Calacatta Gold","variant_name":"Calacatta Gold 3cm Polished","sku":"MM-CAL-3CM-POL","internal_ref":"CAL3P-001","gross_length_mm":3200,"gross_width_mm":1600,"thickness_cm":3,"finish":"polished","quality_grade":"A","warehouse":"Norcross Main Yard","rack_location":"B-01-L"}'::jsonb,
    55.11, 'sqft', 85.00, 4684.35, 'USD',
    'pending', NOW() - INTERVAL '5 days', NOW()
),
(
    'd0000002-0000-0000-0000-000000000000',
    'c0000001-0000-0000-0000-000000000000',
    'dd000001-0000-0000-0000-000000000000',
    'ee000001-0000-0000-0000-000000000000',
    '{"material_name":"Carrara White","variant_name":"Carrara White 3cm Polished","sku":"MM-CW-3CM-POL","internal_ref":"CW3P-001","gross_length_mm":3200,"gross_width_mm":1650,"thickness_cm":3,"finish":"polished","quality_grade":"A","warehouse":"Norcross Main Yard","rack_location":"A-01-L"}'::jsonb,
    56.83, 'sqft', 28.50, 1619.66, 'USD',
    'pending', NOW() - INTERVAL '5 days', NOW()
);

-- â”€â”€ Reserve slabs that are on the sent PO â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
UPDATE slabs
SET    status         = 'reserved',
       reserved_for_po = 'c0000001-0000-0000-0000-000000000000',
       status_changed  = NOW() - INTERVAL '5 days',
       updated_at      = NOW() - INTERVAL '5 days'
WHERE  id IN (
    'ee010001-0000-0000-0000-000000000000',
    'ee000001-0000-0000-0000-000000000000'
);

RESET row_security;

