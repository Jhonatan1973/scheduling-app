using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Application.Common;
using SchedulingApp.Contracts.Availability;
using SchedulingApp.Domain;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Domain.Enums;
using SchedulingApp.Domain.Scheduling;

namespace SchedulingApp.Application.Availability;

public sealed class AvailabilityService(IAppDbContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<AvailabilityRuleDto>> GetRulesAsync(Guid professionalId, CancellationToken ct = default)
    {
        var rules = await db.AvailabilityRules.AsNoTracking()
            .Where(r => r.ProfessionalId == professionalId)
            .ToListAsync(ct);

        return rules
            .OrderBy(r => ((int)r.DayOfWeek + 6) % 7) // Monday first
            .ThenBy(r => r.StartTime)
            .Select(r => r.ToDto())
            .ToList();
    }

    public async Task<AvailabilityRuleDto> AddRuleAsync(Actor actor, CreateAvailabilityRuleRequest request, CancellationToken ct = default)
    {
        var professionalId = actor.RequireProfessionalId();

        if (!Enum.IsDefined(request.DayOfWeek))
            throw ValidationException.For(nameof(request.DayOfWeek), "Invalid day of week.");

        var existing = await db.AvailabilityRules
            .Where(r => r.ProfessionalId == professionalId && r.DayOfWeek == request.DayOfWeek)
            .ToListAsync(ct);

        var rule = AvailabilityRule.Create(professionalId, request.DayOfWeek, request.StartTime, request.EndTime, existing);
        db.AvailabilityRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule.ToDto();
    }

    public async Task DeleteRuleAsync(Actor actor, Guid ruleId, CancellationToken ct = default)
    {
        var professionalId = actor.RequireProfessionalId();
        var rule = await db.AvailabilityRules.FirstOrDefaultAsync(r => r.Id == ruleId && r.ProfessionalId == professionalId, ct)
            ?? throw new NotFoundException("Availability rule");

        db.AvailabilityRules.Remove(rule);
        await db.SaveChangesAsync(ct);
    }

    public async Task<DaySlotsDto> GetSlotsAsync(Guid professionalId, DateOnly date, CancellationToken ct = default)
    {
        var professional = await db.Professionals.AsNoTracking().FirstOrDefaultAsync(p => p.Id == professionalId, ct)
            ?? throw new NotFoundException("Professional");

        var slots = await ComputeSlotsAsync(professional, date, ct);
        var tz = professional.GetTimeZone();

        return new DaySlotsDto(
            date,
            professional.TimeZoneId,
            slots.Select(s => new SlotDto(s.StartUtc.ToZoned(tz), s.EndUtc.ToZoned(tz), s.IsAvailable)).ToList());
    }

    /// <summary>Shared by the slot listing and the booking use case so both apply exactly the same rules.</summary>
    internal async Task<IReadOnlyList<Slot>> ComputeSlotsAsync(Professional professional, DateOnly localDate, CancellationToken ct)
    {
        var tz = professional.GetTimeZone();
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var todayLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz));

        if (localDate < todayLocal || localDate > todayLocal.AddDays(BookingPolicy.MaxDaysAhead))
            return [];

        var rules = await db.AvailabilityRules.AsNoTracking()
            .Where(r => r.ProfessionalId == professional.Id && r.DayOfWeek == localDate.DayOfWeek)
            .ToListAsync(ct);

        if (rules.Count == 0)
            return [];

        var day = SlotCalculator.LocalDayToUtcRange(localDate, tz);
        var busy = await db.Appointments.AsNoTracking()
            .Where(a => a.ProfessionalId == professional.Id
                        && a.Status != AppointmentStatus.Cancelled
                        && a.StartUtc < day.EndUtc
                        && a.EndUtc > day.StartUtc)
            .Select(a => new { a.StartUtc, a.EndUtc })
            .ToListAsync(ct);

        return SlotCalculator.Generate(
            localDate,
            rules,
            professional.SlotDurationMinutes,
            tz,
            busy.Select(b => new TimeRange(b.StartUtc, b.EndUtc)),
            nowUtc);
    }
}
