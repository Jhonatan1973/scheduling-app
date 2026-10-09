using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Application.Availability;
using SchedulingApp.Application.Common;
using SchedulingApp.Application.Notifications;
using SchedulingApp.Contracts.Appointments;
using SchedulingApp.Domain;
using SchedulingApp.Domain.Common;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Domain.Enums;
using SchedulingApp.Domain.Scheduling;

namespace SchedulingApp.Application.Appointments;

public sealed class AppointmentService(
    IAppDbContext db,
    AvailabilityService availability,
    AppointmentNotifier notifier,
    TimeProvider clock)
{
    public async Task<AppointmentDto> BookAsync(Actor actor, BookAppointmentRequest request, CancellationToken ct = default)
    {
        actor.RequireClient();

        var professional = await db.Professionals.FirstOrDefaultAsync(p => p.Id == request.ProfessionalId, ct)
            ?? throw new NotFoundException("Professional");

        var startUtc = request.Start.UtcDateTime;
        var tz = professional.GetTimeZone();
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, tz));

        var slots = await availability.ComputeSlotsAsync(professional, localDate, ct);
        var slot = slots.FirstOrDefault(s => s.StartUtc == startUtc);

        if (slot == default)
            throw new ConflictException("slot.not_offered", "The selected time is not offered by this professional.");
        if (!slot.IsAvailable)
            throw new ConflictException("slot.taken", "Sorry, this time was just booked by someone else. Please pick another one.");

        var clientHasOverlap = await db.Appointments.AnyAsync(a =>
            a.ClientId == actor.UserId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.StartUtc < slot.EndUtc && a.EndUtc > slot.StartUtc, ct);

        if (clientHasOverlap)
            throw new ConflictException("client.overlap", "You already have another appointment at this time.");

        Appointment appointment;
        try
        {
            appointment = Appointment.Request(
                professional.Id, actor.UserId, actor.Name, actor.Email,
                slot.StartUtc, slot.EndUtc, request.Notes, clock.GetUtcNow().UtcDateTime);
        }
        catch (DomainException ex)
        {
            throw ValidationException.For(ex.Code, ex.Message);
        }

        db.Appointments.Add(appointment);
        notifier.Requested(appointment, professional);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The partial unique index (ProfessionalId, StartUtc) WHERE Status <> Cancelled
            // rejects concurrent double bookings that passed the checks above.
            throw new ConflictException("slot.taken", "Sorry, this time was just booked by someone else. Please pick another one.");
        }

        return appointment.ToDto(professional);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetForClientAsync(Actor actor, CancellationToken ct = default)
    {
        actor.RequireClient();

        var items = await db.Appointments.AsNoTracking()
            .Include(a => a.Professional)
            .Where(a => a.ClientId == actor.UserId)
            .OrderBy(a => a.StartUtc)
            .ToListAsync(ct);

        return items.Select(a => a.ToDto(a.Professional!)).ToList();
    }

    public async Task<ScheduleDto> GetScheduleAsync(Actor actor, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var professionalId = actor.RequireProfessionalId();
        var professional = await db.Professionals.AsNoTracking().FirstOrDefaultAsync(p => p.Id == professionalId, ct)
            ?? throw new NotFoundException("Professional");

        var tz = professional.GetTimeZone();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime, tz));
        var start = from ?? today;
        var end = to ?? start.AddDays(6);

        if (end < start)
            throw ValidationException.For("to", "'to' must be on or after 'from'.");
        if (end.DayNumber - start.DayNumber > 62)
            throw ValidationException.For("to", "The range cannot be longer than 62 days.");

        var fromUtc = SlotCalculator.LocalDayToUtcRange(start, tz).StartUtc;
        var toUtc = SlotCalculator.LocalDayToUtcRange(end, tz).EndUtc;

        var items = await db.Appointments.AsNoTracking()
            .Where(a => a.ProfessionalId == professionalId && a.StartUtc >= fromUtc && a.StartUtc < toUtc)
            .OrderBy(a => a.StartUtc)
            .ToListAsync(ct);

        var dtos = items.Select(a => a.ToDto(professional)).ToList();

        return new ScheduleDto(
            start,
            end,
            professional.TimeZoneId,
            items.Count(a => a.Status == AppointmentStatus.Pending),
            items.Count(a => a.Status == AppointmentStatus.Confirmed),
            items.Count(a => a.Status == AppointmentStatus.Cancelled),
            dtos);
    }

    public async Task<AppointmentDto> ConfirmAsync(Actor actor, Guid appointmentId, CancellationToken ct = default)
    {
        var professionalId = actor.RequireProfessionalId();
        var appointment = await LoadAsync(appointmentId, ct);

        if (appointment.ProfessionalId != professionalId)
            throw new NotFoundException("Appointment");

        Apply(() => appointment.Confirm(clock.GetUtcNow().UtcDateTime));
        notifier.Confirmed(appointment, appointment.Professional!);
        await db.SaveChangesAsync(ct);

        return appointment.ToDto(appointment.Professional!);
    }

    public async Task<AppointmentDto> CancelAsync(Actor actor, Guid appointmentId, string? reason, CancellationToken ct = default)
    {
        var appointment = await LoadAsync(appointmentId, ct);

        var isOwner = actor.Role switch
        {
            UserRole.Client => appointment.ClientId == actor.UserId,
            UserRole.Professional => appointment.ProfessionalId == actor.ProfessionalId,
            _ => false
        };

        if (!isOwner)
            throw new NotFoundException("Appointment");

        Apply(() => appointment.Cancel(actor.Role, reason, clock.GetUtcNow().UtcDateTime));
        notifier.Cancelled(appointment, appointment.Professional!);
        await db.SaveChangesAsync(ct);

        return appointment.ToDto(appointment.Professional!);
    }

    private async Task<Appointment> LoadAsync(Guid id, CancellationToken ct) =>
        await db.Appointments.Include(a => a.Professional).FirstOrDefaultAsync(a => a.Id == id, ct)
        ?? throw new NotFoundException("Appointment");

    private static void Apply(Action transition)
    {
        try
        {
            transition();
        }
        catch (DomainException ex)
        {
            throw new ConflictException(ex.Code, ex.Message);
        }
    }
}
