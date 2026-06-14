-- ============================================================
-- V011: Performance indexes
-- Naming: idx_{table}_{columns}[_{filter_description}]
-- ============================================================

-- ── Tenants and users ─────────────────────────────────────────────────────────
CREATE INDEX idx_users_tenant_id        ON users(tenant_id);
CREATE INDEX idx_users_clerk_id         ON users(clerk_user_id);

-- ── Slabs — the most query-intensive table ────────────────────────────────────

-- PRIMARY catalog browse: fabricator searches available slabs across connected suppliers
-- This is the hottest query in the system — runs on every catalog page load
CREATE INDEX idx_slabs_catalog_available ON slabs(tenant_id, material_type, color_family, thickness_cm, finish)
    WHERE status = 'available' AND is_active = TRUE;

COMMENT ON INDEX idx_slabs_catalog_available
    IS 'Filtered index for fabricator catalog browse. Covers the most common WHERE clause.';

-- Full-text search on the generated tsvector column
CREATE INDEX idx_slabs_search_vector ON slabs USING GIN (search_vector);

COMMENT ON INDEX idx_slabs_search_vector
    IS 'GIN index on generated tsvector column. Used by: WHERE search_vector @@ plainto_tsquery(''english'', @q)';

-- Supplier views their own inventory
CREATE INDEX idx_slabs_tenant_status    ON slabs(tenant_id, status, updated_at DESC)
    WHERE is_active = TRUE;

-- Slab status change — used when releasing reserved slabs after PO cancel/decline
CREATE INDEX idx_slabs_status           ON slabs(status, tenant_id)
    WHERE is_active = TRUE;

-- Bundle view — list all slabs in a quarry lot
CREATE INDEX idx_slabs_bundle           ON slabs(bundle_id, status)
    WHERE bundle_id IS NOT NULL AND is_active = TRUE;

-- Reserved PO linkage — find slabs reserved for a specific PO
CREATE INDEX idx_slabs_reserved_for_po  ON slabs(reserved_for_po)
    WHERE reserved_for_po IS NOT NULL;

-- Price range filter — combined with material type for catalog filtering
CREATE INDEX idx_slabs_price            ON slabs(tenant_id, price_override)
    WHERE status = 'available' AND is_active = TRUE AND price_override IS NOT NULL;

-- Trigram index for ILIKE-based search on internal_ref and material_name
CREATE INDEX idx_slabs_internal_ref_trgm ON slabs USING GIN (internal_ref gin_trgm_ops);
CREATE INDEX idx_slabs_material_name_trgm ON slabs USING GIN (material_name gin_trgm_ops);

-- ── Product variants ──────────────────────────────────────────────────────────
CREATE INDEX idx_variants_product_id    ON product_variants(product_id);
CREATE INDEX idx_variants_tenant_id     ON product_variants(tenant_id);
CREATE INDEX idx_variants_sku_search    ON product_variants USING GIN (sku gin_trgm_ops);
CREATE INDEX idx_products_tenant_active ON products(tenant_id, category_code)
    WHERE is_active = TRUE;
CREATE INDEX idx_products_fts_search    ON products USING GIN (
    to_tsvector('english', COALESCE(name,'') || ' ' || COALESCE(brand,'') || ' ' || COALESCE(short_description,''))
);

-- ── Connections — runs on EVERY catalog API call (access gate) ────────────────

-- The single most critical index in the system.
-- Every slab catalog query does: EXISTS(SELECT 1 FROM connections WHERE fabricator_id=? AND supplier_id=? AND status='active')
CREATE INDEX idx_connections_active_pair ON connections(fabricator_id, supplier_id)
    WHERE status = 'active';

COMMENT ON INDEX idx_connections_active_pair
    IS 'CRITICAL: Covers the catalog access gate EXISTS() subquery. Must stay fast as this runs on every catalog request.';

-- Supplier sees all their connections
CREATE INDEX idx_connections_supplier    ON connections(supplier_id, status, requested_at DESC);

-- Fabricator sees all their connections
CREATE INDEX idx_connections_fabricator  ON connections(fabricator_id, status, requested_at DESC);

-- Pending requests inbox for supplier
CREATE INDEX idx_connections_pending     ON connections(supplier_id, requested_at ASC)
    WHERE status = 'pending';

-- ── Purchase orders ───────────────────────────────────────────────────────────

-- Supplier PO inbox — sent POs awaiting acknowledgment
CREATE INDEX idx_po_supplier_sent       ON purchase_orders(supplier_id, sent_at DESC)
    WHERE status = 'sent';

COMMENT ON INDEX idx_po_supplier_sent
    IS 'Supplier inbox: all POs in sent status ordered by arrival time.';

-- Supplier all POs (inbox with all statuses)
CREATE INDEX idx_po_supplier            ON purchase_orders(supplier_id, status, created_at DESC);

-- Fabricator my orders list
CREATE INDEX idx_po_fabricator          ON purchase_orders(fabricator_id, status, created_at DESC);

-- Job-linked POs
CREATE INDEX idx_po_job_id             ON purchase_orders(job_id)
    WHERE job_id IS NOT NULL;

-- Unacked PO alert: POs in sent status for > 24 hours (checked by background worker)
CREATE INDEX idx_po_unacked_alert       ON purchase_orders(sent_at)
    WHERE status = 'sent' AND sent_at IS NOT NULL;

-- ── PO line items ─────────────────────────────────────────────────────────────
CREATE INDEX idx_poli_po_id            ON po_line_items(po_id, created_at ASC);
CREATE INDEX idx_poli_slab_id          ON po_line_items(slab_id)
    WHERE slab_id IS NOT NULL;
CREATE INDEX idx_poli_variant_id       ON po_line_items(variant_id);

-- ── Notifications ─────────────────────────────────────────────────────────────

-- Notification inbox — unread count badge query
CREATE INDEX idx_notifications_unread   ON notifications(tenant_id, is_read, created_at DESC)
    WHERE is_read = FALSE;

-- Notification inbox list
CREATE INDEX idx_notifications_tenant   ON notifications(tenant_id, created_at DESC);

-- ── Sync events — background worker query ─────────────────────────────────────

-- SyncEventWorker polls this every 15 seconds
CREATE INDEX idx_sync_events_pending    ON sync_events(status, next_retry_at)
    WHERE status IN ('pending', 'failed');

COMMENT ON INDEX idx_sync_events_pending
    IS 'Background worker index. Covers: WHERE status IN (''pending'',''failed'') AND next_retry_at <= NOW()';

-- ── Jobs ──────────────────────────────────────────────────────────────────────
CREATE INDEX idx_jobs_tenant_status     ON jobs(tenant_id, status, fabrication_date);

-- ── Slab photos ───────────────────────────────────────────────────────────────
CREATE INDEX idx_slab_photos_slab_id    ON slab_photos(slab_id, sort_order ASC);

-- ── Price lists ───────────────────────────────────────────────────────────────
CREATE INDEX idx_price_list_items_variant ON price_list_items(variant_id);
CREATE INDEX idx_connection_price_lists   ON connection_price_lists(connection_id);

-- ── Warehouse ──────────────────────────────────────────────────────────────────
CREATE INDEX idx_warehouses_tenant      ON warehouses(tenant_id)
    WHERE is_active = TRUE;