-- ============================================================
-- V009: Notifications and webhooks
-- ============================================================

CREATE TABLE notifications (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       UUID            NOT NULL,
    user_id         UUID,               -- NULL = broadcast to all tenant users
    type            VARCHAR(50)     NOT NULL,
    title           VARCHAR(200)    NOT NULL,
    body            TEXT            NOT NULL,
    entity_type     VARCHAR(50),
    entity_id       UUID,
    link_url        VARCHAR(500),
    is_read         BOOLEAN         NOT NULL DEFAULT FALSE,
    read_at         TIMESTAMPTZ,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_notifications         PRIMARY KEY (id),
    CONSTRAINT fk_notifications_tenant  FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT ck_notifications_type    CHECK (type IN (
                                            'new_po','po_acknowledged','po_partially_acked',
                                            'po_countered','po_confirmed','po_shipped',
                                            'po_received','po_disputed','po_cancelled',
                                            'connection_requested','connection_approved',
                                            'connection_declined','connection_suspended',
                                            'connection_terminated','price_changed',
                                            'new_stock','low_stock_warning','po_unacked_24h',
                                            'delivery_confirmed','system'))
);

COMMENT ON TABLE  notifications         IS 'In-app notification inbox. Pushed via SignalR on creation.';
COMMENT ON COLUMN notifications.type    IS 'Determines icon, colour, and notification preference lookup key.';
COMMENT ON COLUMN notifications.user_id IS 'NULL = visible to all users in the tenant. Set for user-specific notifications.';

-- ── Webhook endpoints ─────────────────────────────────────────────────────────
CREATE TABLE webhook_endpoints (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       UUID            NOT NULL,
    url             VARCHAR(500)    NOT NULL,
    description     VARCHAR(200),
    secret_hash     VARCHAR(200)    NOT NULL,   -- HMAC secret stored as bcrypt hash (never plaintext)
    event_filter    TEXT[]          NOT NULL DEFAULT '{}',  -- empty = all events
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    last_triggered  TIMESTAMPTZ,
    success_count   INTEGER         NOT NULL DEFAULT 0,
    failure_count   INTEGER         NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_webhook_endpoints         PRIMARY KEY (id),
    CONSTRAINT fk_webhook_endpoints_tenant  FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT ck_webhook_url               CHECK (url LIKE 'https://%')
);

COMMENT ON TABLE  webhook_endpoints             IS 'Registered HTTPS endpoints for outbound webhook delivery.';
COMMENT ON COLUMN webhook_endpoints.secret_hash IS 'Hashed HMAC secret. Plaintext shown once at creation, never stored.';
COMMENT ON COLUMN webhook_endpoints.event_filter IS 'PostgreSQL text array. Empty = subscribe to all events. Otherwise subset of event type strings.';

-- ── Webhook delivery logs ─────────────────────────────────────────────────────
CREATE TABLE webhook_delivery_logs (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    endpoint_id     UUID            NOT NULL,
    sync_event_id   UUID,
    event_type      VARCHAR(100)    NOT NULL,
    payload_preview TEXT,
    http_status     SMALLINT,
    response_body   TEXT,
    latency_ms      INTEGER,
    delivered_at    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    success         BOOLEAN         NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_webhook_delivery_logs         PRIMARY KEY (id),
    CONSTRAINT fk_wdl_endpoint                  FOREIGN KEY (endpoint_id) REFERENCES webhook_endpoints(id) ON DELETE CASCADE
);