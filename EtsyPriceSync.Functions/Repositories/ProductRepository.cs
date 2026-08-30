using EtsyPriceSync.Functions.Models;
using EtsyPriceSync.Functions.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EtsyPriceSync.Functions.Repositories;

public sealed class ProductRepository(IOptions<DatabaseOptions> options) : IProductRepository
{
    private const string SelectSql = "SELECT Id, Title, EtsyUrl, Price, Status, EtsyListingId, PriceCurrency, PriceLastSyncedUtc FROM dbo.Products WHERE EtsyUrl IS NOT NULL AND LTRIM(RTRIM(EtsyUrl)) <> '';";
    private const string ChangedSql = "UPDATE dbo.Products SET Price=@Price, EtsyListingId=@EtsyListingId, PriceCurrency=@PriceCurrency, PriceLastSyncedUtc=@PriceLastSyncedUtc WHERE Id=@Id;";
    private const string CheckedSql = "UPDATE dbo.Products SET EtsyListingId=@EtsyListingId, PriceCurrency=@PriceCurrency, PriceLastSyncedUtc=@PriceLastSyncedUtc WHERE Id=@Id;";
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var products = new List<Product>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(SelectSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new Product(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetInt64(5), reader.IsDBNull(6) ? null : reader.GetString(6), reader.IsDBNull(7) ? null : DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)));
        }
        return products;
    }

    public async Task RecordSuccessfulPriceCheckAsync(Guid productId, long listingId, string formattedPrice, string currencyCode, DateTime checkedAtUtc, bool priceChanged, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(priceChanged ? ChangedSql : CheckedSql, connection);
        command.Parameters.Add("@Id", System.Data.SqlDbType.UniqueIdentifier).Value = productId;
        command.Parameters.Add("@EtsyListingId", System.Data.SqlDbType.BigInt).Value = listingId;
        command.Parameters.Add("@PriceCurrency", System.Data.SqlDbType.Char, 3).Value = currencyCode;
        command.Parameters.Add("@PriceLastSyncedUtc", System.Data.SqlDbType.DateTime2).Value = checkedAtUtc;
        if (priceChanged) command.Parameters.Add("@Price", System.Data.SqlDbType.NVarChar, -1).Value = formattedPrice;
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows != 1) throw new InvalidOperationException($"Expected one updated row but received {rows}.");
    }
}
