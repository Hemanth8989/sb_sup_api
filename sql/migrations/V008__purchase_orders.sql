-- ============================================================
-- V008: Purchase orders and line items
-- PO lifecycle: draft → sent → acknowledged/partially_acked →
--               countered → confirmed → shipped → received → closed
-- Branch paths: cancelled, disputed
-- ============================================================

CREATE TABLE purchase_orders (
    id                  UUID            NOT NULL DEFAULT gen_random_uuid(),
    po_number           VARCHAR(30)     NOT NULL,
    fabricator_id       UUID            NOT NULL,
    supplier_id         UUID            NOT NULL,
    job_id              UUID,

    -- Lifecycle
    status              VARCHAR(20)     NOT NULL DEFAULT 'draft',
    status_changed      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    -- Financials
    subtotal            NUMERIC(14,2)   NOT NULL DEFAULT 0,
    discount_amount     NUMERIC(14,2)   NOT NULL DEFAULT 0,
    tax_amount          NUMERIC(14,2)   NOT NULL DEFAULT 0,
    shipping_amount     NUMERIC(14,2)   NOT NULL DEFAULT 0,
    total_amount        NUMERIC(14,2)   NOT NULL DEFAULT 0,
    currency            VARCHAR(3)      NOT NULL DEFAULT 'USD',

    -- Delivery
    delivery_address_id UUID,
    requested_delivery  DATE,
    confirmed_delivery  DATE,

    -- Shipment
    tracking_number     VARCHAR(100),
    carrier             VARCHAR(100),

    -- Counter offer — stored as JSONB for flexibility
    counter_offer       JSONB,

    -- Timestamps
    sent_at             TIMESTAMPTZ,
    acked_at            TIMESTAMPTZ,
    shipped_at          TIMESTAMPTZ,
    received_at         TIMESTAMPTZ,

    -- Notes
    fabricator_notes    VARCHAR(1000),
    supplier_notes      VARCHAR(1000),
    internal_ref        VARCHAR(200),

    -- Integration
    synced_to_moraware  BOOLEAN         NOT NULL DEFAULT FALSE,
    moraware_po_id      VARCHAR(100),

    -- Audit
    created_by          UUID,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_purchase_orders       PRIMARY KEY (id),
    CONSTRAINT fk_po_fabricator         FOREIGN KEY (fabricator_id)         REFERENCES tenants(id)   ON DELETE RESTRICT,
    CONSTRAINT fk_po_supplier           FOREIGN KEY (supplier_id)           REFERENCES tenants(id)   ON DELETE RESTRICT,
    CONSTRAINT fk_po_job                FOREIGN KEY (job_id)                REFERENCES jobs(id)      ON DELETE SET NULL,
    CONSTRAINT fk_po_delivery_address   FOREIGN KEY (delivery_address_id)   REFERENCES addresses(id) ON DELETE SET NULL,
    CONSTRAINT uq_po_number             UNIQUE (po_number),
    CONSTRAINT ck_po_status             CHECK (status IN (
                                            'draft','sent','acknowledged','partially_acked',
                                            'countered','confirmed','shipped','received',
                                            'closed','disputed','cancelled')),
    CONSTRAINT ck_po_currency           CHECK (currency ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_po_subtotal           CHECK (subtotal >= 0),
    CONSTRAINT ck_po_total              CHECK (total_amount >= 0),
    CONSTRAINT ck_po_no_self            CHECK (fabricator_id <> supplier_id)
);

COMMENT ON TABLE  purchase_orders               IS 'Purchase orders sent from fabricators to suppliers.';
COMMENT ON COLUMN purchase_orders.po_number     IS 'Human-readable identifier. Format: PO-{YYYY}-{000001}.';
COMMENT ON COLUMN purchase_orders.counter_offer IS 'JSONB: {"proposedDelivery":"2026-07-15","supplierNote":"...","lineChanges":[...]}';
COMMENT ON COLUMN purchase_orders.status        IS '11-state lifecycle. draft is local-only; sent triggers supplier notification.';

-- ── PO status history ─────────────────────────────────────────────────────────
CREATE TABLE po_status_history (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    po_id           UUID            NOT NULL,
    from_status     VARCHAR(20),
    to_status       VARCHAR(20)     NOT NULL,
    changed_by      UUID,
    note            TEXT,
    changed_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_po_status_history     PRIMARY KEY (id),
    CONSTRAINT fk_po_history_po         FOREIGN KEY (po_id) REFERENCES purchase_orders(id) ON DELETE CASCADE
);

-- ── PO line items ─────────────────────────────────────────────────────────────
CREATE TABLE po_line_items (
    id                  UUID            NOT NULL DEFAULT gen_random_uuid(),
    po_id               UUID            NOT NULL,
    variant_id          UUID            NOT NULL,
    slab_id             UUID,               -- NULL for quantity-based product lines

    -- Snapshot of the item at ordering time — immutable JSON
    item_snapshot       JSONB           NOT NULL DEFAULT '{}'::JSONB,

    -- Quantity and pricing
    quantity            NUMERIC(10,3)   NOT NULL,
    unit_of_measure     VARCHAR(20)     NOT NULL DEFAULT 'each',
    unit_price          NUMERIC(12,2)   NOT NULL,
    line_total          NUMERIC(14,2)   NOT NULL,
    currency            VARCHAR(3)      NOT NULL DEFAULT 'USD',

    -- Line status
    status              VARCHAR(20)     NOT NULL DEFAULT 'pending',
    decline_reason      VARCHAR(500),

    -- Substitution
    substitute_variant  UUID,
    substitute_slab     UUID,

    -- Counter price
    counter_price       NUMERIC(12,2),
    counter_note        VARCHAR(500),

    -- Receipt
    qty_received        NUMERIC(10,3),
    received_condition  VARCHAR(20),
    discrepancy_note    TEXT,

    -- Audit
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_po_line_items             PRIMARY KEY (id),
    CONSTRAINT fk_poli_po                   FOREIGN KEY (po_id)     REFERENCES purchase_orders(id) ON DELETE CASCADE,
    CONSTRAINT fk_poli_variant              FOREIGN KEY (variant_id) REFERENCES product_variants(id),
    CONSTRAINT fk_poli_slab                 FOREIGN KEY (slab_id)   REFERENCES slabs(id)           ON DELETE SET NULL,
    CONSTRAINT ck_poli_status               CHECK (status IN ('pending','confirmed','declined','substituted','received')),
    CONSTRAINT ck_poli_quantity             CHECK (quantity > 0),
    CONSTRAINT ck_poli_unit_price           CHECK (unit_price >= 0),
    CONSTRAINT ck_poli_line_total           CHECK (line_total >= 0),
    CONSTRAINT ck_poli_condition            CHECK (received_condition IN (
                                                'perfect','minor_damage','major_damage',
                                                'wrong_item','short_shipped') OR received_condition IS NULL),
    CONSTRAINT ck_poli_uom                  CHECK (unit_of_measure IN (
                                                'each','sqft','sqm','linear_ft','linear_m',
                                                'liter','gallon','kg','lb','box','case','roll','bag','pair','set'))
);

COMMENT ON TABLE  po_line_items             IS 'Individual line items within a purchase order. One row per slab or product quantity.';
COMMENT ON COLUMN po_line_items.item_snapshot IS 'Immutable JSON snapshot of the slab or product at ordering time.';
COMMENT ON COLUMN po_line_items.slab_id     IS 'NULL for quantity-tracked products. Set for slab-type line items.';