-- ============================================================
-- V005: Slabs, slab photos, slab bundles
-- One row in slabs = one physical piece of stone in the supplier's yard
-- ============================================================

-- ── Slab bundles (quarry lots) ────────────────────────────────────────────────
CREATE TABLE slab_bundles (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       UUID            NOT NULL,
    bundle_ref      VARCHAR(100)    NOT NULL,
    material_name   VARCHAR(200)    NOT NULL,
    quarry_name     VARCHAR(200),
    origin_country  VARCHAR(2),
    arrival_date    DATE,
    invoice_ref     VARCHAR(100),
    notes           TEXT,
    slab_count      INTEGER         NOT NULL DEFAULT 0,
    active_count    INTEGER         NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_slab_bundles          PRIMARY KEY (id),
    CONSTRAINT fk_slab_bundles_tenant   FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_slab_bundles_ref      UNIQUE (tenant_id, bundle_ref)
);

COMMENT ON TABLE slab_bundles IS 'Groups slabs from the same quarry lot. Enables bookmatched slab identification.';

-- ── Slabs ─────────────────────────────────────────────────────────────────────
CREATE TABLE slabs (
    -- Identity
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    variant_id      UUID            NOT NULL,
    tenant_id       UUID            NOT NULL,   -- supplier tenant (denormalised for RLS)
    bundle_id       UUID,
    internal_ref    VARCHAR(100)    NOT NULL,
    barcode         VARCHAR(100),

    -- Material classification
    material_type   VARCHAR(30)     NOT NULL,
    material_name   VARCHAR(120)    NOT NULL,
    color_family    VARCHAR(30),
    pattern         VARCHAR(30),
    origin_country  VARCHAR(2),
    quarry_name     VARCHAR(120),
    lot_number      VARCHAR(50),
    block_number    VARCHAR(50),

    -- Physical dimensions
    thickness_cm    NUMERIC(5,2)    NOT NULL,
    finish          VARCHAR(20)     NOT NULL,
    gross_length_mm INTEGER         NOT NULL,
    gross_width_mm  INTEGER         NOT NULL,

    -- Computed dimensions — stored for query performance
    -- Equivalent to SQL Server PERSISTED computed columns
    net_sqft        NUMERIC(10,4)   GENERATED ALWAYS AS
                        (ROUND(CAST(gross_length_mm AS NUMERIC) * gross_width_mm / 1000000.0 * 10.7639, 4))
                    STORED,
    net_sqm         NUMERIC(10,6)   GENERATED ALWAYS AS
                        (ROUND(CAST(gross_length_mm AS NUMERIC) * gross_width_mm / 1000000.0, 6))
                    STORED,

    -- Weight
    weight_kg       NUMERIC(8,2),

    -- Pricing
    price_override  NUMERIC(12,2),

    -- Location
    warehouse_id    UUID,
    rack_location   VARCHAR(50),

    -- Quality
    quality_grade   CHAR(1)         NOT NULL DEFAULT 'A',

    -- Status lifecycle
    status          VARCHAR(20)     NOT NULL DEFAULT 'available',
    status_changed  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    reserved_for_po UUID,

    -- Flags
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    is_remnant      BOOLEAN         NOT NULL DEFAULT FALSE,
    parent_slab_id  UUID,

    -- Audit
    created_by      UUID,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_slabs                 PRIMARY KEY (id),
    CONSTRAINT fk_slabs_variant         FOREIGN KEY (variant_id)    REFERENCES product_variants(id),
    CONSTRAINT fk_slabs_tenant          FOREIGN KEY (tenant_id)     REFERENCES tenants(id)          ON DELETE CASCADE,
    CONSTRAINT fk_slabs_bundle          FOREIGN KEY (bundle_id)     REFERENCES slab_bundles(id)     ON DELETE SET NULL,
    CONSTRAINT fk_slabs_warehouse       FOREIGN KEY (warehouse_id)  REFERENCES warehouses(id)       ON DELETE SET NULL,
    CONSTRAINT fk_slabs_parent          FOREIGN KEY (parent_slab_id) REFERENCES slabs(id)           ON DELETE SET NULL,
    CONSTRAINT uq_slabs_internal_ref    UNIQUE (tenant_id, internal_ref),
    CONSTRAINT ck_slabs_material_type   CHECK (material_type IN (
                                            'granite','marble','quartzite','quartz','porcelain',
                                            'dekton','limestone','travertine','onyx','slate','soapstone','other')),
    CONSTRAINT ck_slabs_finish          CHECK (finish IN ('polished','honed','leathered','brushed','sandblasted','flamed','natural')),
    CONSTRAINT ck_slabs_color_family    CHECK (color_family IN (
                                            'white','cream','beige','gray','charcoal',
                                            'black','blue','green','red','brown','gold','multi')),
    CONSTRAINT ck_slabs_pattern         CHECK (pattern IN ('solid','veined','bookmatched','flecked','exotic')),
    CONSTRAINT ck_slabs_quality         CHECK (quality_grade IN ('A','B','C')),
    CONSTRAINT ck_slabs_status          CHECK (status IN ('available','reserved','allocated','shipped','hold','sold')),
    CONSTRAINT ck_slabs_thickness       CHECK (thickness_cm > 0 AND thickness_cm <= 10),
    CONSTRAINT ck_slabs_dimensions      CHECK (gross_length_mm > 0 AND gross_width_mm > 0),
    CONSTRAINT ck_slabs_price           CHECK (price_override IS NULL OR price_override >= 0)
);

