namespace Stratix.Domain.Entities;

/// <summary>Metadata for a file attached to a project (the binary lives in external storage).</summary>
public class ProjectFile : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long ProjectId { get; set; }
    public Project? Project { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string Url { get; set; } = string.Empty;
    public long UploadedById { get; set; }
    public User? UploadedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
