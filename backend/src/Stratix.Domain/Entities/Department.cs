namespace Stratix.Domain.Entities;

public class Department : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
