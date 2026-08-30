namespace EtsyPriceSync.Functions.Services;

public interface IEtsyListingIdParser
{
    bool TryParse(string? etsyUrl, out long listingId);
}
