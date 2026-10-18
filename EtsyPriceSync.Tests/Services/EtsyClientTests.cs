using System.Net;
using System.Text;
using System.Text.Json;
using EtsyPriceSync.Functions.Models;
using EtsyPriceSync.Functions.Options;
using EtsyPriceSync.Functions.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EtsyPriceSync.Tests.Services;

public sealed class EtsyClientTests
{
    [Fact]
    public async Task GetListingsAsync_Http200_ReturnsListingAndUsesBatchEndpoint()
    {
        var json = JsonSerializer.Serialize(new { count = 1, results = new[] { new { listing_id = 123L, state = "active", price = new { amount = 211L, divisor = 100L, currency_code = "GBP" }, converted_price = new { amount = 229L, divisor = 100L, currency_code = "USD" } } } });
        var handler = new StubHandler(_ => Response(HttpStatusCode.OK, json));
        var result = await CreateClient(handler).GetListingsAsync([123], "USD", CancellationToken.None);
        Assert.Single(result.Listings);
        Assert.Equal("GBP", result.Listings[123].Price?.CurrencyCode);
        Assert.Equal("USD", result.Listings[123].ConvertedPrice?.CurrencyCode);
        Assert.Equal(229L, result.Listings[123].ConvertedPrice?.Amount);
        Assert.Equal(1, result.RequestsMade);
        Assert.Equal(0, result.FailedRequests);
        Assert.Contains("application/listings/batch?", handler.Uris.Single());
        Assert.Contains("listing_ids=123", handler.Uris.Single());
        Assert.Contains("currency=USD", handler.Uris.Single());
        Assert.Equal("key:secret", handler.ApiKeys.Single());
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(429)]
    [InlineData(500)]
    public async Task GetListingsAsync_HttpFailure_PreservesEmptyResult(int status)
    {
        var handler = new StubHandler(_ => Response((HttpStatusCode)status, "{}"));
        var result = await CreateClient(handler).GetListingsAsync([123], "USD", CancellationToken.None);
        Assert.Empty(result.Listings);
        Assert.Equal(1, result.FailedRequests);
    }

    [Fact]
    public async Task GetListingsAsync_MalformedJson_IsFailedSafely()
    {
        var result = await CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK, "{not-json"))).GetListingsAsync([123], "USD", CancellationToken.None);
        Assert.Empty(result.Listings);
        Assert.Equal(1, result.FailedRequests);
    }

    [Fact]
    public async Task GetListingsAsync_MissingPrice_ReturnsListingForServiceValidation()
    {
        var json = JsonSerializer.Serialize(new { count = 1, results = new[] { new { listing_id = 123L, state = "active" } } });
        var result = await CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK, json))).GetListingsAsync([123], "USD", CancellationToken.None);
        Assert.Null(result.Listings[123].Price);
    }

    [Fact]
    public async Task GetListingsAsync_ChunksMoreThanOneHundredIds()
    {
        var handler = new StubHandler(_ => Response(HttpStatusCode.OK, JsonSerializer.Serialize(new { count = 0, results = Array.Empty<object>() })));
        var result = await CreateClient(handler).GetListingsAsync(Enumerable.Range(1, 250).Select(x => (long)x).ToArray(), "USD", CancellationToken.None);
        Assert.Equal(3, result.RequestsMade);
        Assert.Equal(3, handler.RequestCount);
    }

    private static EtsyClient CreateClient(StubHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.etsy.com/v3/") };
        return new EtsyClient(httpClient, Options.Create(new EtsyOptions { Keystring = "key", SharedSecret = "secret" }), NullLogger<EtsyClient>.Instance);
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string json) => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public List<string> Uris { get; } = [];
        public List<string> ApiKeys { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Uris.Add(request.RequestUri!.ToString());
            ApiKeys.Add(string.Join(",", request.Headers.GetValues("x-api-key")));
            return Task.FromResult(responseFactory(request));
        }
    }
}
