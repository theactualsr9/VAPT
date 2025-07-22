using SecureApiVAPT.DTOs;
using SecureApiVAPT.Models;

namespace SecureApiVAPT.Services;

public class ProductService : IProductService
{
    private readonly ILogger<ProductService> _logger;
    private readonly List<Product> _products = new(); // In-memory storage for demo

    public ProductService(ILogger<ProductService> logger)
    {
        _logger = logger;
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await Task.FromResult(_products.FirstOrDefault(p => p.Id == id && p.IsActive));
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await Task.FromResult(_products.Where(p => p.IsActive));
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
    {
        // For demo purposes, filtering by name containing category
        return await Task.FromResult(_products.Where(p => p.IsActive && p.Name.Contains(category, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<Product> CreateProductAsync(ProductDto productDto)
    {
        var product = new Product
        {
            Id = _products.Count + 1,
            Name = productDto.Name,
            Price = productDto.Price,
            Description = productDto.Description,
            StockQuantity = productDto.StockQuantity
        };

        _products.Add(product);
        _logger.LogInformation("Product created: {ProductName}", product.Name);
        
        return await Task.FromResult(product);
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

        _logger.LogInformation("Product updated: {ProductName}", product.Name);
        return true;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await GetProductByIdAsync(id);
        if (product == null) return false;

        product.IsActive = false;
        _logger.LogInformation("Product deactivated: {ProductName}", product.Name);
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

        _logger.LogInformation("Stock updated for product {ProductName}: {Quantity}", product.Name, quantity);
        return true;
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllProductsAsync();

        return await Task.FromResult(_products.Where(p => p.IsActive && 
            (p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
             p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))));
    }
} 