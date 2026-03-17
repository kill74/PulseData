using Microsoft.AspNetCore.Mvc;
using PulseData.Core.Interfaces;

namespace PulseData.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsRepository _analytics;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsRepository analytics, ILogger<AnalyticsController> logger)
    {
        _analytics = analytics;
        _logger = logger;
    }

    /// <summary>
    /// Monthly revenue summary with MoM growth percentage.
    /// </summary>
    [HttpGet("sales-summary")]
    public async Task<IActionResult> GetSalesSummary()
    {
        _logger.LogInformation("Fetching monthly sales summary");
        var result = await _analytics.GetMonthlySummaryAsync();
        return Ok(result);
    }

    /// <summary>
    /// Top-selling products by total revenue.
    /// </summary>
    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 10)
    {
        if (limit is < 1 or > 100)
            return BadRequest("Limit must be between 1 and 100.");

        var result = await _analytics.GetTopProductsAsync(limit);
        return Ok(result);
    }

    /// <summary>
    /// Customer stats ordered by lifetime value. Includes LTV quartile segmentation.
    /// </summary>
    [HttpGet("customer-stats")]
    public async Task<IActionResult> GetCustomerStats([FromQuery] int limit = 20)
    {
        if (limit is < 1 or > 200)
            return BadRequest("Limit must be between 1 and 200.");

        var result = await _analytics.GetCustomerStatsAsync(limit);
        return Ok(result);
    }

    /// <summary>
    /// Revenue and unit sales broken down by product category.
    /// </summary>
    [HttpGet("category-performance")]
    public async Task<IActionResult> GetCategoryPerformance()
    {
        var result = await _analytics.GetCategoryPerformanceAsync();
        return Ok(result);
    }

    /// <summary>
    /// Daily sales data for a custom date range.
    /// Defaults to the last 30 days if no dates are provided.
    /// </summary>
    [HttpGet("sales-by-period")]
    public async Task<IActionResult> GetSalesByPeriod(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var end   = endDate   ?? DateTime.UtcNow;
        var start = startDate ?? end.AddDays(-30);

        if (start > end)
            return BadRequest("startDate must be before endDate.");

        if ((end - start).TotalDays > 365)
            return BadRequest("Date range cannot exceed 365 days.");

        var result = await _analytics.GetSalesByPeriodAsync(start, end);
        return Ok(result);
    }
}
