-- ============================================================================
-- 017 — Subscription plans (SaaS tiers). Idempotent.
-- ============================================================================
IF OBJECT_ID('dbo.subscription_plans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.subscription_plans (
        id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_subscription_plans PRIMARY KEY,
        name              NVARCHAR(100)  NOT NULL,
        max_users         INT            NOT NULL,
        max_projects      INT            NOT NULL,
        ai_enabled        BIT            NOT NULL CONSTRAINT df_plans_ai DEFAULT 0,
        storage_limit_mb  BIGINT         NOT NULL,
        price             DECIMAL(10,2)  NOT NULL CONSTRAINT df_plans_price DEFAULT 0
    );
    CREATE UNIQUE INDEX ux_subscription_plans_name ON dbo.subscription_plans(name);
END
GO

MERGE dbo.subscription_plans AS t
USING (VALUES
    (N'Starter',       5,      3,      0, 1024,      0.00),
    (N'Professional',  50,     50,     1, 51200,    49.00),
    (N'Enterprise',    100000, 100000, 1, 1048576, 499.00)
) AS s(name, max_users, max_projects, ai_enabled, storage_limit_mb, price)
ON t.name = s.name
WHEN NOT MATCHED THEN
    INSERT (name, max_users, max_projects, ai_enabled, storage_limit_mb, price)
    VALUES (s.name, s.max_users, s.max_projects, s.ai_enabled, s.storage_limit_mb, s.price);
GO

PRINT '017_subscription_plans: completed.';
GO
