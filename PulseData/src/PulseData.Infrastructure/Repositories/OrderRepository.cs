using Dapper;
using PulseData.Core.DTOs;
using PulseData.Core.Interfaces;
using PulseData.Core.Models;
using PulseData.Infrastructure.Data;

namespace PulseData.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly DbConnectionFactory _db;

    public OrderRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    /// <summary>
    /// Fetches an order with its line items and customer name in a single query
    /// using Dapper's multi-mapping.
    /// </summary>
    public async Task<Order?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT
                o.id, o.customer_id, o.status, o.total_amount, o.currency,
                o.placed_at, o.shipped_at, o.delivered_at,
                c.id, c.email, c.first_name, c.last_name, c.country,
                oi.id, oi.order_id, oi.product_id, oi.quantity, oi.unit_price, oi.subtotal,
                p.id, p.sku, p.name
            FROM orders o
            JOIN customers c    ON c.id = o.customer_id
            JOIN order_items oi ON oi.order_id = o.id
            JOIN products p     ON p.id = oi.product_id
            WHERE o.id = @Id
            """;

        using var conn = await _db.CreateConnectionAsync();

        Order? result = null;

        await conn.QueryAsync<Order, Customer, OrderItem, Product, Order>(
            sql,
            (order, customer, item, product) =>
            {
                result ??= order;
                result.Customer = customer;

                item.Product = product;
                result.Items.Add(item);

                return result;
            },
            new { Id = id },
            splitOn: "id,id,id"
        );

        return result;
    }

    /// <summary>
    /// Returns paginated orders with customer name. Optionally filters by status.
    /// </summary>
    public async Task<PagedResult<OrderSummaryDto>> GetAllAsync(
        int page,
        int pageSize,
        string? status = null)
    {
        var offset = (page - 1) * pageSize;

        var countSql = """
            SELECT COUNT(*)
            FROM orders o
            WHERE (@Status IS NULL OR o.status = @Status)
            """;

        var dataSql = """
            SELECT
                o.id            AS Id,
                o.customer_id   AS CustomerId,
                c.first_name || ' ' || c.last_name AS CustomerName,
                o.status        AS Status,
                o.total_amount  AS TotalAmount,
                o.currency      AS Currency,
                o.placed_at     AS PlacedAt
            FROM orders o
            JOIN customers c ON c.id = o.customer_id
            WHERE (@Status IS NULL OR o.status = @Status)
            ORDER BY o.placed_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        var parameters = new { Status = status, PageSize = pageSize, Offset = offset };

        using var conn = await _db.CreateConnectionAsync();

        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await conn.QueryAsync<OrderSummaryDto>(dataSql, parameters);

        return new PagedResult<OrderSummaryDto>(items, total, page, pageSize);
    }

    public async Task<IEnumerable<OrderSummaryDto>> GetByCustomerAsync(int customerId)
    {
        const string sql = """
            SELECT
                o.id            AS Id,
                o.customer_id   AS CustomerId,
                c.first_name || ' ' || c.last_name AS CustomerName,
                o.status        AS Status,
                o.total_amount  AS TotalAmount,
                o.currency      AS Currency,
                o.placed_at     AS PlacedAt
            FROM orders o
            JOIN customers c ON c.id = o.customer_id
            WHERE o.customer_id = @CustomerId
            ORDER BY o.placed_at DESC
            """;

        using var conn = await _db.CreateConnectionAsync();
        return await conn.QueryAsync<OrderSummaryDto>(sql, new { CustomerId = customerId });
    }
}
