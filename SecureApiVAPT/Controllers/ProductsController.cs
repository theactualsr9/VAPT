using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureApiVAPT.DTOs;
using SecureApiVAPT.Services;
using System.ComponentModel.DataAnnotations;

namespace SecureApiVAPT.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _productService.GetAllProductsAsync();
        
        var productDtos = products.Select(p => new
        {
            p.Id,
            p.Name,
            p.Price,
            p.Description,
            p.StockQuantity,
            p.CreatedAt,
            p.UpdatedAt,
            p.IsActive
        });

        return Ok(productDtos);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { Error = "Product not found" });
        }

        return Ok(new
        {
            product.Id,
            product.Name,
            product.Price,
            product.Description,
            product.StockQuantity,
            product.CreatedAt,
            product.UpdatedAt,
            product.IsActive
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProduct([FromBody] ProductDto productDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var product = await _productService.CreateProductAsync(productDto);

        _logger.LogInformation("Product created by admin: {ProductName}", product.Name);

        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, new
        {
            product.Id,
            product.Name,
            product.Price,
            product.Description,
            product.StockQuantity,
            product.CreatedAt,
            product.IsActive
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto productDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _productService.UpdateProductAsync(id, productDto);
        if (!success)
        {
            return NotFound(new { Error = "Product not found" });
        }

        _logger.LogInformation("Product updated by admin: {ProductId}", id);

        return Ok(new { Message = "Product updated successfully" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var success = await _productService.DeleteProductAsync(id);
        if (!success)
        {
            return NotFound(new { Error = "Product not found" });
        }

        _logger.LogInformation("Product deleted by admin: {ProductId}", id);

        return Ok(new { Message = "Product deleted successfully" });
    }

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProducts([FromQuery] string? searchTerm)
    {
        var products = await _productService.SearchProductsAsync(searchTerm ?? string.Empty);
        
        var productDtos = products.Select(p => new
        {
            p.Id,
            p.Name,
            p.Price,
            p.Description,
            p.StockQuantity,
            p.CreatedAt,
            p.UpdatedAt
        });

        return Ok(productDtos);
    }

    [HttpGet("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductsByCategory(string category)
    {
        var products = await _productService.GetProductsByCategoryAsync(category);
        
        var productDtos = products.Select(p => new
        {
            p.Id,
            p.Name,
            p.Price,
            p.Description,
            p.StockQuantity,
            p.CreatedAt,
            p.UpdatedAt
        });

        return Ok(productDtos);
    }

    [HttpPatch("{id}/stock")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto updateStockDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _productService.UpdateStockAsync(id, updateStockDto.Quantity);
        if (!success)
        {
            return BadRequest(new { Error = "Failed to update stock. Product not found or insufficient stock." });
        }

        _logger.LogInformation("Stock updated by admin: {ProductId}, Quantity: {Quantity}", id, updateStockDto.Quantity);

        return Ok(new { Message = "Stock updated successfully" });
    }
}

public class UpdateStockDto
{
    [Required]
    public int Quantity { get; set; }
} 