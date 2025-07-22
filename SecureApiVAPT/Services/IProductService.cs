using SecureApiVAPT.DTOs;
using SecureApiVAPT.Models;

namespace SecureApiVAPT.Services;

public interface IProductService
{
    Task<Product?> GetProductByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category);
    Task<Product> CreateProductAsync(ProductDto productDto);
    Task<bool> UpdateProductAsync(int id, ProductDto productDto);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> UpdateStockAsync(int id, int quantity);
    Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
} 