using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchedulingApp.Application.Dtos;
using SchedulingApp.Application.Services;
using SchedulingApp.Domain.Constants;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvailabilityController(SchedulingService schedulingService) : ControllerBase
{
    [HttpGet("{professionalId:guid}/rules")]
    public async Task<IActionResult> GetRules(Guid professionalId, CancellationToken cancellationToken) =>
        Ok(await schedulingService.GetAvailabilityRulesAsync(professionalId, cancellationToken));

    [HttpGet("{professionalId:guid}/slots")]
    public async Task<IActionResult> GetSlots(Guid professionalId, [FromQuery] DateOnly date, CancellationToken cancellationToken) =>
        Ok(await schedulingService.GetAvailableSlotsAsync(professionalId, date, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("rules")]
    public async Task<IActionResult> UpsertRule(UpsertAvailabilityRuleRequest request, CancellationToken cancellationToken)
    {
        var professionalId = GetProfessionalId();
        if (professionalId is null)
        {
            return BadRequest(new { message = "Professional profile not found." });
        }

        try
        {
            return Ok(await schedulingService.UpsertAvailabilityRuleAsync(professionalId.Value, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("rules/{ruleId:guid}")]
    public async Task<IActionResult> DeleteRule(Guid ruleId, CancellationToken cancellationToken)
    {
        var professionalId = GetProfessionalId();
        if (professionalId is null)
        {
            return BadRequest(new { message = "Professional profile not found." });
        }

        try
        {
            await schedulingService.DeleteAvailabilityRuleAsync(professionalId.Value, ruleId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private Guid? GetProfessionalId()
    {
        var value = User.FindFirstValue("professional_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
