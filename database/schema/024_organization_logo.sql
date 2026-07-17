-- 024 — organization logo for tenant branding
IF COL_LENGTH('dbo.organizations', 'logo_file_name') IS NULL
BEGIN
    ALTER TABLE dbo.organizations ADD logo_file_name NVARCHAR(255) NULL;
END
GO
PRINT '024_organization_logo: completed.';
GO
