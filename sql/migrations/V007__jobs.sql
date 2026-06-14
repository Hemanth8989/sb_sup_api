-- ============================================================
-- V007: Jobs (fabricator customer work orders)
-- ============================================================

CREATE TABLE jobs (
    id                  UUID            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id           UUID            NOT NULL,   -- fabricator tenant
    job_number          VARCHAR(50)     NOT NULL,
    job_name            VARCHAR(300)    NOT NULL,
    customer_name       VARCHAR(200),
    customer_email      VARCHAR(254),
    customer_phone      VARCHAR(30),
    status              VARCHAR(20)     NOT NULL DEFAULT 'quoted',
    template_date       DATE,
    fabrication_date    DATE,
    install_date        DATE,
    material_budget     NUMERIC(12,2),
    total_ordered       NUMERIC(12,2)   NOT NULL DEFAULT 0,
    total_received      NUMERIC(12,2)   NOT NULL DEFAULT 0,
    moraware_job_id     VARCHAR(100),
    actionflow_job_id   VARCHAR(100),
    notes               TEXT,
    created_by          UUID,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_jobs              PRIMARY KEY (id),
    CONSTRAINT fk_jobs_tenant       FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_jobs_number       UNIQUE (tenant_id, job_number),
    CONSTRAINT ck_jobs_status       CHECK (status IN (
                                        'quoted','approved','templated','fabricating',
                                        'ready','installed','closed','cancelled')),
    CONSTRAINT ck_jobs_budget       CHECK (material_budget IS NULL OR material_budget >= 0),
    CONSTRAINT ck_jobs_ordered      CHECK (total_ordered >= 0),
    CONSTRAINT ck_jobs_received     CHECK (total_received >= 0)
);

COMMENT ON TABLE  jobs                  IS 'Customer work orders managed by fabricator tenants.';
COMMENT ON COLUMN jobs.total_ordered    IS 'Sum of non-draft PO total_amounts linked to this job. Updated by trigger.';
COMMENT ON COLUMN jobs.total_received   IS 'Sum of received PO amounts. Updated when PO status = received.';