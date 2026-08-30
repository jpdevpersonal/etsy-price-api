namespace EtsyPriceSync.Functions.Options;

public sealed class EtsyOptions
{
    public const string SectionName = "Etsy";
    public const string PlaceholderKeystring = "REPLACE_WITH_REAL_ETSY_KEYSTRING";
    public const string PlaceholderSharedSecret = "REPLACE_WITH_REAL_ETSY_SHARED_SECRET";
    public const string PlaceholderAppName = "REPLACE_WITH_REAL_ETSY_APP_NAME";
    public string Keystring { get; init; } = PlaceholderKeystring;
    public string SharedSecret { get; init; } = PlaceholderSharedSecret;
    public string AppName { get; init; } = PlaceholderAppName;
    public string CallbackUrl { get; init; } = "https://example.com/etsy/callback";
    public string BaseUrl { get; init; } = "https://api.etsy.com/v3";
    public string Currency { get; init; } = "USD";
}
