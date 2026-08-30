using EtsyPriceSync.Functions.Models;

namespace EtsyPriceSync.Functions.Services;

public interface IEtsyClient
{
    Task<EtsyFetchResult> GetListingsAsync(IReadOnlyCollection<long> listingIds, string currencyCode, CancellationToken cancellationToken);
}
