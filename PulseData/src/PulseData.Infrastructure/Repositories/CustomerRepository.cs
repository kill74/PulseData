using Dapper;
using PulseData.Core.DTOs;
using PulseData.Core.Interfaces;
using PulseData.Core.Models;
using PulseData.Infrastructure.Data;

namespace PulseData.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly DbConnectionFactory _db;

    public CustomerRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT id, email, first_name AS FirstName, last_name AS LastName,
                   country, city, created_at AS CreatedAt, is_active AS IsActive
            FROM customers
            WHERE id = @Id
            """;

        using var conn = await _db.CreateConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
    }

    public async Task<PagedResult<Customer>> GetAllAsync(int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;

        const string countSql = "SELECT COUNT(*) FROM customers WHERE is_active = TRUE";

        const string dataSql = """
            SELECT id, email, first_name AS FirstName, last_name AS LastName,
                   country, city, created_at AS CreatedAt, is_active AS IsActive
            FROM customers
            WHERE is_active = TRUE
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        using var conn = await _db.CreateConnectionAsync();

        var total = await conn.ExecuteScalarAsync<int>(countSql);
        var items = await conn.QueryAsync<Customer>(dataSql, new { PageSize = pageSize, Offset = offset });

        return new PagedResult<Customer>(items, total, page, pageSize);
    }
}

public class ProductRepository : IProductRepository
{
    private readonly DbConnectionFactory _db;

    public ProductRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT
                p.id, p.category_id AS CategoryId, p.sku, p.name, p.description,
                p.unit_price AS UnitPrice, p.stock, p.is_active AS IsActive, p.created_at AS CreatedAt,
                c.id, c.name, c.description
            FROM products p
            JOIN categories c ON c.id = p.category_id
            WHERE p.id = @Id
            """;

        using var conn = await _db.CreateConnectionAsync();

        var results = await conn.QueryAsync<Product, Category, Product>(
            sql,
            (product, category) =>
            {
                product.Category = category;
                return product;
            },
            new { Id = id },
            splitOn: "id"
        );

        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
    {
        const string sql = """
            SELECT id, category_id AS CategoryId, sku, name, description,
                   unit_price AS UnitPrice, stock, is_active AS IsActive, created_at AS CreatedAt
            FROM products
            WHERE category_id = @CategoryId AND is_active = TRUE
            ORDER BY name
            """;

        using var conn = await _db.CreateConnectionAsync();
        return await conn.QueryAsync<Product>(sql, new { CategoryId = categoryId });
    }

    public async Task<PagedResult<Product>> GetAllAsync(int page, int pageSize, bool activeOnly = true)
    {
        var offset = (page - 1) * pageSize;

        var countSql = "SELECT COUNT(*) FROM products WHERE (@ActiveOnly = FALSE OR is_active = TRUE)";

        var dataSql = """
            SELECT id, category_id AS CategoryId, sku, name, description,
                   unit_price AS UnitPrice, stock, is_active AS IsActive, created_at AS CreatedAt
            FROM products
            WHERE (@ActiveOnly = FALSE OR is_active = TRUE)
            ORDER BY name
            LIMIT @PageSize OFFSET @Offset
            """;

        var parameters = new { ActiveOnly = activeOnly, PageSize = pageSize, Offset = offset };

        using var conn = await _db.CreateConnectionAsync();

        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await conn.QueryAsync<Product>(dataSql, parameters);

        return new PagedResult<Product>(items, total, page, pageSize);
    }
}
