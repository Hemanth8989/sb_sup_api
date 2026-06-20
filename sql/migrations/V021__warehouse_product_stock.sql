-- Warehouse-level stock tracking for products & supplies (non-slab variants).
-- Slabs are individually tracked via slabs.warehouse_id.
-- This table tracks quantity of each product_variant at each warehouse.

CREATE TABLE warehouse_product_stock (
    id            UUID        NOT NULL DEFAULT gen_random_uuid(),
    tenant_id     UUID        NOT NULL,
    warehouse_id  UUID        NOT NULL,
    variant_id    UUID        NOT NULL,
    qty_on_hand   INTEGER     NOT NULL DEFAULT 0,
    qty_reserved  INTEGER     NOT NULL DEFAULT 0,
    rack_location VARCHAR(50),
    reorder_point INTEGER,
    reorder_qty   INTEGER,
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_wps      PRIMARY KEY (id),
    CONSTRAINT fk_wps_ten  FOREIGN KEY (tenant_id)   REFERENCES tenants(id)          ON DELETE CASCADE,
    CONSTRAINT fk_wps_wh   FOREIGN KEY (warehouse_id) REFERENCES warehouses(id)       ON DELETE CASCADE,
    CONSTRAINT fk_wps_var  FOREIGN KEY (variant_id)  REFERENCES product_variants(id)  ON DELETE CASCADE,
    CONSTRAINT uq_wps      UNIQUE (tenant_id, warehouse_id, variant_id),
    CONSTRAINT chk_wps_qty CHECK (
        qty_on_hand  >= 0 AND
        qty_reserved >= 0 AND
        qty_reserved <= qty_on_hand
    )
);

CREATE INDEX ix_wps_warehouse ON warehouse_product_stock (warehouse_id, tenant_id);
CREATE INDEX ix_wps_variant   ON warehouse_product_stock (variant_id,   tenant_id);

-- Audit log for every product stock movement in a warehouse.
CREATE TABLE stock_movements (
    id             UUID        NOT NULL DEFAULT gen_random_uuid(),
    tenant_id      UUID        NOT NULL,
    variant_id     UUID        NOT NULL,
    from_warehouse UUID,
    to_warehouse   UUID,
    qty            INTEGER     NOT NULL,
    movement_type  VARCHAR(30) NOT NULL, -- receive | transfer_out | transfer_in | adjustment | po_fulfil
    reference_id   UUID,                 -- PO id or transfer batch id
    notes          TEXT,
    created_by     UUID,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_sm       PRIMARY KEY (id),
    CONSTRAINT fk_sm_ten   FOREIGN KEY (tenant_id)  REFERENCES tenants(id)         ON DELETE CASCADE,
    CONSTRAINT fk_sm_var   FOREIGN KEY (variant_id) REFERENCES product_variants(id) ON DELETE CASCADE
);

CREATE INDEX ix_sm_warehouse ON stock_movements (to_warehouse,   tenant_id, created_at DESC);
CREATE INDEX ix_sm_from_wh   ON stock_movements (from_warehouse, tenant_id, created_at DESC);
CREATE INDEX ix_sm_variant   ON stock_movements (variant_id,     tenant_id, created_at DESC);
