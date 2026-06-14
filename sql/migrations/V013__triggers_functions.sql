-- ============================================================
-- V013: Trigger functions and triggers
-- All trigger functions use PL/pgSQL
-- ============================================================

-- ── Auto-update updated_at column ────────────────────────────────────────────
CREATE OR REPLACE FUNCTION trg_set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := NOW();
    RETURN NEW;
END;
$$;

COMMENT ON FUNCTION trg_set_updated_at()
    IS 'Sets updated_at = NOW() on every UPDATE. Attached to all tables with updated_at column.';

-- Attach to all tables that have an updated_at column
CREATE TRIGGER trg_tenants_updated_at
    BEFORE UPDATE ON tenants
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_supplier_profiles_updated_at
    BEFORE UPDATE ON supplier_profiles
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_fabricator_profiles_updated_at
    BEFORE UPDATE ON fabricator_profiles
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_warehouses_updated_at
    BEFORE UPDATE ON warehouses
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_products_updated_at
    BEFORE UPDATE ON products
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_product_variants_updated_at
    BEFORE UPDATE ON product_variants
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_slabs_updated_at
    BEFORE UPDATE ON slabs
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_slab_bundles_updated_at
    BEFORE UPDATE ON slab_bundles
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_connections_updated_at
    BEFORE UPDATE ON connections
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_jobs_updated_at
    BEFORE UPDATE ON jobs
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_purchase_orders_updated_at
    BEFORE UPDATE ON purchase_orders
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_po_line_items_updated_at
    BEFORE UPDATE ON po_line_items
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_sync_events_updated_at
    BEFORE UPDATE ON sync_events
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_price_lists_updated_at
    BEFORE UPDATE ON price_lists
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_webhook_endpoints_updated_at
    BEFORE UPDATE ON webhook_endpoints
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

CREATE TRIGGER trg_integrations_updated_at
    BEFORE UPDATE ON integrations
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

-- ── Slab photo count limit (max 12 per slab) ──────────────────────────────────
CREATE OR REPLACE FUNCTION trg_slab_photo_limit()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_photo_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_photo_count
    FROM slab_photos
    WHERE slab_id = NEW.slab_id;

    IF v_photo_count >= 12 THEN
        RAISE EXCEPTION 'Slab % already has 12 photos. Delete one before adding more.', NEW.slab_id
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_slab_photo_limit
    BEFORE INSERT ON slab_photos
    FOR EACH ROW EXECUTE FUNCTION trg_slab_photo_limit();

-- ── Slab bundle count maintenance ─────────────────────────────────────────────
CREATE OR REPLACE FUNCTION trg_slab_bundle_counts()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    -- Handle INSERT
    IF TG_OP = 'INSERT' AND NEW.bundle_id IS NOT NULL THEN
        UPDATE slab_bundles SET
            slab_count   = slab_count + 1,
            active_count = active_count + CASE WHEN NEW.is_active THEN 1 ELSE 0 END,
            updated_at   = NOW()
        WHERE id = NEW.bundle_id;
    END IF;

    -- Handle DELETE
    IF TG_OP = 'DELETE' AND OLD.bundle_id IS NOT NULL THEN
        UPDATE slab_bundles SET
            slab_count   = GREATEST(slab_count - 1, 0),
            active_count = GREATEST(active_count - CASE WHEN OLD.is_active THEN 1 ELSE 0 END, 0),
            updated_at   = NOW()
        WHERE id = OLD.bundle_id;
    END IF;

    -- Handle UPDATE (bundle changed or is_active changed)
    IF TG_OP = 'UPDATE' THEN
        -- Slab moved from one bundle to another
        IF OLD.bundle_id IS DISTINCT FROM NEW.bundle_id THEN
            IF OLD.bundle_id IS NOT NULL THEN
                UPDATE slab_bundles SET
                    slab_count   = GREATEST(slab_count - 1, 0),
                    active_count = GREATEST(active_count - CASE WHEN OLD.is_active THEN 1 ELSE 0 END, 0),
                    updated_at   = NOW()
                WHERE id = OLD.bundle_id;
            END IF;
            IF NEW.bundle_id IS NOT NULL THEN
                UPDATE slab_bundles SET
                    slab_count   = slab_count + 1,
                    active_count = active_count + CASE WHEN NEW.is_active THEN 1 ELSE 0 END,
                    updated_at   = NOW()
                WHERE id = NEW.bundle_id;
            END IF;
        -- is_active changed within same bundle
        ELSIF OLD.bundle_id = NEW.bundle_id AND NEW.bundle_id IS NOT NULL
              AND OLD.is_active IS DISTINCT FROM NEW.is_active THEN
            UPDATE slab_bundles SET
                active_count = active_count + CASE WHEN NEW.is_active THEN 1 ELSE -1 END,
                updated_at   = NOW()
            WHERE id = NEW.bundle_id;
        END IF;
    END IF;

    RETURN COALESCE(NEW, OLD);
END;
$$;

CREATE TRIGGER trg_slab_bundle_counts
    AFTER INSERT OR UPDATE OR DELETE ON slabs
    FOR EACH ROW EXECUTE FUNCTION trg_slab_bundle_counts();

