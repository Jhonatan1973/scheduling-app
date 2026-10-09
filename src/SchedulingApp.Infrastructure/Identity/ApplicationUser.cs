using Microsoft.AspNetCore.Identity;

namespace SchedulingApp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public required string FullName { get; set; }
}
