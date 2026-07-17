-- 023 — platform CMS permissions for company admins (not tenant-scoped)
IF OBJECT_ID('dbo.platform_module_permissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.platform_module_permissions (
        id                          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_platform_module_permissions PRIMARY KEY,
        module_code                 NVARCHAR(50)  NOT NULL,
        visible_to_company_admin    BIT           NOT NULL CONSTRAINT df_pmp_visible DEFAULT (1),
        writable_by_company_admin   BIT           NOT NULL CONSTRAINT df_pmp_writable DEFAULT (1),
        sort_order                  INT           NOT NULL CONSTRAINT df_pmp_sort DEFAULT (0),
        updated_at                  DATETIME2(0)  NOT NULL CONSTRAINT df_pmp_updated DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX ux_platform_module_permissions_code ON dbo.platform_module_permissions(module_code);
END
GO
PRINT '023_platform_module_permissions: completed.';
GO
