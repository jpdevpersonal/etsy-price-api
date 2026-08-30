using EtsyPriceSync.Functions.Models;

namespace EtsyPriceSync.Functions.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken);
    Task RecordSuccessfulPriceCheckAsync(Guid productId, long listingId, string formattedPrice, string currencyCode, DateTime checkedAtUtc, bool priceChanged, CancellationToken cancellationToken);
}
