using Dapper;
using PulseData.Core.DTOs;
using PulseData.Core.Interfaces;
using PulseData.Infrastructure.Data;

namespace PulseData.Infrastructure.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly DbConnectionFactory _db;

    public AnalyticsRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    /// <summary>
    /// Pulls from the monthly_revenue_summary view — includes MoM growth %.
    /// </summary>
    public async Task<IEnumerable<SalesSummaryDto>> GetMonthlySummaryAsync()
    {
        const string sql = """
            SELECT
                month               AS Month,
                order_count         AS OrderCount,
                revenue             AS Revenue,
                avg_order_value     AS AvgOrderValue,
                prev_month_revenue  AS PrevMonthRevenue,
                mom_growth_pct      AS MomGrowthPct
            FROM monthly_revenue_summary
            ORDER BY month DESC
            """;

        using var conn = await _db.CreateConnectionAsync();
        return await conn.QueryAsync<SalesSummaryDto>(sql);
    }

    /// <summary>
    /// Top N products by total revenue, with category and units sold.
    /// </summary>
    public async Task<IEnumerable<TopProductDto>> GetTopProductsAsync(int limit)
    {
        const string sql = """
            SELECT
                product_id      AS ProductId,
                sku             AS Sku,
                product_name    AS ProductName,
                category        AS Category,
                times_ordered   AS TimesOrdered,
                units_sold      AS UnitsSold,
                total_revenue   AS TotalRevenue,
                revenue_rank    AS RevenueRank
            FROM product_sales_ranking
            LIMIT @Limit
            """;

        using var conn = await _db.CreateConnectionAsync();
        return await conn.QueryAsync<TopProductDto>(sql, new { Limit = limit });
    }

    /// <summary>
    /// Customer stats from the customer_lifetime_value view.
    /// Returns top N customers by LTV.
    /// </summary>
    public async Task<IEnumerable<CustomerStatsDto>> GetCustomerStatsAsync(int limit)
    {
        const string sql = """
            SELECT
                customer_id             AS CustomerId,
                email                   AS Email,
                full_name               AS FullName,
                country                 AS Country,
                total_orders            AS TotalOrders,
                lifetime_value          AS LifetimeValue,
                avg_order_value         AS AvgOrderValue,
                first_order_at          AS FirstOrderAt,
                last_order_at           AS LastOrderAt,
                days_since_last_order   AS DaysSinceLastOrder,
                ltv_quartile            AS LtvQuartile
            FROM customer_lifetime_value
            LIMIT @Limit
            """;

        using var conn = await _db.CreateConnectionAsync();
        return await conn.QueryAsync<CustomerStatsDto>(sql, new { Limit = limit });
    }

    /// <summary>
    /// Category revenue breakdown with market share percentages.
    /// </summary>
    public async Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceAsync()
    {
        const string sql = """
            SELECT
                category_id         AS CategoryId,
                category            AS Category,
                order_count         AS OrderCount,
                units_sold          AS UnitsSold,
                total_revenue       AS TotalRevenue,
                avg_unit_price      AS AvgUnitPrice,
                revenue_share_pct   AS RevenueSharePct
            FROM category_performance
            WHERE total_revenue IS NOT NULL
            ORDER BY total_revenue DESC
            """;

        using var conn = await _db.CreateConnectionAsync();
        return await conn.QueryAsync<CategoryPerformanceDto>(sql);
    }

    /// <summary>
    /// Calls the get_sales_by_period stored function for daily granularity.
    /// </summary>
    public async Task<IEnumerable<DailySalesDto>> GetSalesByPeriodAsync(
        DateTime startDate,
        DateTime endDate)
    {
        const string sql = """
            SELECT
                sale_date           AS SaleDate,
                order_count         AS OrderCount,
                revenue             AS Revenue,
                avg_order_value     AS AvgOrderValue
            FROM get_sales_by_period(@StartDate, @EndDate)
            """;

        using var conn = await _db.CreateConnectionAsync();
        return await conn.QueryAsync<DailySalesDto>(sql, new { StartDate = startDate, EndDate = endDate });
    }
}
