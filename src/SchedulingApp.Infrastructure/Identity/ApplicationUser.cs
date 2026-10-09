using Microsoft.AspNetCore.Identity;

namespace SchedulingApp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public required string FullName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
