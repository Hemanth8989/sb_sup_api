-- ============================================================
-- V004: Products and product variants
-- Two types of variants:
--   is_slab_variant = TRUE  → individual physical slabs tracked in slabs table
--   is_slab_variant = FALSE → quantity-tracked consumables (blades, adhesives, etc.)
-- ============================================================

CREATE TABLE product_categories (
    id          SMALLSERIAL     PRIMARY KEY,
    code        VARCHAR(50)     NOT NULL UNIQUE,
    label       VARCHAR(100)    NOT NULL,
    sort_order  SMALLINT        NOT NULL DEFAULT 0
);

COMMENT ON TABLE product_categories IS 'Lookup table for product category codes and display labels.';

INSERT INTO product_categories (code, label, sort_order) VALUES
    ('slab',                  'Natural Stone Slab',       1),
    ('blade',                 'Cutting Blade',            2),
    ('bit',                   'Router Bit',               3),
    ('wheel',                 'Grinding Wheel',           4),
    ('pad',                   'Polishing Pad',            5),
    ('sink',                  'Sink / Undermount',        6),
    ('faucet_hole_template',  'Faucet Hole Template',     7),
    ('bracket',               'Bracket / Support',        8),
    ('clip',                  'Clip / Fastener',          9),
    ('adhesive',              'Adhesive / Epoxy',        10),
    ('sealer',                'Sealer / Impregnator',    11),
    ('cleaner',               'Cleaner / Maintenance',   12),
    ('colorant',              'Color Fill / Colorant',   13),
    ('abrasive',              'Abrasive / Sandpaper',    14),
    ('edge_profile_template', 'Edge Profile Template',   15),
    ('backsplash_tile',       'Backsplash Tile',         16),
    ('trim',                  'Trim / Molding',          17),
    ('ppe',                   'PPE / Safety',            18),
    ('dust_collection',       'Dust Collection',         19),
    ('packaging',             'Packaging / Crating',     20),
    ('tool',                  'Hand Tool',               21),
    ('equipment',             'Power Equipment',         22),
    ('other',                 'Other',                   99);

-- ── Products ──────────────────────────────────────────────────────────────────
CREATE TABLE products (
    id                  UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id           UUID            NOT NULL,   -- supplier tenant
    category_code       VARCHAR(50)     NOT NULL,
    name                VARCHAR(300)    NOT NULL,
    brand               VARCHAR(150),
    short_description   TEXT,
    specifications      JSONB           NOT NULL DEFAULT '{}'::JSONB,
    is_active           BOOLEAN         NOT NULL DEFAULT TRUE,
    created_by          UUID,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_products          PRIMARY KEY (id),
    CONSTRAINT fk_products_tenant   FOREIGN KEY (tenant_id)     REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_products_category FOREIGN KEY (category_code) REFERENCES product_categories(code),
    CONSTRAINT ck_products_name     CHECK (LENGTH(TRIM(name)) > 0)
);

COMMENT ON TABLE  products                  IS 'Product catalogue entries. One product can have many variants.';
COMMENT ON COLUMN products.specifications   IS 'JSONB shared attributes for all variants of this product.';
COMMENT ON COLUMN products.category_code    IS 'FK to product_categories.code — slab, blade, adhesive, etc.';

-- Full-text search index for product name and description
CREATE INDEX idx_products_fts ON products
    USING GIN (to_tsvector('english', COALESCE(name,'') || ' ' || COALESCE(brand,'') || ' ' || COALESCE(short_description,'')));

-- ── Product variants ──────────────────────────────────────────────────────────
CREATE TABLE product_variants (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    product_id      UUID            NOT NULL,
    tenant_id       UUID            NOT NULL,   -- denormalised for RLS
    sku             VARCHAR(100)    NOT NULL,
    variant_name    VARCHAR(200)    NOT NULL,
    attributes      JSONB           NOT NULL DEFAULT '{}'::JSONB,
    unit_of_measure VARCHAR(20)     NOT NULL DEFAULT 'each',
    base_price      NUMERIC(12,2)   NOT NULL DEFAULT 0,
    currency        VARCHAR(3)      NOT NULL DEFAULT 'USD',
    -- Quantity tracking (NULL for slab variants — tracked per physical slab)
    qty_available   INTEGER,
    qty_reserved    INTEGER         NOT NULL DEFAULT 0,
    -- Slab flag
    is_slab_variant BOOLEAN         NOT NULL DEFAULT FALSE,
    -- Status
    status          VARCHAR(20)     NOT NULL DEFAULT 'active',
    lead_time_days  SMALLINT,
    -- Photos
    primary_photo_url VARCHAR(500),
    -- Audit
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_product_variants          PRIMARY KEY (id),
    CONSTRAINT fk_variants_product          FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE,
    CONSTRAINT fk_variants_tenant           FOREIGN KEY (tenant_id)  REFERENCES tenants(id)  ON DELETE CASCADE,
    CONSTRAINT uq_variants_sku_tenant       UNIQUE (tenant_id, sku),
    CONSTRAINT ck_variants_uom              CHECK (unit_of_measure IN (
                                                'each','sqft','sqm','linear_ft','linear_m',
                                                'liter','gallon','kg','lb','box','case',
                                                'roll','bag','pair','set')),
    CONSTRAINT ck_variants_status           CHECK (status IN ('active','discontinued','out_of_stock')),
    CONSTRAINT ck_variants_price            CHECK (base_price >= 0),
    CONSTRAINT ck_variants_qty             CHECK (
                                                (is_slab_variant = TRUE  AND qty_available IS NULL) OR
                                                (is_slab_variant = FALSE AND qty_available >= 0)
                                            )
);

COMMENT ON TABLE  product_variants                  IS 'Purchasable variants of a product. Slab variants link to the slabs table.';
COMMENT ON COLUMN product_variants.is_slab_variant  IS 'TRUE = tracked in slabs table (one row per physical piece). FALSE = quantity-tracked.';
COMMENT ON COLUMN product_variants.attributes       IS 'JSONB variant-level specs: {"thickness_cm":3,"finish":"polished","diameter_mm":100}.';
COMMENT ON COLUMN product_variants.qty_available    IS 'NULL for slab variants. Stock on hand for quantity-tracked products.';

-- ── Product price history ─────────────────────────────────────────────────────
CREATE TABLE product_price_history (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    variant_id      UUID            NOT NULL,
    tenant_id       UUID            NOT NULL,
    old_price       NUMERIC(12,2)   NOT NULL,
    new_price       NUMERIC(12,2)   NOT NULL,
    currency        VARCHAR(3)      NOT NULL DEFAULT 'USD',
    changed_by      UUID,
    changed_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_product_price_history         PRIMARY KEY (id),
    CONSTRAINT fk_price_history_variant         FOREIGN KEY (variant_id) REFERENCES product_variants(id) ON DELETE CASCADE,
    CONSTRAINT fk_price_history_tenant          FOREIGN KEY (tenant_id)  REFERENCES tenants(id)           ON DELETE CASCADE
);