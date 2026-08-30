using EtsyPriceSync.Functions.Services;

namespace EtsyPriceSync.Tests.Services;

public sealed class EtsyListingIdParserTests
{
    private readonly EtsyListingIdParser _parser = new();

    [Theory]
    [InlineData("https://www.etsy.com/listing/1765516952/example-product")]
    [InlineData("https://www.etsy.com/listing/1765516952/example-product?ref=shop")]
    [InlineData("https://www.etsy.com/listing/1765516952")]
    [InlineData("https://www.etsy.com/listing/1765516952/")]
    public void TryParse_ValidUrl_ReturnsListingId(string url)
    {
        Assert.True(_parser.TryParse(url, out var id));
        Assert.Equal(1765516952, id);
    }

    [Theory]
    [InlineData("https://example.com/listing/1765516952/item")]
    [InlineData("https://www.etsy.com/listing/")]
    [InlineData("https://www.etsy.com/listing/not-numeric/item")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_InvalidUrl_ReturnsFalse(string? url) => Assert.False(_parser.TryParse(url, out _));
}
