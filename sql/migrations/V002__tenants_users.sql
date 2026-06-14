-- ============================================================
-- V002: Tenants and users
-- ============================================================

CREATE TABLE tenants (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    type            VARCHAR(20)     NOT NULL,
    name            VARCHAR(300)    NOT NULL,
    slug            VARCHAR(100)    NOT NULL,
    plan            VARCHAR(50)     NOT NULL DEFAULT 'starter',
    country         VARCHAR(2)      NOT NULL DEFAULT 'US',
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    trial_ends_at   TIMESTAMPTZ,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_tenants PRIMARY KEY (id),
    CONSTRAINT uq_tenants_slug UNIQUE (slug),
    CONSTRAINT ck_tenants_type CHECK (type IN ('supplier', 'fabricator')),
    CONSTRAINT ck_tenants_plan CHECK (plan IN ('starter', 'pro', 'enterprise'))
);

COMMENT ON TABLE  tenants           IS 'Root tenant record. Every supplier and fabricator is a tenant.';
COMMENT ON COLUMN tenants.type      IS 'supplier | fabricator — determines which portal and endpoints are accessible.';
COMMENT ON COLUMN tenants.slug      IS 'URL-safe unique identifier used in public-facing paths.';
COMMENT ON COLUMN tenants.plan      IS 'Subscription plan tier controlling feature access and limits.';

CREATE TABLE users (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       UUID            NOT NULL,
    clerk_user_id   VARCHAR(200)    NOT NULL,
    email           VARCHAR(254)    NOT NULL,
    full_name       VARCHAR(200)    NOT NULL,
    role            VARCHAR(20)     NOT NULL DEFAULT 'viewer',
    avatar_url      VARCHAR(500),
    phone           VARCHAR(30),
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    last_sign_in    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_users         PRIMARY KEY (id),
    CONSTRAINT fk_users_tenant  FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_users_clerk   UNIQUE (clerk_user_id),
    CONSTRAINT ck_users_role    CHECK (role IN ('owner', 'admin', 'manager', 'viewer'))
);

COMMENT ON TABLE  users                 IS 'Individual users belonging to a tenant. Auth managed by Clerk.';
COMMENT ON COLUMN users.clerk_user_id   IS 'Clerk user ID from JWT sub claim. Used to resolve tenant context.';
COMMENT ON COLUMN users.role            IS 'owner | admin | manager | viewer — controls write permissions within the tenant.';