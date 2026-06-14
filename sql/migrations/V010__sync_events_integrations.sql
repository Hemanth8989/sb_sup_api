-- ============================================================
-- V010: Sync events (reliable delivery queue) and integrations
-- ============================================================

CREATE TABLE sync_events (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    event_type      VARCHAR(100)    NOT NULL,
    source_tenant   UUID            NOT NULL,
    target_tenant   UUID,               -- NULL = broadcast
    entity_type     VARCHAR(50),
    entity_id       UUID,
    payload         JSONB           NOT NULL DEFAULT '{}'::JSONB,
    status          VARCHAR(20)     NOT NULL DEFAULT 'pending',
    attempt_count   SMALLINT        NOT NULL DEFAULT 0,
    max_attempts    SMALLINT        NOT NULL DEFAULT 5,
    last_error      TEXT,
    next_retry_at   TIMESTAMPTZ,
    delivered_at    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_sync_events           PRIMARY KEY (id),
    CONSTRAINT fk_sync_source_tenant    FOREIGN KEY (source_tenant) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT ck_sync_status           CHECK (status IN ('pending','delivered','failed','dead_letter'))
);

COMMENT ON TABLE  sync_events               IS 'Reliable event delivery queue. Polled by SyncEventWorker every 15s.';
COMMENT ON COLUMN sync_events.status        IS 'pending → delivered. pending → failed (retry). failed 5× → dead_letter.';
COMMENT ON COLUMN sync_events.next_retry_at IS 'Exponential backoff: 30s, 2m, 10m, 1h, 4h.';
COMMENT ON COLUMN sync_events.payload       IS 'JSONB event payload delivered to webhook endpoints and SignalR clients.';

-- ── Saved searches ────────────────────────────────────────────────────────────
CREATE TABLE saved_searches (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       UUID            NOT NULL,
    name            VARCHAR(200)    NOT NULL,
    search_params   JSONB           NOT NULL DEFAULT '{}'::JSONB,
    email_alert     BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_saved_searches        PRIMARY KEY (id),
    CONSTRAINT fk_saved_searches_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

COMMENT ON TABLE saved_searches IS 'Fabricator catalog filter presets with optional email alerts on new matching slabs.';

-- ── Integrations ──────────────────────────────────────────────────────────────
CREATE TABLE integrations (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       UUID            NOT NULL,
    type            VARCHAR(50)     NOT NULL,
    is_active       BOOLEAN         NOT NULL DEFAULT FALSE,
    external_shop_id VARCHAR(200),
    webhook_url     VARCHAR(500),
    api_endpoint    VARCHAR(500),
    config          JSONB           NOT NULL DEFAULT '{}'::JSONB,
    last_synced_at  TIMESTAMPTZ,
    last_error      TEXT,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_integrations          PRIMARY KEY (id),
    CONSTRAINT fk_integrations_tenant   FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_integrations          UNIQUE (tenant_id, type),
    CONSTRAINT ck_integrations_type     CHECK (type IN ('moraware','actionflow','slabware','custom'))
);

COMMENT ON TABLE  integrations          IS 'Third-party integration configurations per tenant.';
COMMENT ON COLUMN integrations.config   IS 'JSONB for non-sensitive integration config. Secrets stored in environment variables.';