using System.Net.Http.Json;
using System.Text.Json;
using EtsyPriceSync.Functions.Models;
using EtsyPriceSync.Functions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EtsyPriceSync.Functions.Services;

public sealed class EtsyClient(HttpClient httpClient, IOptions<EtsyOptions> options, ILogger<EtsyClient> logger) : IEtsyClient
{
    private const int MaximumBatchSize = 100;
    private readonly EtsyOptions _options = options.Value;

    public async Task<EtsyFetchResult> GetListingsAsync(IReadOnlyCollection<long> listingIds, string currencyCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listingIds);
        var currency = CurrencyCatalog.NormalizeAndValidate(currencyCode);
        var listings = new Dictionary<long, EtsyListing>();
        var requests = 0;
        var failures = 0;
        foreach (var batch in listingIds.Distinct().BatchAtMost(MaximumBatchSize))
        {
            requests++;
            try
            {
                using var request = CreateRequest(batch, currency);
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                LogRateLimits(response);
                if (!response.IsSuccessStatusCode)
                {
                    failures++;
                    logger.LogWarning("Etsy batch request {RequestNumber} failed with HTTP {StatusCode}; {ListingCount} listings will not be updated.", requests, (int)response.StatusCode, batch.Count);
                    continue;
                }
                var payload = await response.Content.ReadFromJsonAsync<EtsyBatchResponse>(cancellationToken: cancellationToken);
                if (payload?.Results is null)
                {
                    failures++;
                    logger.LogWarning("Etsy batch request {RequestNumber} returned no results collection.", requests);
                    continue;
                }
                foreach (var listing in payload.Results.Where(x => x.ListingId > 0)) listings[listing.ListingId] = listing;
            }
            catch (JsonException ex)
            {
                failures++;
                logger.LogWarning(ex, "Etsy batch request {RequestNumber} returned malformed JSON.", requests);
            }
            catch (HttpRequestException ex)
            {
                failures++;
                logger.LogWarning(ex, "Etsy batch request {RequestNumber} failed after transient retries.", requests);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                failures++;
                logger.LogWarning(ex, "Etsy batch request {RequestNumber} timed out after transient retries.", requests);
            }
        }
        return new EtsyFetchResult(listings, requests, failures);
    }

    private HttpRequestMessage CreateRequest(IReadOnlyList<long> listingIds, string currency)
    {
        var relativeUri = $"application/listings/batch?listing_ids={Uri.EscapeDataString(string.Join(",", listingIds))}&currency={Uri.EscapeDataString(currency)}";
        var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
        request.Headers.Add("x-api-key", $"{_options.Keystring}:{_options.SharedSecret}");
        request.Headers.Accept.ParseAdd("application/json");
        return request;
    }

    private void LogRateLimits(HttpResponseMessage response)
    {
        var second = GetHeader(response, "x-remaining-this-second");
        var daily = GetHeader(response, "x-remaining-today");
        if (second is not null || daily is not null) logger.LogDebug("Etsy rate limit remaining: second {SecondRemaining}, daily {DailyRemaining}.", second, daily);
    }

    private static string? GetHeader(HttpResponseMessage response, string name) => response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
}
