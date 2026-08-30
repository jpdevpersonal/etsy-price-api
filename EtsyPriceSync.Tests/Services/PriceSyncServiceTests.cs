using EtsyPriceSync.Functions.Models;
using EtsyPriceSync.Functions.Options;
using EtsyPriceSync.Functions.Repositories;
using EtsyPriceSync.Functions.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EtsyPriceSync.Tests.Services;

public sealed class PriceSyncServiceTests
{
    [Fact]
    public async Task SynchronizeAsync_UnchangedPrice_RecordsCheckWithoutPriceChange()
    {
        var repository = new FakeRepository([Product("$2.11")]);
        var result = await CreateService(repository, Listing("USD", 211), "USD").SynchronizeAsync(CancellationToken.None);
        Assert.Equal(0, result.PricesChanged);
        Assert.Equal(1, result.PricesUnchanged);
        Assert.False(Assert.Single(repository.Updates).PriceChanged);
    }

    [Fact]
    public async Task SynchronizeAsync_ChangedPrice_UpdatesFormattedPrice()
    {
        var repository = new FakeRepository([Product("$2.11")]);
        var result = await CreateService(repository, Listing("USD", 229), "USD").SynchronizeAsync(CancellationToken.None);
        Assert.Equal(1, result.PricesChanged);
        var update = Assert.Single(repository.Updates);
        Assert.True(update.PriceChanged);
        Assert.Equal("$2.29", update.Price);
        Assert.Equal("USD", update.Currency);
    }

    [Fact]
    public async Task SynchronizeAsync_EtsyFailure_PreservesExistingPrice()
    {
        var repository = new FakeRepository([Product("$2.11")]);
        var service = CreateService(repository, null, "USD", failedRequests: 1);
        var result = await service.SynchronizeAsync(CancellationToken.None);
        Assert.Empty(repository.Updates);
        Assert.Equal(1, result.ProductsSkipped);
        Assert.True(result.Failures >= 1);
    }

    [Fact]
    public async Task SynchronizeAsync_InvalidUrl_SkipsAndContinues()
    {
        var invalid = Product("$1.00") with { Id = Guid.NewGuid(), EtsyUrl = "https://example.com/listing/123" };
        var repository = new FakeRepository([invalid, Product("$2.11")]);
        var result = await CreateService(repository, Listing("USD", 229), "USD").SynchronizeAsync(CancellationToken.None);
        Assert.Equal(1, result.InvalidEtsyUrls);
        Assert.Single(repository.Updates);
    }

    [Fact]
    public async Task SynchronizeAsync_CurrencyMismatch_PreservesExistingPrice()
    {
        var repository = new FakeRepository([Product("$2.11")]);
        var result = await CreateService(repository, Listing("CAD", 229), "USD").SynchronizeAsync(CancellationToken.None);
        Assert.Empty(repository.Updates);
        Assert.Equal(1, result.ProductsSkipped);
    }

    [Fact]
    public async Task SynchronizeAsync_RequestedGbpAndReturnedGbp_UpdatesPrice()
    {
        var repository = new FakeRepository([Product("£2.00")]);
        var client = new FakeEtsyClient(Listing("GBP", 211));
        var result = await CreateService(repository, client, "GBP").SynchronizeAsync(CancellationToken.None);
        Assert.Equal("GBP", client.RequestedCurrency);
        Assert.Equal("£2.11", Assert.Single(repository.Updates).Price);
        Assert.Equal(1, result.PricesChanged);
    }

    private static Product Product(string price) => new(Guid.NewGuid(), "Product", "https://www.etsy.com/listing/1765516952/example", price, "published", null, null, null);
    private static EtsyListing Listing(string currency, long amount) => new() { ListingId = 1765516952, State = "active", Price = new EtsyMoney { Amount = amount, Divisor = 100, CurrencyCode = currency } };
    private static PriceSyncService CreateService(FakeRepository repository, EtsyListing? listing, string currency, int failedRequests = 0) => CreateService(repository, new FakeEtsyClient(listing, failedRequests), currency);
    private static PriceSyncService CreateService(FakeRepository repository, FakeEtsyClient client, string currency) => new(repository, client, new EtsyListingIdParser(), new CurrencyPriceFormatter(), Options.Create(new EtsyOptions { Currency = currency }), TimeProvider.System, NullLogger<PriceSyncService>.Instance);

    private sealed class FakeEtsyClient(EtsyListing? listing, int failedRequests = 0) : IEtsyClient
    {
        public string? RequestedCurrency { get; private set; }
        public Task<EtsyFetchResult> GetListingsAsync(IReadOnlyCollection<long> listingIds, string currencyCode, CancellationToken cancellationToken)
        {
            RequestedCurrency = currencyCode;
            IReadOnlyDictionary<long, EtsyListing> results = listing is null ? new Dictionary<long, EtsyListing>() : new Dictionary<long, EtsyListing> { [listing.ListingId] = listing };
            return Task.FromResult(new EtsyFetchResult(results, listingIds.Count == 0 ? 0 : 1, failedRequests));
        }
    }

    private sealed class FakeRepository(IReadOnlyList<Product> products) : IProductRepository
    {
        public List<Update> Updates { get; } = [];
        public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken) => Task.FromResult(products);
        public Task RecordSuccessfulPriceCheckAsync(Guid productId, long listingId, string formattedPrice, string currencyCode, DateTime checkedAtUtc, bool priceChanged, CancellationToken cancellationToken)
        {
            Updates.Add(new Update(productId, formattedPrice, currencyCode, checkedAtUtc, priceChanged));
            return Task.CompletedTask;
        }
    }

    private sealed record Update(Guid ProductId, string Price, string Currency, DateTime CheckedAtUtc, bool PriceChanged);
}
