namespace EtsyPriceSync.Functions.Models;

public sealed record Product(Guid Id, string Title, string EtsyUrl, string? Price, string Status, long? EtsyListingId, string? PriceCurrency, DateTime? PriceLastSyncedUtc);
