using System.ComponentModel.DataAnnotations;

namespace SecureApiVAPT.DTOs;

public class FileUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    public bool IsPublic { get; set; } = false;
}

public class FileUploadResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsPublic { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public class FileListResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsPublic { get; set; }
    public bool CanDownload { get; set; }
}

public class FileSearchRequest
{
    [StringLength(100)]
    public string? FileName { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    [StringLength(100)]
    public string? UploadedBy { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsPublic { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class FileUpdateRequest
{
    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    public bool? IsPublic { get; set; }
}

public class FileDownloadRequest
{
    [Required]
    public int FileId { get; set; }
}

public class FileDeleteRequest
{
    [Required]
    public int FileId { get; set; }
} 