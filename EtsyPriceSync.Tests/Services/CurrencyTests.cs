using EtsyPriceSync.Functions.Options;
using EtsyPriceSync.Functions.Services;

namespace EtsyPriceSync.Tests.Services;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("EUR")]
    public void CurrencyCatalog_KnownCode_IsValid(string code) => Assert.True(CurrencyCatalog.IsValid(code));

    [Theory]
    [InlineData("US")]
    [InlineData("123")]
    [InlineData("ZZZ")]
    public void CurrencyCatalog_InvalidCode_IsRejected(string code)
    {
        Assert.False(CurrencyCatalog.IsValid(code));
        Assert.False(OptionsValidation.IsValidCurrency(code));
    }

    [Theory]
    [InlineData("USD", "$2.11")]
    [InlineData("GBP", "£2.11")]
    [InlineData("EUR", "€2.11")]
    [InlineData("CAD", "CAD 2.11")]
    public void Format_UsesExpectedRepresentation(string code, string expected) => Assert.Equal(expected, new CurrencyPriceFormatter().Format(2.11m, code));
}
