using System.Data;
using Microsoft.Data.SqlClient;
using ProductApi.Models;

namespace ProductApi.Data;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(ProductRequest req);
    Task<Product?> UpdateAsync(int id, ProductRequest req);
    Task<bool> DeleteAsync(int id);
}

/// <summary>所有資料存取都透過 dbo.usp_Products_CRUD 這支 SP</summary>
public class ProductRepository : IProductRepository
{
    private const string SpName = "dbo.usp_Products_CRUD";
    private readonly string _connStr;

    public ProductRepository(IConfiguration config)
    {
        _connStr = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default 未設定");
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
        => await QueryAsync("LIST");

    public async Task<Product?> GetByIdAsync(int id)
        => (await QueryAsync("GET", id)).FirstOrDefault();

    public async Task<Product> CreateAsync(ProductRequest req)
        => (await QueryAsync("CREATE", null, req)).Single();

    public async Task<Product?> UpdateAsync(int id, ProductRequest req)
        => (await QueryAsync("UPDATE", id, req)).FirstOrDefault();

    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = new SqlConnection(_connStr);
        await using var cmd = BuildCommand(conn, "DELETE", id, null);
        await conn.OpenAsync();
        var affected = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return affected > 0;
    }

    // ---------- helpers ----------

    private async Task<List<Product>> QueryAsync(string action, int? id = null, ProductRequest? req = null)
    {
        await using var conn = new SqlConnection(_connStr);
        await using var cmd = BuildCommand(conn, action, id, req);
        await conn.OpenAsync();

        var list = new List<Product>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Product
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                Stock = reader.GetInt32(reader.GetOrdinal("Stock")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            });
        }
        return list;
    }

    private static SqlCommand BuildCommand(SqlConnection conn, string action, int? id, ProductRequest? req)
    {
        var cmd = new SqlCommand(SpName, conn) { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 10).Value = action;
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = (object?)id ?? DBNull.Value;
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = (object?)req?.Name ?? DBNull.Value;

        var price = cmd.Parameters.Add("@Price", SqlDbType.Decimal);
        price.Precision = 10;
        price.Scale = 2;
        price.Value = (object?)req?.Price ?? DBNull.Value;

        cmd.Parameters.Add("@Stock", SqlDbType.Int).Value = (object?)req?.Stock ?? DBNull.Value;
        return cmd;
    }
}
