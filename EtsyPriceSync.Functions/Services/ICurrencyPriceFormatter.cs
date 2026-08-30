namespace EtsyPriceSync.Functions.Services;

public interface ICurrencyPriceFormatter
{
    string Format(decimal amount, string currencyCode);
}
