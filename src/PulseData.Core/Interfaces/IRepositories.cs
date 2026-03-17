using PulseData.Core.DTOs;
using PulseData.Core.Models;

namespace PulseData.Core.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<PagedResult<Customer>> GetAllAsync(int page, int pageSize);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<PagedResult<Product>> GetAllAsync(int page, int pageSize, bool activeOnly = true);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<IEnumerable<OrderSummaryDto>> GetByCustomerAsync(int customerId);
    Task<PagedResult<OrderSummaryDto>> GetAllAsync(int page, int pageSize, string? status = null);
}

public interface IAnalyticsRepository
{
    Task<IEnumerable<SalesSummaryDto>> GetMonthlySummaryAsync();
    Task<IEnumerable<TopProductDto>> GetTopProductsAsync(int limit);
    Task<IEnumerable<CustomerStatsDto>> GetCustomerStatsAsync(int limit);
    Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceAsync();
    Task<IEnumerable<DailySalesDto>> GetSalesByPeriodAsync(DateTime startDate, DateTime endDate);
}
