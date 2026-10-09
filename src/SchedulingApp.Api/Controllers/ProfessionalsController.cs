using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchedulingApp.Application.Services;
using SchedulingApp.Domain.Constants;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfessionalsController(SchedulingService schedulingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await schedulingService.GetProfessionalsAsync(cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var professional = await schedulingService.GetProfessionalByUserIdAsync(userId, cancellationToken);
        return professional is null ? NotFound() : Ok(professional);
    }
}
