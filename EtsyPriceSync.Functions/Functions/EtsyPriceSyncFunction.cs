using EtsyPriceSync.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EtsyPriceSync.Functions.Functions;

public sealed class EtsyPriceSyncFunction(IPriceSyncService service, ILogger<EtsyPriceSyncFunction> logger)
{
    [Function(nameof(EtsyPriceSyncFunction))]
    public async Task RunAsync([TimerTrigger("%EtsyPriceSyncSchedule%")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        logger.LogInformation("Etsy price sync timer fired. IsPastDue: {IsPastDue}; next: {Next}.", timerInfo.IsPastDue, timerInfo.ScheduleStatus?.Next);
        await service.SynchronizeAsync(cancellationToken);
    }
}
