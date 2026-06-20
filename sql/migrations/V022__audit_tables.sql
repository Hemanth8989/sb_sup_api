-- V022: Audit tables for slab transfers and events; capacity fields on warehouses

-- Slab transfer log (append-only)
CREATE TABLE IF NOT EXISTS slab_transfer_log (
    id               UUID        NOT NULL DEFAULT gen_random_uuid(),
    tenant_id        UUID        NOT NULL,
    slab_id          UUID        NOT NULL,
    from_warehouse   UUID,
    to_warehouse     UUID        NOT NULL,
    from_rack        VARCHAR(50),
    to_rack          VARCHAR(50),
    transferred_by   UUID,
    notes            TEXT,
    transferred_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_slab_transfer_log PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_stl_slab      ON slab_transfer_log (slab_id);
CREATE INDEX IF NOT EXISTS ix_stl_tenant_wh ON slab_transfer_log (tenant_id, to_warehouse);
CREATE INDEX IF NOT EXISTS ix_stl_from_wh   ON slab_transfer_log (from_warehouse) WHERE from_warehouse IS NOT NULL;

-- Slab events (append-only; tracks status/price/location changes)
CREATE TABLE IF NOT EXISTS slab_events (
    id           UUID        NOT NULL DEFAULT gen_random_uuid(),
    tenant_id    UUID        NOT NULL,
    slab_id      UUID        NOT NULL,
    event_type   VARCHAR(30) NOT NULL, -- status_change | price_change | location_change
    old_value    TEXT,
    new_value    TEXT,
    notes        TEXT,
    created_by   UUID,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_slab_events PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_se_slab   ON slab_events (slab_id);
CREATE INDEX IF NOT EXISTS ix_se_tenant ON slab_events (tenant_id, created_at DESC);

-- Warehouse capacity and notes
ALTER TABLE warehouses
    ADD COLUMN IF NOT EXISTS capacity_sqft NUMERIC(10, 2),
    ADD COLUMN IF NOT EXISTS notes         TEXT;
