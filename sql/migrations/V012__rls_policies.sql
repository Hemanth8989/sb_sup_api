-- ============================================================
-- V012: Row Level Security policies
-- PostgreSQL RLS approach:
--   1. Application sets: SET LOCAL app.tenant_id = '<uuid>';
--      SET LOCAL app.tenant_type = 'supplier' | 'fabricator';
--   2. RLS policies call current_setting('app.tenant_id', TRUE)
--   3. Every query is automatically filtered — no WHERE clause needed
--
-- IMPORTANT: SET LOCAL is transaction-scoped.
-- For Dapper (which uses connection-per-query pattern),
-- the application must wrap each query in a transaction
-- to make SET LOCAL effective. Use DbConnectionFactory
-- which calls SET LOCAL in a transaction wrapper.
-- ============================================================

-- ── Enable RLS on all tenant-scoped tables ────────────────────────────────────
ALTER TABLE tenants             ENABLE ROW LEVEL SECURITY;
ALTER TABLE users               ENABLE ROW LEVEL SECURITY;
ALTER TABLE supplier_profiles   ENABLE ROW LEVEL SECURITY;
ALTER TABLE fabricator_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE addresses           ENABLE ROW LEVEL SECURITY;
ALTER TABLE warehouses          ENABLE ROW LEVEL SECURITY;
ALTER TABLE products            ENABLE ROW LEVEL SECURITY;
ALTER TABLE product_variants    ENABLE ROW LEVEL SECURITY;
ALTER TABLE slab_bundles        ENABLE ROW LEVEL SECURITY;
ALTER TABLE slabs                ENABLE ROW LEVEL SECURITY;
ALTER TABLE slab_photos         ENABLE ROW LEVEL SECURITY;
ALTER TABLE connections         ENABLE ROW LEVEL SECURITY;
ALTER TABLE jobs                ENABLE ROW LEVEL SECURITY;
ALTER TABLE purchase_orders     ENABLE ROW LEVEL SECURITY;
ALTER TABLE po_line_items       ENABLE ROW LEVEL SECURITY;
ALTER TABLE notifications       ENABLE ROW LEVEL SECURITY;
ALTER TABLE sync_events         ENABLE ROW LEVEL SECURITY;
ALTER TABLE price_lists         ENABLE ROW LEVEL SECURITY;
ALTER TABLE webhook_endpoints   ENABLE ROW LEVEL SECURITY;
ALTER TABLE saved_searches      ENABLE ROW LEVEL SECURITY;
ALTER TABLE integrations        ENABLE ROW LEVEL SECURITY;

-- ── Helper function — reads tenant ID from session context ────────────────────
CREATE OR REPLACE FUNCTION current_tenant_id()
RETURNS UUID
LANGUAGE SQL
STABLE
AS $$
    SELECT NULLIF(current_setting('app.tenant_id', TRUE), '')::UUID;
$$;

COMMENT ON FUNCTION current_tenant_id()
    IS 'Reads app.tenant_id from PostgreSQL session context. Set by application on every connection via SET LOCAL.';

CREATE OR REPLACE FUNCTION current_tenant_type()
RETURNS TEXT
LANGUAGE SQL
STABLE
AS $$
    SELECT NULLIF(current_setting('app.tenant_type', TRUE), '');
$$;

-- ── Bypass role for migrations and background services ────────────────────────
-- Create a role that bypasses RLS (used for migrations and admin operations)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'stonebridge_admin') THEN
        CREATE ROLE stonebridge_admin;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'stonebridge_api') THEN
        CREATE ROLE stonebridge_api;
    END IF;
END
$$;

-- Admin bypasses all RLS
ALTER TABLE tenants             FORCE ROW LEVEL SECURITY;
ALTER TABLE users               FORCE ROW LEVEL SECURITY;
ALTER TABLE supplier_profiles   FORCE ROW LEVEL SECURITY;
ALTER TABLE fabricator_profiles FORCE ROW LEVEL SECURITY;

-- ── TENANTS table ──────────────────────────────────────────────────────────────
-- A tenant can only see their own row
CREATE POLICY pol_tenants_select ON tenants
    FOR SELECT
    USING (id = current_tenant_id());

CREATE POLICY pol_tenants_update ON tenants
    FOR UPDATE
    USING (id = current_tenant_id());

-- ── USERS table ───────────────────────────────────────────────────────────────
CREATE POLICY pol_users_select ON users
    FOR SELECT
    USING (tenant_id = current_tenant_id());

CREATE POLICY pol_users_all ON users
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- ── SUPPLIER PROFILES ─────────────────────────────────────────────────────────
-- Suppliers see their own profile; fabricators can see any supplier profile (for directory)
CREATE POLICY pol_supplier_profiles_own ON supplier_profiles
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- Allow fabricators to read supplier profiles (needed for directory query)
CREATE POLICY pol_supplier_profiles_fabricator_read ON supplier_profiles
    FOR SELECT
    USING (current_tenant_type() = 'fabricator');

-- ── FABRICATOR PROFILES ───────────────────────────────────────────────────────
CREATE POLICY pol_fabricator_profiles_own ON fabricator_profiles
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- Suppliers can read fabricator profiles for connected fabricators
CREATE POLICY pol_fabricator_profiles_supplier_read ON fabricator_profiles
    FOR SELECT
    USING (
        current_tenant_type() = 'supplier'
        AND EXISTS (
            SELECT 1 FROM connections c
            WHERE c.fabricator_id = fabricator_profiles.tenant_id
              AND c.supplier_id   = current_tenant_id()
              AND c.status        = 'active'
        )
    );

