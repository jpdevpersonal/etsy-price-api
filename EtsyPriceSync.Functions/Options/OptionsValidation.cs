using EtsyPriceSync.Functions.Services;

namespace EtsyPriceSync.Functions.Options;

public static class OptionsValidation
{
    public static bool IsValidEtsyBaseUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && string.Equals(uri.Host, "api.etsy.com", StringComparison.OrdinalIgnoreCase);
    public static bool IsValidHttpsUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    public static bool IsConfiguredSecret(string? value, string placeholder) => !string.IsNullOrWhiteSpace(value) && !string.Equals(value, placeholder, StringComparison.Ordinal);
    public static bool IsConfiguredValue(string? value) => !string.IsNullOrWhiteSpace(value) && !value.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase);
    public static bool IsValidCurrency(string? value) => CurrencyCatalog.IsValid(value);
}
