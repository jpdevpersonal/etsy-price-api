# EtsyPriceSync

EtsyPriceSync is a .NET 10 Azure Functions v4 isolated-worker timer application. It reads the existing dbo.Products table, obtains current public listing-level prices from Etsy Open API v3, and updates the existing formatted Price string only when needed. It has no HTTP trigger, OAuth flow, Entity Framework, or production credentials.

## Architecture and solution

etsy-price-api.sln contains EtsyPriceSync.Functions and the referencing xUnit project EtsyPriceSync.Tests. The timer calls PriceSyncService; ProductRepository uses Microsoft.Data.SqlClient; EtsyListingIdParser maps URLs; EtsyClient uses IHttpClientFactory; and CurrencyPriceFormatter produces the final database string. reference is optional reference data and is never used at runtime.

The flow is: load products, obtain stable Etsy IDs, de-duplicate and batch IDs, call Etsy, validate listing state/money/currency, calculate decimal amount, format, compare, update, and log a structured summary. One bad URL, response, or SQL row update does not stop other products.

## Existing dbo.Products contract

The supplied dbo.Products definition is authoritative. The job reads only Id, Title, EtsyUrl, Price, Status, EtsyListingId, PriceCurrency, and PriceLastSyncedUtc. It does not retrieve Problem, Description, Bullets, images, or HTML.

Price remains nvarchar(max) for website/API compatibility with values such as $2.11. Etsy amount is divided by divisor using decimal before formatting. A retrieval failure never writes zero and never overwrites the existing Price.

Run Database/migration.sql manually before any database execution. It idempotently adds EtsyListingId bigint NULL, PriceCurrency char(3) NULL, PriceLastSyncedUtc datetime2 NULL, a filtered listing-ID index, a currency shape constraint, and a best-effort URL backfill. It is non-destructive and contains rollback guidance. Test it against a development copy first. The repository expects these columns.

EtsyListingId caches stable mappings but EtsyUrl is retained. PriceCurrency makes formatted text unambiguous. PriceLastSyncedUtc follows policy A: update on every successful valid Etsy price check, even if Price is unchanged. Price itself is assigned only when changed.

All rows with a non-empty EtsyUrl are selected. Status is not restricted. To include only published products later, add `AND Status = published` to SelectSql after confirming the status policy.

## Etsy IDs, endpoint, authentication, and variations

The parser accepts HTTPS etsy.com and subdomain URLs beginning /listing/{positive numeric ID}. Slugs, no slug, query strings, and trailing slashes work. Malformed, non-Etsy, missing, and non-numeric IDs are logged and skipped. Product titles are never used.

The client calls `GET https://api.etsy.com/v3/application/listings/batch` with listing_ids and currency. IDs are split into groups of at most 100. Approximately 42 current products therefore normally use one API request; 101 use two and 500 use five.

Requests contain `x-api-key: <keystring>:<shared_secret>`. Secrets are never logged. The selected public batch endpoint uses current API-key authentication, so this job intentionally implements no OAuth login, PKCE, browser flow, access-token storage, or refresh-token storage. CallbackUrl is retained for future functionality and is not used by this job.

Create/register an application in the Etsy developer portal, supply its AppName and HTTPS callback URL, then obtain the issued keystring and shared secret. These committed values are deliberate placeholders: REPLACE_WITH_REAL_ETSY_KEYSTRING, REPLACE_WITH_REAL_ETSY_SHARED_SECRET, REPLACE_WITH_REAL_ETSY_APP_NAME, and https://example.com/etsy/callback. Startup validation rejects placeholder secrets and app/database values.

When has_variations is true, the public listing-level price is used and logged. It may represent a starting price. The job does not invent prices from variations or call inventory unnecessarily.

## Currency and money

Set Etsy__Currency to an ISO 4217 code such as USD, GBP, EUR, AUD, or CAD. For GBP, the sync uses Etsy's `price` field; for any other configured currency, it uses `converted_price`. The selected field's `currency_code` must match the configured currency or the product price is preserved. Etsy Money is `amount / divisor` using decimal; divisor is not assumed to be 100.

CultureInfo and RegionInfo provide currency metadata. Conventional output includes $2.11, £2.11, and €2.11. Ambiguous symbols use an ISO form such as CAD 2.11 instead of a large handwritten switch.

