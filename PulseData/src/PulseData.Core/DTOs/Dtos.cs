namespace PulseData.Core.DTOs;

// ---------------------------------------------------------------------------
// Analytics DTOs
// ---------------------------------------------------------------------------

public record SalesSummaryDto(
    DateTime Month,
    int OrderCount,
    decimal Revenue,
    decimal AvgOrderValue,
    decimal? PrevMonthRevenue,
    decimal? MomGrowthPct
);

public record TopProductDto(
    int ProductId,
    string Sku,
    string ProductName,
    string Category,
    int TimesOrdered,
    int UnitsSold,
    decimal TotalRevenue,
    int RevenueRank
);

public record CustomerStatsDto(
    int CustomerId,
    string Email,
    string FullName,
    string? Country,
    int TotalOrders,
    decimal LifetimeValue,
    decimal AvgOrderValue,
    DateTime? FirstOrderAt,
    DateTime? LastOrderAt,
    int? DaysSinceLastOrder,
    int? LtvQuartile
);

public record CategoryPerformanceDto(
    int CategoryId,
    string Category,
    int OrderCount,
    int UnitsSold,
    decimal TotalRevenue,
    decimal AvgUnitPrice,
    decimal RevenueSharePct
);

public record DailySalesDto(
    DateTime SaleDate,
    long OrderCount,
    decimal Revenue,
    decimal AvgOrderValue
);

// ---------------------------------------------------------------------------
// Order DTOs
// ---------------------------------------------------------------------------

public record OrderSummaryDto(
    int Id,
    int CustomerId,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime PlacedAt
);

public record OrderDetailDto(
    int Id,
    int CustomerId,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime PlacedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    List<OrderItemDto> Items
);

public record OrderItemDto(
    int ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);

// ---------------------------------------------------------------------------
// Common
// ---------------------------------------------------------------------------

/// <summary>
/// Generic paginated result wrapper.
/// </summary>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPrevPage => Page > 1;
}
