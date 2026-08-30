namespace EtsyPriceSync.Functions.Services;

public sealed class EtsyListingIdParser : IEtsyListingIdParser
{
    public bool TryParse(string? etsyUrl, out long listingId)
    {
        listingId = default;
        if (!Uri.TryCreate(etsyUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !IsEtsyHost(uri.Host))
        {
            return false;
        }
        var segments = uri.AbsolutePath.Split("/", StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || !string.Equals(segments[0], "listing", StringComparison.OrdinalIgnoreCase) || !long.TryParse(segments[1], out listingId) || listingId <= 0)
        {
            listingId = default;
            return false;
        }
        return true;
    }

    private static bool IsEtsyHost(string host) => string.Equals(host, "etsy.com", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".etsy.com", StringComparison.OrdinalIgnoreCase);
}