## Configuration

Copy EtsyPriceSync.Functions/local.settings.json.example to the Git-ignored local.settings.json and replace every placeholder. The isolated worker maps double underscores to sections. Required settings are Etsy__Keystring, Etsy__SharedSecret, Etsy__AppName, Etsy__CallbackUrl, Etsy__BaseUrl, Etsy__Currency, Database__ConnectionString, EtsyPriceSyncSchedule, AzureWebJobsStorage, and FUNCTIONS_WORKER_RUNTIME=dotnet-isolated.

BaseUrl defaults to https://api.etsy.com/v3 and is validated as HTTPS on api.etsy.com. Currency defaults to USD. Never commit local.settings.json, log the API key/shared secret, or log the SQL connection string. Use a least-privilege SQL identity with SELECT on required columns and UPDATE on Price plus the three metadata columns.

## Timer schedule

Azure Functions NCRONTAB has six fields: second, minute, hour, day, month, day-of-week. Daily at 02:00 UTC: `0 0 2 * * *`. Every 12 hours: `0 0 */12 * * *`. Every 6 hours: `0 0 */6 * * *`.

## Local build, test, and run

Requirements are .NET 10 SDK, Visual Studio 2026 WSL support, Azure Functions Core Tools v4, and Azurite or development storage. Open `/home/julian/Development/etsy-price-api/etsy-price-api.sln` through the WSL workflow.

Commands: `dotnet restore etsy-price-api.sln`, `dotnet build etsy-price-api.sln`, and `dotnet test etsy-price-api.sln --no-build`.
After development database, migration, storage, and ignored settings are configured, run `func start` from EtsyPriceSync.Functions. Automated tests never call Etsy or SQL Server.

## Resilience and troubleshooting

The Microsoft standard resilience handler provides bounded timeout and backoff for transient network failures, HTTP 429, and server errors, honoring Retry-After where supported. It does not blindly retry ordinary 400, 401, 403, or 404 responses.

For 401, check keystring/shared-secret pairing, x-api-key format, whitespace, and app activation. For 403, check app approval and endpoint permissions. For 429, inspect safe rate-limit diagnostics, retain batching, and reduce schedule frequency if necessary. For 400/404, verify ID, currency, API version, and whether the listing is removed/inactive. Persistent 5xx, DNS, timeout, malformed JSON, missing price, bad divisor, and currency mismatch preserve Price.

For SQL failures, verify server/database, firewall or private networking, authentication, Encrypt settings, migration status, and least-privilege permissions. A load failure aborts before Etsy calls; one update failure logs and continues. Credentials and full connection strings are not logged.

## Key Vault and Azure SQL managed identity

In a future Function App, protect Etsy__Keystring, Etsy__SharedSecret, and Database__ConnectionString with Key Vault references such as `@Microsoft.KeyVault(SecretUri=...)`. Enable managed identity and grant only secret-user access. Key Vault is not required locally.

For passwordless Azure SQL, enable managed identity, configure an Entra administrator, create the external database user, and grant only required SELECT and column-level UPDATE rights. Set Database__ConnectionString to a managed-identity connection string with Authentication=Active Directory Managed Identity and Encrypt=True.

## Before first use and later deployment

Before the first real Etsy test: copy ignored settings; replace the keystring, shared secret, AppName, callback placeholder, and development database connection; confirm BaseUrl, target currency, and UTC schedule; apply migration.sql to development; configure Azurite; and start intentionally.

Before a development-database test: restore a representative non-production copy, back up sample prices, run/review migration, grant minimal rights, execute once, and verify changed, unchanged, skipped, currency, and timestamp behavior.

Before Azure deployment: reconfirm Etsy current API/auth and rate limits; approve the migration under change control; choose an Azure Functions v4 host supporting the isolated runtime; configure storage, schedule, currency, Key Vault, managed identity, networking, monitoring/Application Insights, alerts, backups, and rollback; rerun restore/build/tests; then deploy separately. This repository does not deploy or create Azure resources. Any future manual HTTP trigger must be authenticated; none is implemented.



Local test database
// "Database__ConnectionString": "Server=172.28.112.1,1433;Database=simplebiztoolkit-test;User ID=sa;Password=T0mbraider&1;Encrypt=True;TrustServerCertificate=True;"