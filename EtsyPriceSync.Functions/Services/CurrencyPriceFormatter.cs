using System.Collections.Concurrent;
using System.Globalization;

namespace EtsyPriceSync.Functions.Services;

public sealed class CurrencyPriceFormatter : ICurrencyPriceFormatter
{
    private static readonly ConcurrentDictionary<string, CurrencyFormat> Formats = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> ConventionalSymbolCurrencies = new(StringComparer.OrdinalIgnoreCase) { "USD", "GBP", "EUR" };

    public string Format(decimal amount, string currencyCode)
    {
        var normalized = CurrencyCatalog.NormalizeAndValidate(currencyCode);
        var format = Formats.GetOrAdd(normalized, CreateFormat);
        var number = amount.ToString($"N{format.DecimalDigits}", CultureInfo.InvariantCulture);
        return format.UseSymbol ? string.Concat(format.Symbol, number) : string.Concat(normalized, " ", number);
    }

    private static CurrencyFormat CreateFormat(string currencyCode)
    {
        var sample = CurrencyCatalog.GetCultures(currencyCode).First();
        var region = new RegionInfo(sample.Name);
        var symbol = region.CurrencySymbol;
        var currencies = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(TryGetRegion).Where(x => x is not null && x.CurrencySymbol == symbol).Select(x => x!.ISOCurrencySymbol).Distinct(StringComparer.OrdinalIgnoreCase).Take(2).Count();
        return new CurrencyFormat(symbol, sample.NumberFormat.CurrencyDecimalDigits, (currencies == 1 && symbol.Length == 1) || ConventionalSymbolCurrencies.Contains(currencyCode));
    }

    private static RegionInfo? TryGetRegion(CultureInfo culture)
    {
        try { return new RegionInfo(culture.Name); } catch (ArgumentException) { return null; }
    }

    private sealed record CurrencyFormat(string Symbol, int DecimalDigits, bool UseSymbol);
}
