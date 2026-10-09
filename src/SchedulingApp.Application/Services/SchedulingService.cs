using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Application.Contracts.Messaging;
using SchedulingApp.Application.Dtos;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Domain.Enums;

namespace SchedulingApp.Application.Services;

public interface ISchedulingDbContext
{
    DbSet<Professional> Professionals { get; }
    DbSet<AvailabilityRule> AvailabilityRules { get; }
    DbSet<Appointment> Appointments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class SchedulingService(
    ISchedulingDbContext db,
    ISlotCache slotCache,
    INotificationPublisher notificationPublisher)
{
    public async Task<ProfessionalDto?> GetProfessionalByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await db.Professionals
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new ProfessionalDto(p.Id, p.DisplayName, p.Specialty, p.SlotDurationMinutes))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProfessionalDto>> GetProfessionalsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Professionals
            .AsNoTracking()
            .OrderBy(p => p.DisplayName)
            .Select(p => new ProfessionalDto(p.Id, p.DisplayName, p.Specialty, p.SlotDurationMinutes))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AvailabilityRuleDto>> GetAvailabilityRulesAsync(
        Guid professionalId,
        CancellationToken cancellationToken = default)
    {
        return await db.AvailabilityRules
            .AsNoTracking()
            .Where(r => r.ProfessionalId == professionalId)
            .OrderBy(r => r.DayOfWeek)
            .ThenBy(r => r.StartTime)
            .Select(r => new AvailabilityRuleDto(r.Id, r.DayOfWeek, r.StartTime, r.EndTime))
            .ToListAsync(cancellationToken);
    }

    public async Task<AvailabilityRuleDto> UpsertAvailabilityRuleAsync(
        Guid professionalId,
        UpsertAvailabilityRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndTime <= request.StartTime)
        {
            throw new InvalidOperationException("End time must be after start time.");
        }

        var rule = await db.AvailabilityRules
            .FirstOrDefaultAsync(r => r.ProfessionalId == professionalId && r.DayOfWeek == request.DayOfWeek, cancellationToken);

        if (rule is null)
        {
            rule = new AvailabilityRule
            {
                Id = Guid.NewGuid(),
                ProfessionalId = professionalId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };
            db.AvailabilityRules.Add(rule);
        }
        else
        {
            rule.StartTime = request.StartTime;
            rule.EndTime = request.EndTime;
        }

        await db.SaveChangesAsync(cancellationToken);
        await InvalidateWeekCacheAsync(professionalId, request.DayOfWeek, cancellationToken);

