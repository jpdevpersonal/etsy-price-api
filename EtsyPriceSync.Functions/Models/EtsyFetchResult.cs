namespace EtsyPriceSync.Functions.Models;

public sealed record EtsyFetchResult(IReadOnlyDictionary<long, EtsyListing> Listings, int RequestsMade, int FailedRequests);
