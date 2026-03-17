using Microsoft.AspNetCore.Mvc;
using PulseData.Core.Interfaces;

namespace PulseData.API.Controllers;

[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customers;

    public CustomersController(ICustomerRepository customers)
    {
        _customers = customers;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) return BadRequest("page must be >= 1.");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100.");

        var result = await _customers.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customers.GetByIdAsync(id);

        if (customer is null)
            return NotFound($"Customer {id} not found.");

        return Ok(customer);
    }
}

// ---------------------------------------------------------------------------

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _products;

    public ProductsController(IProductRepository products)
    {
        _products = products;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool activeOnly = true)
    {
        if (page < 1) return BadRequest("page must be >= 1.");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100.");

        var result = await _products.GetAllAsync(page, pageSize, activeOnly);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _products.GetByIdAsync(id);

        if (product is null)
            return NotFound($"Product {id} not found.");

        return Ok(product);
    }

    [HttpGet("by-category/{categoryId:int}")]
    public async Task<IActionResult> GetByCategory(int categoryId)
    {
        var products = await _products.GetByCategoryAsync(categoryId);
        return Ok(products);
    }
}
