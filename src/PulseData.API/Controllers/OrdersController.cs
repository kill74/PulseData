using Microsoft.AspNetCore.Mvc;
using PulseData.Core.Interfaces;

namespace PulseData.API.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orders;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderRepository orders, ILogger<OrdersController> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    /// <summary>
    /// Paginated list of all orders. Optionally filter by status.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        if (page < 1) return BadRequest("page must be >= 1.");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100.");

        var validStatuses = new[] { "pending", "confirmed", "shipped", "delivered", "cancelled", "refunded" };
        if (status != null && !validStatuses.Contains(status.ToLower()))
            return BadRequest($"Invalid status. Valid values: {string.Join(", ", validStatuses)}");

        var result = await _orders.GetAllAsync(page, pageSize, status?.ToLower());
        return Ok(result);
    }

    /// <summary>
    /// Full order detail including all line items.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orders.GetByIdAsync(id);

        if (order is null)
            return NotFound($"Order {id} not found.");

        return Ok(order);
    }

    /// <summary>
    /// All orders placed by a specific customer, newest first.
    /// </summary>
    [HttpGet("by-customer/{customerId:int}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var orders = await _orders.GetByCustomerAsync(customerId);
        return Ok(orders);
    }
}
