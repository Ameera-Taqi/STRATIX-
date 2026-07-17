-- ============================================================================
-- 018 — Per-organization subscriptions (one-to-one with organizations). Idempotent.
-- ============================================================================
IF OBJECT_ID('dbo.subscriptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.subscriptions (
        id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_subscriptions PRIMARY KEY,
        organization_id  BIGINT        NOT NULL,
        plan_code        NVARCHAR(50)  NOT NULL CONSTRAINT df_subscriptions_plan   DEFAULT 'TRIAL',
        status           NVARCHAR(30)  NOT NULL CONSTRAINT df_subscriptions_status DEFAULT 'TRIALING',
        started_at       DATETIME2(0)  NOT NULL CONSTRAINT df_subscriptions_started DEFAULT SYSUTCDATETIME(),
        trial_ends_at    DATETIME2(0)  NULL,
        ends_at          DATETIME2(0)  NULL,
        created_at       DATETIME2(0)  NOT NULL CONSTRAINT df_subscriptions_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT fk_subscriptions_organization FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id)
    );
    CREATE UNIQUE INDEX ux_subscriptions_organization ON dbo.subscriptions(organization_id);
END
GO

-- Backfill: every existing organization without a subscription gets an ACTIVE one
-- keyed to the plan already recorded on the organization row.
INSERT INTO dbo.subscriptions (organization_id, plan_code, status, started_at, created_at)
SELECT o.id, o.subscription_plan, 'ACTIVE', o.created_at, SYSUTCDATETIME()
FROM dbo.organizations o
WHERE NOT EXISTS (SELECT 1 FROM dbo.subscriptions s WHERE s.organization_id = o.id);
GO

PRINT '018_subscriptions: completed.';
GO
