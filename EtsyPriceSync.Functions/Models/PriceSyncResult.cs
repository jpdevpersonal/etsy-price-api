namespace EtsyPriceSync.Functions.Models;

public sealed record PriceSyncResult(int ProductsLoaded, int ValidListingIds, int InvalidEtsyUrls, int EtsyRequestsMade, int EtsyListingsReturned, int PricesChanged, int PricesUnchanged, int ProductsSkipped, int Failures, TimeSpan Duration);
