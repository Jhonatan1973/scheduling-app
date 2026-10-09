using Microsoft.Extensions.Options;
using SchedulingApp.Application.Appointments;
using SchedulingApp.Application.Availability;
using SchedulingApp.Application.Common;
using SchedulingApp.Application.Notifications;
using SchedulingApp.Contracts.Appointments;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Domain.Enums;
using SchedulingApp.UnitTests.Support;
using static SchedulingApp.UnitTests.Support.Builders;
using C = SchedulingApp.Contracts;

namespace SchedulingApp.UnitTests.Application;

public sealed class AppointmentServiceTests : IDisposable
{
    // Sunday 2030-01-06 12:00 UTC. The professional works Mondays 09:00-12:00 São Paulo time (UTC-3).
    private static readonly DateTimeOffset Now = new(2030, 1, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MondayNineLocal = new(2030, 1, 7, 9, 0, 0, TimeSpan.FromHours(-3));

    private readonly TestDb _db = TestDb.Create();
    private readonly FixedTimeProvider _clock = new(Now);
    private readonly AppointmentService _service;
    private readonly AvailabilityService _availability;
    private readonly Professional _professional;

    private readonly Actor _client = new("client-1", "Carla Client", "carla@test.com", UserRole.Client, null);
    private readonly Actor _otherClient = new("client-2", "Otto Other", "otto@test.com", UserRole.Client, null);
    private readonly Actor _proActor;

    public AppointmentServiceTests()
    {
        _professional = Professional();
        _professional.AvailabilityRules.Add(Rule(DayOfWeek.Monday, 9, 12));
        _db.Professionals.Add(_professional);
        _db.SaveChanges();

        _proActor = new Actor(_professional.UserId, _professional.DisplayName, _professional.Email, UserRole.Professional, _professional.Id);

        _availability = new AvailabilityService(_db, _clock);
        var notifier = new AppointmentNotifier(_db, _clock, Options.Create(new NotificationOptions { PublicWebUrl = "https://app.test" }));
        _service = new AppointmentService(_db, _availability, notifier, _clock);
    }

    [Fact]
    public async Task Booking_creates_a_pending_appointment_and_queues_two_emails()
    {
        var result = await _service.BookAsync(_client, new BookAppointmentRequest(_professional.Id, MondayNineLocal, "First visit"));

        Assert.Equal(C.AppointmentStatus.Pending, result.Status);
        Assert.Equal(MondayNineLocal, result.Start);
        Assert.Equal(MondayNineLocal.AddMinutes(30), result.End);

        var emails = _db.OutboxEmails.ToList();
        Assert.Equal(2, emails.Count);
        Assert.Contains(emails, e => e.ToEmail == _client.Email);
        Assert.Contains(emails, e => e.ToEmail == _professional.Email && e.TextBody.Contains("First visit"));
    }

    [Fact]
    public async Task Booked_slot_is_no_longer_available_and_cannot_be_double_booked()
    {
        await _service.BookAsync(_client, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null));

        var slots = await _availability.GetSlotsAsync(_professional.Id, new DateOnly(2030, 1, 7));
        Assert.False(slots.Slots.Single(s => s.Start == MondayNineLocal).IsAvailable);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.BookAsync(_otherClient, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null)));
        Assert.Equal("slot.taken", ex.Code);
    }

    [Fact]
    public async Task Cannot_book_a_time_outside_working_hours()
    {
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.BookAsync(_client, new BookAppointmentRequest(_professional.Id, MondayNineLocal.AddHours(5), null)));

        Assert.Equal("slot.not_offered", ex.Code);
    }

    [Fact]
    public async Task Professionals_cannot_book()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.BookAsync(_proActor, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null)));
    }

    [Fact]
    public async Task Professional_confirms_and_client_is_notified()
    {
        var booked = await _service.BookAsync(_client, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null));

        var confirmed = await _service.ConfirmAsync(_proActor, booked.Id);

        Assert.Equal(C.AppointmentStatus.Confirmed, confirmed.Status);
        Assert.Equal(3, _db.OutboxEmails.Count());
        Assert.Contains(_db.OutboxEmails, e => e.ToEmail == _client.Email && e.Subject.StartsWith("Appointment confirmed"));
    }

    [Fact]
    public async Task Another_professional_cannot_see_or_confirm_the_appointment()
    {
        var booked = await _service.BookAsync(_client, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null));
        var stranger = new Actor("pro-2", "Stranger", "s@test.com", UserRole.Professional, Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => _service.ConfirmAsync(stranger, booked.Id));
    }

    [Fact]
    public async Task Other_clients_cannot_cancel_someone_elses_appointment()
    {
        var booked = await _service.BookAsync(_client, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null));

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CancelAsync(_otherClient, booked.Id, null));
    }

    [Fact]
    public async Task Client_cancellation_frees_the_slot_and_notifies_the_professional()
    {
        var booked = await _service.BookAsync(_client, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null));

        var cancelled = await _service.CancelAsync(_client, booked.Id, "Can't make it");

        Assert.Equal(C.AppointmentStatus.Cancelled, cancelled.Status);
        Assert.Equal(C.UserRole.Client, cancelled.CancelledBy);
        Assert.Contains(_db.OutboxEmails, e => e.ToEmail == _professional.Email && e.Subject.Contains("cancelled"));

        var slots = await _availability.GetSlotsAsync(_professional.Id, new DateOnly(2030, 1, 7));
        Assert.True(slots.Slots.Single(s => s.Start == MondayNineLocal).IsAvailable);

        // the freed slot can be booked again
        await _service.BookAsync(_otherClient, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null));
    }

    [Fact]
    public async Task Confirming_a_cancelled_appointment_is_a_conflict()
    {
        var booked = await _service.BookAsync(_client, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null));
        await _service.CancelAsync(_client, booked.Id, null);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.ConfirmAsync(_proActor, booked.Id));
        Assert.Equal("appointment.cancelled", ex.Code);
    }

    [Fact]
    public async Task Client_cannot_hold_two_appointments_at_the_same_time()
    {
        var other = Professional();
        other.AvailabilityRules.Add(Rule(DayOfWeek.Monday, 9, 12));
        _db.Professionals.Add(other);
        await _db.SaveChangesAsync();

        await _service.BookAsync(_client, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.BookAsync(_client, new BookAppointmentRequest(other.Id, MondayNineLocal, null)));
        Assert.Equal("client.overlap", ex.Code);
    }

    [Fact]
    public async Task Schedule_returns_appointments_and_counters_for_the_range()
    {
        var first = await _service.BookAsync(_client, new BookAppointmentRequest(_professional.Id, MondayNineLocal, null));
        await _service.BookAsync(_otherClient, new BookAppointmentRequest(_professional.Id, MondayNineLocal.AddMinutes(30), null));
        await _service.ConfirmAsync(_proActor, first.Id);

        var schedule = await _service.GetScheduleAsync(_proActor, new DateOnly(2030, 1, 7), new DateOnly(2030, 1, 13));

        Assert.Equal(2, schedule.Appointments.Count);
        Assert.Equal(1, schedule.PendingCount);
        Assert.Equal(1, schedule.ConfirmedCount);
        Assert.Equal("America/Sao_Paulo", schedule.TimeZoneId);
    }

    [Fact]
    public async Task Slots_are_not_offered_beyond_the_booking_window()
    {
        var farAway = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(SchedulingApp.Domain.Scheduling.BookingPolicy.MaxDaysAhead + 1);
        while (farAway.DayOfWeek != DayOfWeek.Monday)
            farAway = farAway.AddDays(1);

        var slots = await _availability.GetSlotsAsync(_professional.Id, farAway);

        Assert.Empty(slots.Slots);
    }

    public void Dispose() => _db.Dispose();
}
