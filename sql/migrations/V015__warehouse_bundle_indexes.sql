-- ============================================================
-- V015: Performance indexes for Warehouse and Bundle features
--
-- These indexes cover the query patterns introduced by:
--   WarehouseRepository  — GET all (with stats), GET by id, transfer slabs
--   BundleRepository     — GET all (with search), GET detail (with slabs)
-- ============================================================

-- ── Warehouses ────────────────────────────────────────────────────────────────

-- Warehouse list ordered by primary-first, then name
-- Covers: WHERE tenant_id = ? AND is_active = TRUE ORDER BY is_primary DESC, name ASC
CREATE INDEX idx_warehouses_tenant_list
    ON warehouses(tenant_id, is_primary DESC, name ASC)
    WHERE is_active = TRUE;

COMMENT ON INDEX idx_warehouses_tenant_list
    IS 'WarehouseRepository.GetAllAsync: tenant list ordered primary-first then alphabetical.';

-- ── Slabs — warehouse stats aggregation ──────────────────────────────────────

-- The warehouse stats LEFT JOIN groups slabs by warehouse_id within a tenant.
-- Without this, PostgreSQL does a seqscan on slabs filtered by tenant_id.
-- Covers:
--   SELECT warehouse_id, COUNT(*), COUNT(*) FILTER (WHERE status=?), SUM(net_sqft*...)
--   FROM slabs WHERE tenant_id = ? AND is_active = TRUE
--   GROUP BY warehouse_id
CREATE INDEX idx_slabs_warehouse_stats
    ON slabs(tenant_id, warehouse_id, status)
    WHERE is_active = TRUE;

COMMENT ON INDEX idx_slabs_warehouse_stats
    IS 'WarehouseRepository stats aggregation: groups active slabs by warehouse within a tenant.';

-- Transfer slabs: UPDATE slabs SET warehouse_id=? WHERE tenant_id=? AND id=ANY(?) AND is_active
-- Already covered by PK on id; tenant+active filter benefits from the index above.

-- ── Slab bundles ──────────────────────────────────────────────────────────────

-- Bundle list query: WHERE tenant_id = ? ORDER BY updated_at DESC
-- Also filters on bundle_ref / material_name / quarry_name via ILIKE when search is supplied.
CREATE INDEX idx_slab_bundles_tenant_updated
    ON slab_bundles(tenant_id, updated_at DESC);

COMMENT ON INDEX idx_slab_bundles_tenant_updated
    IS 'BundleRepository.GetAllAsync: ordered list by last-modified, filtered by tenant.';

-- Trigram indexes for ILIKE search on bundle_ref, material_name, quarry_name.
-- Required because PostgreSQL cannot use a B-tree for %search% patterns.
CREATE INDEX idx_slab_bundles_ref_trgm
    ON slab_bundles USING GIN (bundle_ref gin_trgm_ops);

CREATE INDEX idx_slab_bundles_material_trgm
    ON slab_bundles USING GIN (material_name gin_trgm_ops);

CREATE INDEX idx_slab_bundles_quarry_trgm
    ON slab_bundles USING GIN (quarry_name gin_trgm_ops);

COMMENT ON INDEX idx_slab_bundles_ref_trgm
    IS 'BundleRepository search: ILIKE on bundle_ref. Requires pg_trgm (already enabled via V001).';
COMMENT ON INDEX idx_slab_bundles_material_trgm
    IS 'BundleRepository search: ILIKE on material_name.';
COMMENT ON INDEX idx_slab_bundles_quarry_trgm
    IS 'BundleRepository search: ILIKE on quarry_name.';

-- Bundle detail slab list: WHERE bundle_id=? AND is_active=TRUE AND tenant_id=? ORDER BY internal_ref
-- The existing idx_slabs_bundle covers (bundle_id, status) but not tenant_id or ordering.
-- Add a covering index for the detail page query.
CREATE INDEX idx_slabs_bundle_detail
    ON slabs(bundle_id, tenant_id, internal_ref ASC)
    WHERE bundle_id IS NOT NULL AND is_active = TRUE;

COMMENT ON INDEX idx_slabs_bundle_detail
    IS 'BundleRepository.GetByIdAsync slab list: ordered by internal_ref within a bundle+tenant.';

-- Slab photos lateral join: WHERE slab_id=? ORDER BY sort_order ASC LIMIT 1
-- Already covered by idx_slab_photos_slab_id (V011). No additional index needed.