-- ── ADDRESSES ─────────────────────────────────────────────────────────────────
CREATE POLICY pol_addresses_own ON addresses
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- ── WAREHOUSES ────────────────────────────────────────────────────────────────
CREATE POLICY pol_warehouses_own ON warehouses
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- ── PRODUCTS ──────────────────────────────────────────────────────────────────
-- Suppliers see and manage their own products
CREATE POLICY pol_products_supplier ON products
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- Fabricators can read products from connected suppliers
CREATE POLICY pol_products_fabricator_read ON products
    FOR SELECT
    USING (
        current_tenant_type() = 'fabricator'
        AND EXISTS (
            SELECT 1 FROM connections c
            WHERE c.supplier_id   = products.tenant_id
              AND c.fabricator_id = current_tenant_id()
              AND c.status        = 'active'
        )
    );

-- ── PRODUCT VARIANTS ──────────────────────────────────────────────────────────
CREATE POLICY pol_variants_supplier ON product_variants
    FOR ALL
    USING (tenant_id = current_tenant_id());

CREATE POLICY pol_variants_fabricator_read ON product_variants
    FOR SELECT
    USING (
        current_tenant_type() = 'fabricator'
        AND EXISTS (
            SELECT 1 FROM connections c
            WHERE c.supplier_id   = product_variants.tenant_id
              AND c.fabricator_id = current_tenant_id()
              AND c.status        = 'active'
        )
    );

-- ── SLABS ─────────────────────────────────────────────────────────────────────
-- Suppliers see only their own slabs
CREATE POLICY pol_slabs_supplier ON slabs
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- Fabricators see available slabs from connected suppliers only
CREATE POLICY pol_slabs_fabricator_read ON slabs
    FOR SELECT
    USING (
        current_tenant_type() = 'fabricator'
        AND status IN ('available', 'reserved')
        AND is_active = TRUE
        AND EXISTS (
            SELECT 1 FROM connections c
            WHERE c.supplier_id   = slabs.tenant_id
              AND c.fabricator_id = current_tenant_id()
              AND c.status        = 'active'
        )
    );

-- ── SLAB PHOTOS ───────────────────────────────────────────────────────────────
CREATE POLICY pol_slab_photos_supplier ON slab_photos
    FOR ALL
    USING (tenant_id = current_tenant_id());

CREATE POLICY pol_slab_photos_fabricator_read ON slab_photos
    FOR SELECT
    USING (
        current_tenant_type() = 'fabricator'
        AND EXISTS (
            SELECT 1 FROM slabs s
            JOIN connections c ON c.supplier_id = s.tenant_id
                              AND c.fabricator_id = current_tenant_id()
                              AND c.status = 'active'
            WHERE s.id = slab_photos.slab_id
        )
    );

-- ── SLAB BUNDLES ──────────────────────────────────────────────────────────────
CREATE POLICY pol_slab_bundles_own ON slab_bundles
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- ── CONNECTIONS ───────────────────────────────────────────────────────────────
-- Both parties can see connections they are part of
CREATE POLICY pol_connections_own ON connections
    FOR ALL
    USING (
        fabricator_id = current_tenant_id()
        OR supplier_id = current_tenant_id()
    );

-- ── JOBS ──────────────────────────────────────────────────────────────────────
-- Only the fabricator tenant that created the job can see it
CREATE POLICY pol_jobs_own ON jobs
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- ── PURCHASE ORDERS ───────────────────────────────────────────────────────────
-- Both fabricator and supplier parties can see the PO
CREATE POLICY pol_purchase_orders_parties ON purchase_orders
    FOR ALL
    USING (
        fabricator_id = current_tenant_id()
        OR supplier_id = current_tenant_id()
    );

-- ── PO LINE ITEMS ─────────────────────────────────────────────────────────────
-- Line items are visible to both parties of the PO
CREATE POLICY pol_po_line_items_parties ON po_line_items
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM purchase_orders po
            WHERE po.id = po_line_items.po_id
              AND (po.fabricator_id = current_tenant_id()
                   OR po.supplier_id = current_tenant_id())
        )
    );

-- ── NOTIFICATIONS ─────────────────────────────────────────────────────────────
CREATE POLICY pol_notifications_own ON notifications
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- ── SYNC EVENTS ───────────────────────────────────────────────────────────────
CREATE POLICY pol_sync_events_source ON sync_events
    FOR ALL
    USING (
        source_tenant = current_tenant_id()
        OR target_tenant = current_tenant_id()
        OR target_tenant IS NULL  -- broadcast events
    );

-- ── PRICE LISTS ───────────────────────────────────────────────────────────────
CREATE POLICY pol_price_lists_own ON price_lists
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- ── WEBHOOK ENDPOINTS ─────────────────────────────────────────────────────────
CREATE POLICY pol_webhook_endpoints_own ON webhook_endpoints
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- ── SAVED SEARCHES ────────────────────────────────────────────────────────────
CREATE POLICY pol_saved_searches_own ON saved_searches
    FOR ALL
    USING (tenant_id = current_tenant_id());

-- ── INTEGRATIONS ──────────────────────────────────────────────────────────────
CREATE POLICY pol_integrations_own ON integrations
    FOR ALL
    USING (tenant_id = current_tenant_id());