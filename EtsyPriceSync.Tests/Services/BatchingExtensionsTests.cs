using EtsyPriceSync.Functions.Services;

namespace EtsyPriceSync.Tests.Services;

public sealed class BatchingExtensionsTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(42, 1)]
    [InlineData(100, 1)]
    [InlineData(101, 2)]
    [InlineData(250, 3)]
    public void BatchAtMost_ProducesExpectedBatches(int count, int expectedBatches)
    {
        var batches = Enumerable.Range(1, count).BatchAtMost(100).ToArray();
        Assert.Equal(expectedBatches, batches.Length);
        Assert.All(batches, batch => Assert.InRange(batch.Count, 1, 100));
        Assert.Equal(count, batches.Sum(batch => batch.Count));
    }
}
