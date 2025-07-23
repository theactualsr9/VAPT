using SecureApiVAPT.DTOs;
using SecureApiVAPT.Models;
using SecureApiVAPT.Data;
using Microsoft.Data.SqlClient;

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
        using var reader = await _context.ExecuteStoredProcedureAsync("sp_GetProductById", 
            _context.CreateParameter("@ProductId", id));
        
        if (await reader.ReadAsync())
        {
            return MapProductFromReader(reader);
        }
        return null;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        using var reader = await _context.ExecuteStoredProcedureAsync("sp_GetAllProducts");
        
        var products = new List<Product>();
        while (await reader.ReadAsync())
        {
            products.Add(MapProductFromReader(reader));
        }
        return products;
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
    {
        using var reader = await _context.ExecuteStoredProcedureAsync("sp_GetProductsByCategory", 
            _context.CreateParameter("@Category", category));
        
        var products = new List<Product>();
        while (await reader.ReadAsync())
        {
            products.Add(MapProductFromReader(reader));
        }
        return products;
    }

    public async Task<Product> CreateProductAsync(ProductDto productDto)
    {
        var product = new Product
        {
            Name = productDto.Name,
            Price = productDto.Price,
            Description = productDto.Description,
            StockQuantity = productDto.StockQuantity
        };

        var result = await _context.ExecuteStoredProcedureScalarAsync("sp_CreateProduct",
            _context.CreateParameter("@Name", product.Name),
            _context.CreateParameter("@Price", product.Price),
            _context.CreateParameter("@Description", product.Description),
            _context.CreateParameter("@StockQuantity", product.StockQuantity));

        if (result != null)
        {
            product.Id = Convert.ToInt32(result);
        }

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

        await _context.ExecuteStoredProcedureNonQueryAsync("sp_UpdateProduct",
            _context.CreateParameter("@ProductId", product.Id),
            _context.CreateParameter("@Name", product.Name),
            _context.CreateParameter("@Price", product.Price),
            _context.CreateParameter("@Description", product.Description),
            _context.CreateParameter("@StockQuantity", product.StockQuantity));

        _logger.LogInformation("Product updated: {ProductName}", product.Name);
        return true;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await GetProductByIdAsync(id);
        if (product == null) return false;

        await _context.ExecuteStoredProcedureNonQueryAsync("sp_DeleteProduct",
            _context.CreateParameter("@ProductId", id));

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

        await _context.ExecuteStoredProcedureNonQueryAsync("sp_UpdateProductStock",
            _context.CreateParameter("@ProductId", id),
            _context.CreateParameter("@Quantity", quantity));

        _logger.LogInformation("Stock updated for product {ProductName}: {Quantity}", product.Name, quantity);
        return true;
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllProductsAsync();

        using var reader = await _context.ExecuteStoredProcedureAsync("sp_SearchProducts",
            _context.CreateParameter("@SearchTerm", searchTerm));
        
        var products = new List<Product>();
        while (await reader.ReadAsync())
        {
            products.Add(MapProductFromReader(reader));
        }
        return products;
    }

    private static Product MapProductFromReader(SqlDataReader reader)
    {
        return new Product
        {
            Id = reader.GetInt32("Id"),
            Name = reader.GetString("Name"),
            Price = reader.GetDecimal("Price"),
            Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
            StockQuantity = reader.GetInt32("StockQuantity"),
            CreatedAt = reader.GetDateTime("CreatedAt"),
            UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt"),
            IsActive = reader.GetBoolean("IsActive")
        };
    }
} 