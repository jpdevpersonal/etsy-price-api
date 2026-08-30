using System.Text.Json.Serialization;

namespace EtsyPriceSync.Functions.Models;

public sealed class EtsyBatchResponse
{
    [JsonPropertyName("count")]
    public int Count { get; init; }
    [JsonPropertyName("results")]
    public IReadOnlyList<EtsyListing>? Results { get; init; }
}