COMMENT ON TABLE  slabs                     IS 'One row = one physical stone slab in a supplier''s inventory.';
COMMENT ON COLUMN slabs.net_sqft            IS 'GENERATED ALWAYS AS STORED: gross_length_mm * gross_width_mm / 1,000,000 * 10.7639';
COMMENT ON COLUMN slabs.net_sqm             IS 'GENERATED ALWAYS AS STORED: gross_length_mm * gross_width_mm / 1,000,000';
COMMENT ON COLUMN slabs.internal_ref        IS 'Supplier-assigned reference. Unique per tenant.';
COMMENT ON COLUMN slabs.reserved_for_po     IS 'PO ID when status = reserved. NULL otherwise.';
COMMENT ON COLUMN slabs.status              IS 'Lifecycle: available → reserved → allocated → shipped → sold. Also: hold (manual pause).';

-- Full-text search vector for slab catalog search
-- Stored separately as a generated column for performance
ALTER TABLE slabs ADD COLUMN search_vector TSVECTOR
    GENERATED ALWAYS AS (
        to_tsvector('english',
            COALESCE(material_name, '') || ' ' ||
            COALESCE(material_type, '') || ' ' ||
            COALESCE(color_family, '')  || ' ' ||
            COALESCE(quarry_name, '')   || ' ' ||
            COALESCE(lot_number, '')    || ' ' ||
            COALESCE(internal_ref, '')  || ' ' ||
            COALESCE(origin_country, '') || ' ' ||
            COALESCE(rack_location, '')
        )
    ) STORED;

-- ── Slab photos ───────────────────────────────────────────────────────────────
CREATE TABLE slab_photos (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    slab_id         UUID            NOT NULL,
    tenant_id       UUID            NOT NULL,
    url             VARCHAR(500)    NOT NULL,   -- R2 object key (path, not full URL)
    thumb_url       VARCHAR(500),
    cdn_url         VARCHAR(500),
    photo_type      VARCHAR(20)     NOT NULL DEFAULT 'front',
    sort_order      SMALLINT        NOT NULL DEFAULT 0,
    width_px        INTEGER,
    height_px       INTEGER,
    size_bytes      INTEGER,
    uploaded_by     UUID,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_slab_photos           PRIMARY KEY (id),
    CONSTRAINT fk_slab_photos_slab      FOREIGN KEY (slab_id)   REFERENCES slabs(id)    ON DELETE CASCADE,
    CONSTRAINT fk_slab_photos_tenant    FOREIGN KEY (tenant_id) REFERENCES tenants(id)  ON DELETE CASCADE,
    CONSTRAINT ck_slab_photos_type      CHECK (photo_type IN ('front','back','edge','detail','installed','vein')),
    CONSTRAINT ck_slab_photos_order     CHECK (sort_order >= 0 AND sort_order <= 11)
);

COMMENT ON TABLE  slab_photos           IS 'Photos for a slab stored in Cloudflare R2. Max 12 per slab enforced by trigger.';
COMMENT ON COLUMN slab_photos.url       IS 'R2 object key (e.g. slabs/{slabId}/{uuid}.jpg). Not a full URL.';

-- ── Slab price history ────────────────────────────────────────────────────────
CREATE TABLE slab_price_history (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    slab_id         UUID            NOT NULL,
    tenant_id       UUID            NOT NULL,
    old_price       NUMERIC(12,2),
    new_price       NUMERIC(12,2),
    changed_by      UUID,
    changed_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_slab_price_history        PRIMARY KEY (id),
    CONSTRAINT fk_slab_price_slab           FOREIGN KEY (slab_id)   REFERENCES slabs(id)   ON DELETE CASCADE,
    CONSTRAINT fk_slab_price_tenant         FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);