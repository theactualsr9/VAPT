using Microsoft.EntityFrameworkCore;
using SecureApiVAPT.DTOs;
using SecureApiVAPT.Models;
using SecureApiVAPT.Data;

namespace SecureApiVAPT.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _context.Products.Where(p => p.IsActive).ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
    {
        // Since Product model doesn't have Category, just return all products for now
        return await GetAllProductsAsync();
    }

    public async Task<Product> CreateProductAsync(ProductDto productDto)
    {
        var product = new Product
        {
            Name = productDto.Name,
            Price = productDto.Price,
            Description = productDto.Description,
            StockQuantity = productDto.StockQuantity,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductName}", product.Name);
        return product;
    }

    public async Task<bool> UpdateProductAsync(int id, ProductDto productDto)
    {
        var product = await GetProductByIdAsync(id);
        if (product == null) return false;

        product.Name = productDto.Name;
        product.Price = productDto.Price;
        product.Description = productDto.Description;
        product.StockQuantity = productDto.StockQuantity;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product updated: {ProductName}", product.Name);
        return true;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await GetProductByIdAsync(id);
        if (product == null) return false;

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product deleted: {ProductName}", product.Name);
        return true;
    }

    public async Task<bool> UpdateStockAsync(int id, int quantity)
    {
        var product = await GetProductByIdAsync(id);
        if (product == null) return false;

        if (product.StockQuantity + quantity < 0)
        {
            _logger.LogWarning("Insufficient stock for product: {ProductName}", product.Name);
            return false;
        }

        product.StockQuantity += quantity;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock updated for product {ProductName}: {Quantity}", product.Name, quantity);
        return true;
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllProductsAsync();

        return await _context.Products
            .Where(p => p.IsActive && (p.Name.Contains(searchTerm) || 
                       (p.Description != null && p.Description.Contains(searchTerm))))
            .ToListAsync();
    }
} 