using EtsyPriceSync.Functions.Models;

namespace EtsyPriceSync.Tests.Services;

public sealed class EtsyMoneyTests
{
    [Theory]
    [InlineData(211, 100, "2.11")]
    [InlineData(211, 10, "21.1")]
    [InlineData(12345, 1000, "12.345")]
    public void TryGetDecimalAmount_ValidMoney_UsesDivisor(long amount, long divisor, string expected)
    {
        var money = new EtsyMoney { Amount = amount, Divisor = divisor };
        Assert.True(money.TryGetDecimalAmount(out var result));
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), result);
    }

    [Fact]
    public void TryGetDecimalAmount_ZeroDivisor_ReturnsFalse() => Assert.False(new EtsyMoney { Amount = 211, Divisor = 0 }.TryGetDecimalAmount(out _));

    [Fact]
    public void TryGetDecimalAmount_LargeValue_RemainsDecimal()
    {
        Assert.True(new EtsyMoney { Amount = long.MaxValue, Divisor = 1000 }.TryGetDecimalAmount(out var result));
        Assert.Equal(9223372036854775.807m, result);
    }
}
