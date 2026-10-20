using System.Diagnostics;
using EtsyPriceSync.Functions.Models;
using EtsyPriceSync.Functions.Options;
using EtsyPriceSync.Functions.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EtsyPriceSync.Functions.Services;

public sealed class PriceSyncService(IProductRepository repository, IEtsyClient etsyClient, IEtsyListingIdParser parser, ICurrencyPriceFormatter formatter, IOptions<EtsyOptions> options, TimeProvider timeProvider, ILogger<PriceSyncService> logger) : IPriceSyncService
{
    public async Task<PriceSyncResult> SynchronizeAsync(CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var currency = CurrencyCatalog.NormalizeAndValidate(options.Value.Currency);
        IReadOnlyList<Product> products;
        try { products = await repository.GetProductsAsync(cancellationToken); }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Unable to load products; no Etsy requests were attempted."); throw; }
        var mapped = new List<(Product Product, long ListingId)>();
        var invalid = 0;
        foreach (var product in products)
        {
            if (product.EtsyListingId is > 0) mapped.Add((product, product.EtsyListingId.Value));
            else if (parser.TryParse(product.EtsyUrl, out var id)) mapped.Add((product, id));
            else { invalid++; logger.LogWarning("Product {ProductId} has an invalid Etsy URL and was skipped.", product.Id); }
        }
        var fetched = await etsyClient.GetListingsAsync(mapped.Select(x => x.ListingId).Distinct().ToArray(), currency, cancellationToken);
        var changed = 0; var unchanged = 0; var skipped = invalid; var failures = fetched.FailedRequests;
        foreach (var (product, listingId) in mapped)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!fetched.Listings.TryGetValue(listingId, out var listing)) { skipped++; failures++; logger.LogWarning("Etsy did not return listing {ListingId} for product {ProductId}; price preserved.", listingId, product.Id); continue; }
            if (!string.IsNullOrWhiteSpace(listing.State) && !string.Equals(listing.State, "active", StringComparison.OrdinalIgnoreCase)) { skipped++; logger.LogWarning("Listing {ListingId} is {State}; price preserved.", listingId, listing.State); continue; }
            var etsyPrice = string.Equals(currency, "GBP", StringComparison.OrdinalIgnoreCase) ? listing.Price : listing.ConvertedPrice;
            if (etsyPrice is null || !etsyPrice.TryGetDecimalAmount(out var amount)) { skipped++; failures++; logger.LogWarning("Listing {ListingId} has no valid {PriceField}; price preserved.", listingId, currency == "GBP" ? "price" : "converted_price"); continue; }
            if (string.IsNullOrWhiteSpace(etsyPrice.CurrencyCode)) { skipped++; failures++; logger.LogWarning("Listing {ListingId} returned no currency code; price preserved.", listingId); continue; }
            var returnedCurrencyCode = etsyPrice.CurrencyCode;
            if (!string.Equals(returnedCurrencyCode, currency, StringComparison.OrdinalIgnoreCase)) { skipped++; failures++; logger.LogWarning("Listing {ListingId} returned {ReturnedCurrency}, not {Currency}; price preserved.", listingId, returnedCurrencyCode, currency); continue; }
            if (listing.HasVariations) logger.LogInformation("Listing {ListingId} has variations; using its public listing-level price, which may be a starting price.", listingId);
            var price = formatter.Format(amount, currency);
            var isChanged = !string.Equals(product.Price, price, StringComparison.Ordinal);
            try
            {
                await repository.RecordSuccessfulPriceCheckAsync(product.Id, listingId, price, returnedCurrencyCode, timeProvider.GetUtcNow().UtcDateTime, isChanged, cancellationToken);
                if (isChanged) changed++; else unchanged++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { skipped++; failures++; logger.LogError(ex, "Failed to record price for product {ProductId}; continuing.", product.Id); }
        }
        timer.Stop();
        var result = new PriceSyncResult(products.Count, mapped.Count, invalid, fetched.RequestsMade, fetched.Listings.Count, changed, unchanged, skipped, failures, timer.Elapsed);
        logger.LogInformation("Etsy sync completed: {ProductsLoaded} loaded, {ValidListingIds} valid IDs, {InvalidEtsyUrls} invalid URLs, {EtsyRequestsMade} requests, {EtsyListingsReturned} listings, {PricesChanged} changed, {PricesUnchanged} unchanged, {ProductsSkipped} skipped, {Failures} failures, {DurationMs} ms.", result.ProductsLoaded, result.ValidListingIds, result.InvalidEtsyUrls, result.EtsyRequestsMade, result.EtsyListingsReturned, result.PricesChanged, result.PricesUnchanged, result.ProductsSkipped, result.Failures, result.Duration.TotalMilliseconds);
        return result;
    }
}