-- ── Enforce single primary warehouse per tenant ───────────────────────────────
CREATE OR REPLACE FUNCTION trg_warehouse_single_primary()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.is_primary = TRUE THEN
        UPDATE warehouses
        SET is_primary = FALSE, updated_at = NOW()
        WHERE tenant_id = NEW.tenant_id
          AND id        <> NEW.id
          AND is_primary = TRUE;
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_warehouse_single_primary
    BEFORE INSERT OR UPDATE ON warehouses
    FOR EACH ROW WHEN (NEW.is_primary = TRUE)
    EXECUTE FUNCTION trg_warehouse_single_primary();

-- ── PO status history audit ───────────────────────────────────────────────────
CREATE OR REPLACE FUNCTION trg_po_status_history()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF OLD.status IS DISTINCT FROM NEW.status THEN
        INSERT INTO po_status_history (po_id, from_status, to_status, changed_at)
        VALUES (NEW.id, OLD.status, NEW.status, NOW());
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_po_status_history
    AFTER UPDATE ON purchase_orders
    FOR EACH ROW EXECUTE FUNCTION trg_po_status_history();

-- ── Connection status history audit ───────────────────────────────────────────
CREATE OR REPLACE FUNCTION trg_connection_status_history()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF OLD.status IS DISTINCT FROM NEW.status THEN
        INSERT INTO connection_history (connection_id, from_status, to_status, changed_at)
        VALUES (NEW.id, OLD.status, NEW.status, NOW());
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_connection_status_history
    AFTER UPDATE ON connections
    FOR EACH ROW EXECUTE FUNCTION trg_connection_status_history();

-- ── PO total recalculation trigger ───────────────────────────────────────────
CREATE OR REPLACE FUNCTION trg_recalculate_po_totals()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE purchase_orders SET
        subtotal     = (
            SELECT COALESCE(SUM(line_total), 0)
            FROM   po_line_items
            WHERE  po_id  = COALESCE(NEW.po_id, OLD.po_id)
              AND  status <> 'declined'
        ),
        total_amount = subtotal + tax_amount + shipping_amount - discount_amount,
        updated_at   = NOW()
    WHERE id = COALESCE(NEW.po_id, OLD.po_id);

    RETURN COALESCE(NEW, OLD);
END;
$$;

CREATE TRIGGER trg_po_line_item_totals
    AFTER INSERT OR UPDATE OF line_total, status OR DELETE ON po_line_items
    FOR EACH ROW EXECUTE FUNCTION trg_recalculate_po_totals();

-- ── Slab status changed — update status_changed timestamp ─────────────────────
CREATE OR REPLACE FUNCTION trg_slab_status_changed()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF OLD.status IS DISTINCT FROM NEW.status THEN
        NEW.status_changed := NOW();
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_slab_status_changed
    BEFORE UPDATE ON slabs
    FOR EACH ROW EXECUTE FUNCTION trg_slab_status_changed();

-- ── Supplier stats refresh function (called by background worker) ─────────────
CREATE OR REPLACE FUNCTION refresh_supplier_stats(p_tenant_id UUID)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE supplier_profiles SET
        avg_lead_days = (
            SELECT AVG(
                EXTRACT(EPOCH FROM (po.received_at - po.sent_at)) / 86400.0
            )
            FROM   purchase_orders po
            WHERE  po.supplier_id = p_tenant_id
              AND  po.status      IN ('received', 'closed')
              AND  po.sent_at     IS NOT NULL
              AND  po.received_at IS NOT NULL
              AND  po.created_at  >= NOW() - INTERVAL '90 days'
        ),
        fulfillment_rate = (
            SELECT CASE
                WHEN COUNT(*) = 0 THEN NULL
                ELSE ROUND(
                    COUNT(*) FILTER (WHERE status IN ('received','closed'))::NUMERIC
                    / COUNT(*)::NUMERIC * 100, 2
                )
            END
            FROM   purchase_orders
            WHERE  supplier_id = p_tenant_id
              AND  status      NOT IN ('draft', 'cancelled')
              AND  created_at  >= NOW() - INTERVAL '90 days'
        ),
        avg_response_hrs = (
            SELECT AVG(
                EXTRACT(EPOCH FROM (acked_at - sent_at)) / 3600.0
            )
            FROM   purchase_orders
            WHERE  supplier_id = p_tenant_id
              AND  acked_at    IS NOT NULL
              AND  sent_at     IS NOT NULL
              AND  created_at  >= NOW() - INTERVAL '90 days'
        ),
        total_slabs_sold = (
            SELECT COUNT(*)
            FROM   slabs
            WHERE  tenant_id = p_tenant_id
              AND  status    = 'sold'
        ),
        warehouse_count = (
            SELECT COUNT(*)
            FROM   warehouses
            WHERE  tenant_id = p_tenant_id
              AND  is_active  = TRUE
        ),
        updated_at = NOW()
    WHERE tenant_id = p_tenant_id;
END;
$$;

COMMENT ON FUNCTION refresh_supplier_stats(UUID)
    IS 'Recalculates all supplier performance metrics for the past 90 days. Called by SyncEventWorker.';

-- ── Helper function: generate PO number ───────────────────────────────────────
CREATE OR REPLACE FUNCTION generate_po_number()
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE
    v_seq   BIGINT;
    v_year  TEXT;
BEGIN
    SELECT NEXTVAL('seq_po_number') INTO v_seq;
    v_year := EXTRACT(YEAR FROM NOW())::TEXT;
    RETURN 'PO-' || v_year || '-' || LPAD(v_seq::TEXT, 6, '0');
END;
$$;

COMMENT ON FUNCTION generate_po_number()
    IS 'Generates the next PO number in format PO-{YYYY}-{000001}. Calls seq_po_number.';