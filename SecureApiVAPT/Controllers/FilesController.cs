using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureApiVAPT.Data;
using SecureApiVAPT.DTOs;
using SecureApiVAPT.Models;
using System.Security.Cryptography;

namespace SecureApiVAPT.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FilesController> _logger;
    private readonly IConfiguration _configuration;

    public FilesController(ApplicationDbContext context, ILogger<FilesController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile([FromForm] FileUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("No file provided");

        var maxFileSize = _configuration.GetValue<long>("FileUpload:MaxFileSize", 10 * 1024 * 1024);
        if (request.File.Length > maxFileSize)
            return BadRequest("File too large");

        var allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? new[] { ".jpg", ".pdf", ".txt" };
        var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest("File type not allowed");

        var uploadPath = _configuration.GetValue<string>("FileUpload:UploadPath", "uploads");
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadPath, fileName);

        Directory.CreateDirectory(uploadPath);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        var checksum = await CalculateChecksum(filePath);
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        var fileUpload = new FileUpload
        {
            FileName = fileName,
            OriginalFileName = request.File.FileName,
            ContentType = request.File.ContentType,
            FilePath = filePath,
            FileSize = request.File.Length,
            UploadedBy = userId,
            Description = request.Description,
            Category = request.Category,
            IsPublic = request.IsPublic,
            Checksum = checksum
        };

        _context.FileUploads.Add(fileUpload);
        await _context.SaveChangesAsync();

        _logger.LogInformation("File uploaded: {FileName} by {UserId}", fileName, userId);

        var response = new FileUploadResponse
        {
            Id = fileUpload.Id,
            FileName = fileUpload.FileName,
            OriginalFileName = fileUpload.OriginalFileName,
            ContentType = fileUpload.ContentType,
            FileSize = fileUpload.FileSize,
            UploadedBy = fileUpload.UploadedBy,
            UploadedAt = fileUpload.UploadedAt,
            Description = fileUpload.Description,
            Category = fileUpload.Category,
            IsPublic = fileUpload.IsPublic,
            DownloadUrl = $"/api/v1/files/download/{fileUpload.Id}"
        };

        return CreatedAtAction(nameof(GetFile), new { id = fileUpload.Id }, response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(int id)
    {
        var file = await _context.FileUploads.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
        if (file == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!file.IsPublic && file.UploadedBy != userId)
            return Forbid();

        var response = new FileListResponse
        {
            Id = file.Id,
            FileName = file.FileName,
            OriginalFileName = file.OriginalFileName,
            ContentType = file.ContentType,
            FileSize = file.FileSize,
            UploadedBy = file.UploadedBy,
            UploadedAt = file.UploadedAt,
            Description = file.Description,
            Category = file.Category,
            IsPublic = file.IsPublic,
            CanDownload = file.IsPublic || file.UploadedBy == userId
        };

        return Ok(response);
    }

    [HttpGet("download/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var file = await _context.FileUploads.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
        if (file == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!file.IsPublic && file.UploadedBy != userId)
            return Forbid();

        if (!System.IO.File.Exists(file.FilePath))
            return NotFound("File not found on disk");

        var fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
        return File(fileBytes, file.ContentType, file.OriginalFileName);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiles([FromQuery] FileSearchRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var query = _context.FileUploads.Where(f => f.IsActive);

        // Filter by access rights
        query = query.Where(f => f.IsPublic || f.UploadedBy == userId);

        // Apply filters
        if (!string.IsNullOrEmpty(request.FileName))
            query = query.Where(f => f.OriginalFileName.Contains(request.FileName));

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(f => f.Category == request.Category);

        if (!string.IsNullOrEmpty(request.UploadedBy))
            query = query.Where(f => f.UploadedBy == request.UploadedBy);

        if (request.FromDate.HasValue)
            query = query.Where(f => f.UploadedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(f => f.UploadedAt <= request.ToDate.Value);

        if (request.IsPublic.HasValue)
            query = query.Where(f => f.IsPublic == request.IsPublic.Value);

        // Pagination
        var totalCount = await query.CountAsync();
        var files = await query
            .OrderByDescending(f => f.UploadedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var response = files.Select(f => new FileListResponse
        {
            Id = f.Id,
            FileName = f.FileName,
            OriginalFileName = f.OriginalFileName,
            ContentType = f.ContentType,
            FileSize = f.FileSize,
            UploadedBy = f.UploadedBy,
            UploadedAt = f.UploadedAt,
            Description = f.Description,
            Category = f.Category,
            IsPublic = f.IsPublic,
            CanDownload = f.IsPublic || f.UploadedBy == userId
        });

        return Ok(new { Files = response, TotalCount = totalCount, Page = request.Page, PageSize = request.PageSize });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(int id)
    {
        var file = await _context.FileUploads.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
        if (file == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (file.UploadedBy != userId)
            return Forbid();

        file.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("File deleted: {FileName} by {UserId}", file.FileName, userId);

        return Ok(new { Message = "File deleted successfully" });
    }

    private static async Task<string> CalculateChecksum(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = System.IO.File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToBase64String(hash);
    }
} 