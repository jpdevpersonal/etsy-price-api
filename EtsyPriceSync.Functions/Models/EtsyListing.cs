using System.Text.Json.Serialization;

namespace EtsyPriceSync.Functions.Models;

public sealed class EtsyListing
{
    [JsonPropertyName("listing_id")]
    public long ListingId { get; init; }
    [JsonPropertyName("state")]
    public string? State { get; init; }
    [JsonPropertyName("price")]
    public EtsyMoney? Price { get; init; }
    [JsonPropertyName("converted_price")]
    public EtsyMoney? ConvertedPrice { get; init; }
    [JsonPropertyName("has_variations")]
    public bool HasVariations { get; init; }
}
