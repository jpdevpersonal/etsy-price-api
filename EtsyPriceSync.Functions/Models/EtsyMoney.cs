using System.Text.Json.Serialization;

namespace EtsyPriceSync.Functions.Models;

public sealed class EtsyMoney
{
    [JsonPropertyName("amount")]
    public long? Amount { get; init; }
    [JsonPropertyName("divisor")]
    public long? Divisor { get; init; }
    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; init; }

    public bool TryGetDecimalAmount(out decimal amount)
    {
        amount = default;
        if (Amount is null || Divisor is null || Divisor <= 0)
        {
            return false;
        }
        amount = decimal.Divide(Amount.Value, Divisor.Value);
        return true;
    }
}
