using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SecureApiVAPT.Data;

public class ApplicationDbContext : DbContext
{
    private readonly string _connectionString;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) : base(options)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found");
    }

    // Stored Procedure Execution Methods
    public async Task<SqlDataReader> ExecuteStoredProcedureAsync(string procedureName, params SqlParameter[] parameters)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = new SqlCommand($"EXEC {procedureName}", connection);
        command.Parameters.AddRange(parameters);
        
        return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
    }

    public async Task<int> ExecuteStoredProcedureNonQueryAsync(string procedureName, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        using var command = new SqlCommand($"EXEC {procedureName}", connection);
        command.Parameters.AddRange(parameters);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<object> ExecuteStoredProcedureScalarAsync(string procedureName, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        using var command = new SqlCommand($"EXEC {procedureName}", connection);
        command.Parameters.AddRange(parameters);
        
        return await command.ExecuteScalarAsync();
    }

    // Helper methods for common operations
    public SqlParameter CreateParameter(string name, object value)
    {
        return new SqlParameter(name, value ?? DBNull.Value);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations for EF migrations only
        modelBuilder.Entity<Models.User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Age).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<Models.Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<Models.FileUpload>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FileSize).IsRequired();
            entity.Property(e => e.UploadedBy).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UploadedAt).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.UploadedBy);
            entity.HasIndex(e => e.UploadedAt);
        });

        modelBuilder.Entity<Models.AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(50);
            entity.Property(e => e.UserName).HasMaxLength(100);
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.OldValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => e.Timestamp);
        });
    }
} 