using System.ComponentModel.DataAnnotations;

namespace SecureApiVAPT.Models;

public class FileUpload
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [Required]
    [StringLength(100)]
    public string UploadedBy { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    public bool IsPublic { get; set; } = false;

    public string? Checksum { get; set; }
} 