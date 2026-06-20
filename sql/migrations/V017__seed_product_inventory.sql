-- ============================================================
-- V017: Seed non-slab product inventory for Marble Masters (sup1)
--
-- Adds realistic quantity-tracked products a stone supplier would sell:
--   2 sinks, 1 sealer, 1 adhesive, 1 blade, 1 edge template
-- Uses UUIDs in the pr/vr series to avoid collisions.
-- ============================================================

SET row_security = OFF;

-- ── Products ──────────────────────────────────────────────────────────────────
INSERT INTO products (id, tenant_id, category_code, name, brand, short_description, is_active, created_at, updated_at) VALUES
    ('pr000001-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'sink', 'Stainless Steel Undermount Sink', 'Blanco',
     'Single-bowl 18-gauge stainless undermount. Fits 33" base cabinets. Radius corners.',
     TRUE, NOW() - INTERVAL '3 months', NOW()),

    ('pr000002-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'sink', 'Farmhouse Apron Front Sink', 'Kohler',
     'Cast-iron apron sink with enameled interior. 36" single bowl. White finish.',
     TRUE, NOW() - INTERVAL '3 months', NOW()),

    ('pr000003-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'sealer', '511 Impregnator Sealer', 'Miracle Sealants',
     'Penetrating sealer for natural stone, grout, and masonry. Quart covers 250–500 sqft.',
     TRUE, NOW() - INTERVAL '2 months', NOW()),

    ('pr000004-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'adhesive', 'Tixo White Adhesive', 'Tenax',
     'Polyester-based adhesive for stone seams and cracks. Translucent white. 1-liter kit.',
     TRUE, NOW() - INTERVAL '2 months', NOW()),

    ('pr000005-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'blade', 'Premium Bridge Saw Blade 14"', 'MK Diamond',
     'Turbo segmented diamond blade for granite and quartzite. 14" × 1" arbor. Wet cut.',
     TRUE, NOW() - INTERVAL '1 month', NOW()),

    ('pr000006-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'edge_profile_template', 'Ogee Edge Router Template Set', 'Prolam',
     'Full ogee and demi-ogee template set in 18mm HDPE. Works with Festool and Fein routers.',
     TRUE, NOW() - INTERVAL '1 month', NOW());

-- ── Product variants (is_slab_variant = FALSE — qty tracked) ─────────────────
INSERT INTO product_variants (
    id, product_id, tenant_id, sku, variant_name, attributes,
    unit_of_measure, base_price, currency, qty_available, qty_reserved,
    is_slab_variant, status, lead_time_days, created_at, updated_at
) VALUES
    -- Blanco sink (each)
    ('vr000001-0000-0000-0000-000000000000',
     'pr000001-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'BL-440-SS-33', 'Blanco 440 Single 33" Stainless',
     '{"bowl_size":"33x19","gauge":18,"finish":"satin"}'::jsonb,
     'each', 245.00, 'USD', 8, 0, FALSE, 'active', 5,
     NOW() - INTERVAL '3 months', NOW()),

    -- Kohler farmhouse sink (each)
    ('vr000002-0000-0000-0000-000000000000',
     'pr000002-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'KH-WHITEHAVEN-36', 'Kohler Whitehaven 36" White Farmhouse',
     '{"bowl_size":"36x22","material":"cast_iron","finish":"white_enamel"}'::jsonb,
     'each', 1150.00, 'USD', 3, 0, FALSE, 'active', 7,
     NOW() - INTERVAL '3 months', NOW()),

    -- Miracle 511 Sealer — quart
    ('vr000003-0000-0000-0000-000000000000',
     'pr000003-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'MS-511-QT', '511 Impregnator Quart',
     '{"volume_oz":32,"coverage_sqft":500,"voc_compliant":true}'::jsonb,
     'each', 28.50, 'USD', 24, 0, FALSE, 'active', 2,
     NOW() - INTERVAL '2 months', NOW()),

    -- Miracle 511 Sealer — gallon
    ('vr000004-0000-0000-0000-000000000000',
     'pr000003-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'MS-511-GAL', '511 Impregnator Gallon',
     '{"volume_oz":128,"coverage_sqft":2000,"voc_compliant":true}'::jsonb,
     'gallon', 89.00, 'USD', 6, 0, FALSE, 'active', 2,
     NOW() - INTERVAL '2 months', NOW()),

    -- Tenax Tixo White
    ('vr000005-0000-0000-0000-000000000000',
     'pr000004-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'TX-TIXO-W-1L', 'Tixo White 1-Liter Kit',
     '{"color":"white","base":"polyester","viscosity":"medium"}'::jsonb,
     'each', 34.00, 'USD', 18, 0, FALSE, 'active', 3,
     NOW() - INTERVAL '2 months', NOW()),

    -- MK Diamond blade
    ('vr000006-0000-0000-0000-000000000000',
     'pr000005-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'MK-900-14IN', 'MK-900 14" Premium Bridge Blade',
     '{"diameter_in":14,"arbor_in":1,"segment":"turbo","cut_type":"wet"}'::jsonb,
     'each', 185.00, 'USD', 5, 0, FALSE, 'active', 4,
     NOW() - INTERVAL '1 month', NOW()),

    -- Ogee template set
    ('vr000007-0000-0000-0000-000000000000',
     'pr000006-0000-0000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'PL-OGEE-SET', 'Ogee Full + Demi Set — 18mm HDPE',
     '{"profiles":["full_ogee","demi_ogee"],"material":"hdpe","thickness_mm":18}'::jsonb,
     'set', 98.00, 'USD', 4, 0, FALSE, 'active', 5,
     NOW() - INTERVAL '1 month', NOW());

RESET row_security;
