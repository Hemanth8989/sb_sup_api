-- ============================================================
-- V006: Connections and price lists
-- Connections control fabricator access to supplier catalogs
-- ============================================================

CREATE TABLE connections (
    id                  UUID            NOT NULL DEFAULT gen_random_uuid(),
    fabricator_id       UUID            NOT NULL,
    supplier_id         UUID            NOT NULL,
    status              VARCHAR(20)     NOT NULL DEFAULT 'pending',
    pricing_tier        VARCHAR(20)     NOT NULL DEFAULT 'standard',
    initiated_by        UUID,
    approved_by         UUID,
    request_message     VARCHAR(500),
    decline_reason      VARCHAR(300),
    fabricator_notes    TEXT,
    requested_at        TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    connected_at        TIMESTAMPTZ,
    suspended_at        TIMESTAMPTZ,
    terminated_at       TIMESTAMPTZ,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_connections           PRIMARY KEY (id),
    CONSTRAINT fk_connections_fab       FOREIGN KEY (fabricator_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_connections_sup       FOREIGN KEY (supplier_id)   REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_connections_pair      UNIQUE (fabricator_id, supplier_id),
    CONSTRAINT ck_connections_status    CHECK (status IN ('pending','active','suspended','declined','terminated')),
    CONSTRAINT ck_connections_tier      CHECK (pricing_tier IN ('standard','preferred','vip')),
    CONSTRAINT ck_connections_no_self   CHECK (fabricator_id <> supplier_id)
);

COMMENT ON TABLE  connections               IS 'Bilateral access control between fabricator and supplier. One row per pair.';
COMMENT ON COLUMN connections.status        IS 'pending → active (catalog access granted). suspended/terminated removes access.';
COMMENT ON COLUMN connections.pricing_tier  IS 'Determines which price list applies for this fabricator.';
COMMENT ON COLUMN connections.fabricator_notes IS 'Private notes visible only to the supplier about this fabricator.';

-- ── Connection history audit ──────────────────────────────────────────────────
CREATE TABLE connection_history (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    connection_id   UUID            NOT NULL,
    from_status     VARCHAR(20),
    to_status       VARCHAR(20)     NOT NULL,
    changed_by      UUID,
    reason          VARCHAR(300),
    changed_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_connection_history        PRIMARY KEY (id),
    CONSTRAINT fk_conn_history_connection   FOREIGN KEY (connection_id) REFERENCES connections(id) ON DELETE CASCADE
);

-- ── Price lists ───────────────────────────────────────────────────────────────
CREATE TABLE price_lists (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       UUID            NOT NULL,
    name            VARCHAR(200)    NOT NULL,
    tier            VARCHAR(20)     NOT NULL DEFAULT 'standard',
    currency        VARCHAR(3)      NOT NULL DEFAULT 'USD',
    valid_from      DATE,
    valid_to        DATE,
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_price_lists           PRIMARY KEY (id),
    CONSTRAINT fk_price_lists_tenant    FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT ck_price_lists_tier      CHECK (tier IN ('standard','preferred','vip'))
);

CREATE TABLE price_list_items (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    price_list_id   UUID            NOT NULL,
    variant_id      UUID            NOT NULL,
    unit_price      NUMERIC(12,2)   NOT NULL,
    currency        VARCHAR(3)      NOT NULL DEFAULT 'USD',
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_price_list_items          PRIMARY KEY (id),
    CONSTRAINT fk_pli_price_list            FOREIGN KEY (price_list_id) REFERENCES price_lists(id) ON DELETE CASCADE,
    CONSTRAINT fk_pli_variant               FOREIGN KEY (variant_id)    REFERENCES product_variants(id),
    CONSTRAINT uq_pli_list_variant          UNIQUE (price_list_id, variant_id),
    CONSTRAINT ck_pli_price                 CHECK (unit_price >= 0)
);

CREATE TABLE connection_price_lists (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    connection_id   UUID            NOT NULL,
    price_list_id   UUID            NOT NULL,
    assigned_at     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    assigned_by     UUID,

    CONSTRAINT pk_connection_price_lists        PRIMARY KEY (id),
    CONSTRAINT fk_cpl_connection                FOREIGN KEY (connection_id)  REFERENCES connections(id)  ON DELETE CASCADE,
    CONSTRAINT fk_cpl_price_list                FOREIGN KEY (price_list_id)  REFERENCES price_lists(id)
);