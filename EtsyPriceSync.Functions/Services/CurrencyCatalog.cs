using System.Globalization;

namespace EtsyPriceSync.Functions.Services;

public static class CurrencyCatalog
{
    public static bool IsValid(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 || !currencyCode.All(char.IsAsciiLetter))
        {
            return false;
        }
        return GetCultures(currencyCode.ToUpperInvariant()).Any();
    }

    public static string NormalizeAndValidate(string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        var normalized = currencyCode.ToUpperInvariant();
        return IsValid(normalized) ? normalized : throw new ArgumentException($"{currencyCode} is not a recognized ISO 4217 currency code.", nameof(currencyCode));
    }

    public static IEnumerable<CultureInfo> GetCultures(string currencyCode)
    {
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            RegionInfo region;
            try { region = new RegionInfo(culture.Name); } catch (ArgumentException) { continue; }
            if (string.Equals(region.ISOCurrencySymbol, currencyCode, StringComparison.OrdinalIgnoreCase))
            {
                yield return culture;
            }
        }
    }
}
