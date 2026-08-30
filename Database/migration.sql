/* Additive, idempotent migration for the existing dbo.Products table. Review and run manually against development first. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.Products', N'EtsyListingId') IS NULL ALTER TABLE dbo.Products ADD EtsyListingId bigint NULL;
IF COL_LENGTH(N'dbo.Products', N'PriceCurrency') IS NULL ALTER TABLE dbo.Products ADD PriceCurrency char(3) NULL;
IF COL_LENGTH(N'dbo.Products', N'PriceLastSyncedUtc') IS NULL ALTER TABLE dbo.Products ADD PriceLastSyncedUtc datetime2 NULL;

/* Best-effort backfill from HTTPS Etsy /listing/{numeric-id} URLs. */
;WITH UrlParts AS
(
    SELECT Id, SUBSTRING(EtsyUrl, CHARINDEX('/listing/', EtsyUrl) + LEN('/listing/'), 64) AS ListingTail
    FROM dbo.Products
    WHERE EtsyListingId IS NULL AND EtsyUrl IS NOT NULL AND CHARINDEX('/listing/', EtsyUrl) > 0
      AND (LOWER(EtsyUrl) LIKE 'https://etsy.com/%' OR LOWER(EtsyUrl) LIKE 'https://%.etsy.com/%')
), ParsedIds AS
(
    SELECT Id, TRY_CONVERT(bigint, LEFT(ListingTail, PATINDEX('%[^0-9]%', ListingTail + 'x') - 1)) AS ListingId FROM UrlParts
)
UPDATE products SET EtsyListingId = parsed.ListingId
FROM dbo.Products AS products INNER JOIN ParsedIds AS parsed ON parsed.Id = products.Id
WHERE parsed.ListingId > 0;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Products') AND name = N'CK_Products_PriceCurrency_ISOCode')
    ALTER TABLE dbo.Products WITH CHECK ADD CONSTRAINT CK_Products_PriceCurrency_ISOCode CHECK (PriceCurrency IS NULL OR (LEN(PriceCurrency) = 3 AND PriceCurrency NOT LIKE '%[^A-Z]%'));

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Products') AND name = N'IX_Products_EtsyListingId')
    CREATE NONCLUSTERED INDEX IX_Products_EtsyListingId ON dbo.Products(EtsyListingId) WHERE EtsyListingId IS NOT NULL;

COMMIT TRANSACTION;

/* OPTIONAL DESTRUCTIVE ROLLBACK - review before manually running:
DROP INDEX IF EXISTS IX_Products_EtsyListingId ON dbo.Products;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Products') AND name=N'CK_Products_PriceCurrency_ISOCode') ALTER TABLE dbo.Products DROP CONSTRAINT CK_Products_PriceCurrency_ISOCode;
IF COL_LENGTH(N'dbo.Products',N'PriceLastSyncedUtc') IS NOT NULL ALTER TABLE dbo.Products DROP COLUMN PriceLastSyncedUtc;
IF COL_LENGTH(N'dbo.Products',N'PriceCurrency') IS NOT NULL ALTER TABLE dbo.Products DROP COLUMN PriceCurrency;
IF COL_LENGTH(N'dbo.Products',N'EtsyListingId') IS NOT NULL ALTER TABLE dbo.Products DROP COLUMN EtsyListingId;
*/
