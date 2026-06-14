-- ============================================================
-- V003: Supplier profiles, fabricator profiles, addresses, warehouses
-- ============================================================

-- ── Supplier profile ──────────────────────────────────────────────────────────
CREATE TABLE supplier_profiles (
    tenant_id           UUID            NOT NULL,
    display_name        VARCHAR(300)    NOT NULL,
    logo_url            VARCHAR(500),
    description         TEXT,
    website             VARCHAR(300),
    phone               VARCHAR(30),
    address_line1       VARCHAR(200),
    address_line2       VARCHAR(200),
    city                VARCHAR(100),
    state_province      VARCHAR(100),
    postal_code         VARCHAR(20),
    country             VARCHAR(2)      NOT NULL DEFAULT 'US',
    established_year    SMALLINT,
    verified            BOOLEAN         NOT NULL DEFAULT FALSE,
    verified_at         TIMESTAMPTZ,
    -- Computed stats (refreshed by background worker)
    avg_lead_days       NUMERIC(6,2),
    fulfillment_rate    NUMERIC(5,2),   -- percentage 0-100
    avg_response_hrs    NUMERIC(8,2),
    total_slabs_sold    INTEGER         NOT NULL DEFAULT 0,
    warehouse_count     INTEGER         NOT NULL DEFAULT 0,
    -- Notification preferences stored as JSONB
    notification_prefs  JSONB           NOT NULL DEFAULT '{}'::JSONB,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_supplier_profiles     PRIMARY KEY (tenant_id),
    CONSTRAINT fk_supplier_tenant       FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT ck_supplier_rate         CHECK (fulfillment_rate IS NULL OR (fulfillment_rate >= 0 AND fulfillment_rate <= 100))
);

COMMENT ON TABLE  supplier_profiles             IS 'Public and private profile data for supplier tenants.';
COMMENT ON COLUMN supplier_profiles.verified    IS 'Set by StoneBridge admin after document verification.';
COMMENT ON COLUMN supplier_profiles.notification_prefs
    IS 'JSONB: { "new_po": {"email":true,"in_app":true,"sms":false}, ... }';

-- ── Fabricator profile ────────────────────────────────────────────────────────
CREATE TABLE fabricator_profiles (
    tenant_id           UUID            NOT NULL,
    display_name        VARCHAR(300)    NOT NULL,
    logo_url            VARCHAR(500),
    description         TEXT,
    website             VARCHAR(300),
    phone               VARCHAR(30),
    address_line1       VARCHAR(200),
    city                VARCHAR(100),
    state_province      VARCHAR(100),
    postal_code         VARCHAR(20),
    country             VARCHAR(2)      NOT NULL DEFAULT 'US',
    shop_size           VARCHAR(20),    -- solo | small | medium | large | enterprise
    monthly_job_volume  SMALLINT,
    notification_prefs  JSONB           NOT NULL DEFAULT '{}'::JSONB,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_fabricator_profiles   PRIMARY KEY (tenant_id),
    CONSTRAINT fk_fabricator_tenant     FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT ck_fabricator_shop_size  CHECK (shop_size IN ('solo','small','medium','large','enterprise'))
);

-- ── Addresses ─────────────────────────────────────────────────────────────────
CREATE TABLE addresses (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       UUID            NOT NULL,
    label           VARCHAR(100),       -- 'Main office', 'Job site', 'Warehouse'
    line1           VARCHAR(200)    NOT NULL,
    line2           VARCHAR(200),
    city            VARCHAR(100)    NOT NULL,
    state_province  VARCHAR(100),
    postal_code     VARCHAR(20),
    country         VARCHAR(2)      NOT NULL DEFAULT 'US',
    is_default      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_addresses         PRIMARY KEY (id),
    CONSTRAINT fk_addresses_tenant  FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

COMMENT ON TABLE addresses IS 'Reusable delivery and billing addresses per tenant.';

-- ── Warehouses ────────────────────────────────────────────────────────────────
CREATE TABLE warehouses (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       UUID            NOT NULL,
    name            VARCHAR(200)    NOT NULL,
    address_line1   VARCHAR(200),
    city            VARCHAR(100),
    state_province  VARCHAR(100),
    postal_code     VARCHAR(20),
    country         VARCHAR(2)      NOT NULL DEFAULT 'US',
    phone           VARCHAR(30),
    is_primary      BOOLEAN         NOT NULL DEFAULT FALSE,
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_warehouses        PRIMARY KEY (id),
    CONSTRAINT fk_warehouses_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

COMMENT ON TABLE  warehouses            IS 'Physical storage locations belonging to a supplier tenant.';
COMMENT ON COLUMN warehouses.is_primary IS 'Only one warehouse per tenant can be primary. Enforced by trigger.';