        return new AvailabilityRuleDto(rule.Id, rule.DayOfWeek, rule.StartTime, rule.EndTime);
    }

    public async Task DeleteAvailabilityRuleAsync(Guid professionalId, Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await db.AvailabilityRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.ProfessionalId == professionalId, cancellationToken)
            ?? throw new KeyNotFoundException("Availability rule not found.");

        db.AvailabilityRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
        await InvalidateWeekCacheAsync(professionalId, rule.DayOfWeek, cancellationToken);
    }

    public async Task<IReadOnlyList<TimeSlotDto>> GetAvailableSlotsAsync(
        Guid professionalId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var cached = await slotCache.GetAsync(professionalId, date, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var professional = await db.Professionals
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == professionalId, cancellationToken)
            ?? throw new KeyNotFoundException("Professional not found.");

        var rules = await db.AvailabilityRules
            .AsNoTracking()
            .Where(r => r.ProfessionalId == professionalId)
            .ToListAsync(cancellationToken);

        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var appointments = await db.Appointments
            .AsNoTracking()
            .Where(a => a.ProfessionalId == professionalId && a.StartUtc >= dayStart && a.StartUtc < dayEnd)
            .ToListAsync(cancellationToken);

        var slots = SlotGenerator.Generate(date, professional, rules, appointments);
        await slotCache.SetAsync(professionalId, date, slots, cancellationToken);
        return slots;
    }

    public async Task<AppointmentDto> BookAsync(
        string clientUserId,
        string clientName,
        string clientEmail,
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var professional = await db.Professionals
            .FirstOrDefaultAsync(p => p.Id == request.ProfessionalId, cancellationToken)
            ?? throw new KeyNotFoundException("Professional not found.");

        var date = DateOnly.FromDateTime(request.StartUtc);
        var slots = await GetAvailableSlotsAsync(professional.Id, date, cancellationToken);
        var slot = slots.FirstOrDefault(s => s.StartUtc == request.StartUtc && s.IsAvailable)
            ?? throw new InvalidOperationException("Selected time slot is not available.");

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professional.Id,
            ClientUserId = clientUserId,
            ClientName = clientName,
            ClientEmail = clientEmail,
            StartUtc = slot.StartUtc,
            EndUtc = slot.EndUtc,
            Status = AppointmentStatus.Confirmed,
            Notes = request.Notes
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);
        await slotCache.InvalidateAsync(professional.Id, date, cancellationToken);

        await notificationPublisher.PublishAsync(
            new AppointmentNotificationMessage(
                appointment.Id,
                "Booked",
                clientEmail,
                clientName,
                professional.DisplayName,
                appointment.StartUtc,
                appointment.EndUtc),
            cancellationToken);

        return ToDto(appointment, professional.DisplayName);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetClientAppointmentsAsync(
        string clientUserId,
        CancellationToken cancellationToken = default)
    {
        return await db.Appointments
            .AsNoTracking()
            .Include(a => a.Professional)
            .Where(a => a.ClientUserId == clientUserId)
            .OrderByDescending(a => a.StartUtc)
            .Select(a => new AppointmentDto(
                a.Id,
                a.ProfessionalId,
                a.Professional!.DisplayName,
                a.ClientName,
                a.StartUtc,
                a.EndUtc,
                a.Status,
                a.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetProfessionalAppointmentsAsync(
        Guid professionalId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var query = db.Appointments
            .AsNoTracking()
            .Include(a => a.Professional)
            .Where(a => a.ProfessionalId == professionalId);

        if (from.HasValue)
        {
            var fromUtc = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.StartUtc >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = to.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(a => a.StartUtc <= toUtc);
        }

        return await query
            .OrderBy(a => a.StartUtc)
            .Select(a => new AppointmentDto(
                a.Id,
                a.ProfessionalId,
                a.Professional!.DisplayName,
                a.ClientName,
                a.StartUtc,
                a.EndUtc,
                a.Status,
                a.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<DashboardSummaryDto> GetDashboardAsync(
        Guid professionalId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekEnd = today.AddDays(7);

        var appointments = await GetProfessionalAppointmentsAsync(professionalId, today, weekEnd, cancellationToken);
        var todayCount = appointments.Count(a => DateOnly.FromDateTime(a.StartUtc) == today && a.Status != AppointmentStatus.Cancelled);
        var weekCount = appointments.Count(a => a.Status != AppointmentStatus.Cancelled);
        var upcoming = appointments
            .Where(a => a.StartUtc >= DateTime.UtcNow && a.Status != AppointmentStatus.Cancelled)
            .Take(10)
            .ToList();

        return new DashboardSummaryDto(todayCount, weekCount, upcoming);
    }

    public async Task CancelAsync(
        Guid appointmentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var appointment = await db.Appointments
            .Include(a => a.Professional)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (!isAdmin && appointment.ClientUserId != userId)
        {
            throw new UnauthorizedAccessException("You cannot cancel this appointment.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return;
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);

        var date = DateOnly.FromDateTime(appointment.StartUtc);
        await slotCache.InvalidateAsync(appointment.ProfessionalId, date, cancellationToken);

        await notificationPublisher.PublishAsync(
            new AppointmentNotificationMessage(
                appointment.Id,
                "Cancelled",
                appointment.ClientEmail,
                appointment.ClientName,
                appointment.Professional!.DisplayName,
                appointment.StartUtc,
                appointment.EndUtc),
            cancellationToken);
    }

    private async Task InvalidateWeekCacheAsync(Guid professionalId, DayOfWeek dayOfWeek, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < 28; i++)
        {
            var date = today.AddDays(i);
            if (date.DayOfWeek == dayOfWeek)
            {
                await slotCache.InvalidateAsync(professionalId, date, cancellationToken);
            }
        }
    }

    private static AppointmentDto ToDto(Appointment appointment, string professionalName) =>
        new(
            appointment.Id,
            appointment.ProfessionalId,
            professionalName,
            appointment.ClientName,
            appointment.StartUtc,
            appointment.EndUtc,
            appointment.Status,
            appointment.Notes);
}
