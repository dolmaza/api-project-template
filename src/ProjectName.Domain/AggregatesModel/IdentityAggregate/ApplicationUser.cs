using Microsoft.AspNetCore.Identity;

namespace ProjectName.Domain.AggregatesModel.IdentityAggregate;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = null!;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
}