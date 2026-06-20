-- ============================================================
-- truncate_all.sql
-- Wipes all data while preserving schema, sequences, and RLS.
-- Run as superuser (same role used for migrations).
-- TRUNCATE ... CASCADE handles FK ordering automatically.
-- ============================================================

SET row_security = OFF;

TRUNCATE TABLE
    -- audit / history (leaf nodes first, but CASCADE handles it anyway)
    po_status_history,
    connection_history,
    slab_price_history,

    -- transactional
    po_line_items,
    purchase_orders,
    job_slabs,
    jobs,

    -- catalog / inventory
    slab_photos,
    slabs,
    bundle_slabs,
    slab_bundles,
    price_list_items,
    connection_price_lists,
    price_lists,
    connections,
    product_variants,
    products,
    product_categories,

    -- profiles / locations
    warehouses,
    addresses,
    fabricator_profiles,
    supplier_profiles,

    -- integration / notifications
    webhook_deliveries,
    webhook_endpoints,
    notifications,
    sync_events,
    integration_configs,

    -- identity
    users,
    tenants

CASCADE;

-- Reset sequences so IDs start fresh (optional — UUIDs don't need this,
-- but any SERIAL / BIGSERIAL columns (e.g. slab_price_history.id) will reset)
-- Uncomment if you use integer PKs anywhere:
-- SELECT setval(c.oid, 1, false)
-- FROM   pg_class c
-- JOIN   pg_namespace n ON n.oid = c.relnamespace
-- WHERE  c.relkind = 'S' AND n.nspname = 'public';

RESET row_security;

-- Confirm
SELECT schemaname, tablename, n_live_tup
FROM   pg_stat_user_tables
WHERE  schemaname = 'public'
ORDER  BY tablename;
