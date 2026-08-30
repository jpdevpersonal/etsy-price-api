using EtsyPriceSync.Functions.Models;

namespace EtsyPriceSync.Functions.Services;

public interface IPriceSyncService
{
    Task<PriceSyncResult> SynchronizeAsync(CancellationToken cancellationToken);
}
