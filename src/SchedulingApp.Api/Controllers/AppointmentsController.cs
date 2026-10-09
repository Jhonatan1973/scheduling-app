using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchedulingApp.Application.Dtos;
using SchedulingApp.Application.Services;
using SchedulingApp.Domain.Constants;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController(SchedulingService schedulingService) : ControllerBase
{
    [Authorize(Roles = Roles.Client)]
    [HttpPost]
    public async Task<IActionResult> Book(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name = User.FindFirstValue(ClaimTypes.Name) ?? "Client";

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var appointment = await schedulingService.BookAsync(userId, name, email, request, cancellationToken);
            return CreatedAtAction(nameof(GetMine), appointment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (User.IsInRole(Roles.Admin))
        {
            var professionalId = GetProfessionalId();
            if (professionalId is null)
            {
                return BadRequest(new { message = "Professional profile not found." });
            }

            return Ok(await schedulingService.GetProfessionalAppointmentsAsync(professionalId.Value, null, null, cancellationToken));
        }

        return Ok(await schedulingService.GetClientAppointmentsAsync(userId, cancellationToken));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var professionalId = GetProfessionalId();
        if (professionalId is null)
        {
            return BadRequest(new { message = "Professional profile not found." });
        }

        return Ok(await schedulingService.GetDashboardAsync(professionalId.Value, cancellationToken));
    }

    [HttpPost("{appointmentId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid appointmentId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            await schedulingService.CancelAsync(appointmentId, userId, User.IsInRole(Roles.Admin), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    private Guid? GetProfessionalId()
    {
        var value = User.FindFirstValue("professional_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
