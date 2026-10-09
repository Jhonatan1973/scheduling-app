using SchedulingApp.Domain.Common;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Domain.Enums;
using static SchedulingApp.UnitTests.Support.Builders;

namespace SchedulingApp.UnitTests.Domain;

public class AppointmentTests
{
    private static readonly DateTime Start = Utc(2030, 5, 10, 14);
    private static readonly DateTime Before = Start.AddDays(-1);

    [Fact]
    public void New_requests_start_as_pending()
    {
        var appointment = Appointment(Start);

        Assert.Equal(AppointmentStatus.Pending, appointment.Status);
        Assert.True(appointment.IsActive);
    }

    [Fact]
    public void Cannot_request_a_time_in_the_past()
    {
        var ex = Assert.Throws<DomainException>(() => Appointment(Start, nowUtc: Start.AddMinutes(1)));
        Assert.Equal("appointment.in_past", ex.Code);
    }

    [Fact]
    public void Rejects_notes_longer_than_the_limit()
    {
        var notes = new string('x', SchedulingApp.Domain.Entities.Appointment.NotesMaxLength + 1);

        Assert.Throws<DomainException>(() => SchedulingApp.Domain.Entities.Appointment.Request(
            Guid.NewGuid(), "c", "Client", "c@test.com", Start, Start.AddMinutes(30), notes, Before));
    }

    [Fact]
    public void Professional_can_confirm_a_pending_request()
    {
        var appointment = Appointment(Start);

        appointment.Confirm(Before);

        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        Assert.Equal(Before, appointment.ConfirmedAtUtc);
    }

    [Fact]
    public void Cannot_confirm_twice_or_after_cancellation()
    {
        var confirmed = Appointment(Start);
        confirmed.Confirm(Before);
        Assert.Equal("appointment.already_confirmed", Assert.Throws<DomainException>(() => confirmed.Confirm(Before)).Code);

        var cancelled = Appointment(Start);
        cancelled.Cancel(UserRole.Client, null, Before);
        Assert.Equal("appointment.cancelled", Assert.Throws<DomainException>(() => cancelled.Confirm(Before)).Code);
    }

    [Fact]
    public void Cancel_records_who_cancelled_and_why()
    {
        var appointment = Appointment(Start);

        appointment.Cancel(UserRole.Professional, "  Sick leave  ", Before);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal(UserRole.Professional, appointment.CancelledBy);
        Assert.Equal("Sick leave", appointment.CancellationReason);
        Assert.False(appointment.IsActive);
    }

    [Fact]
    public void Cannot_cancel_twice_or_after_the_start()
    {
        var appointment = Appointment(Start);
        appointment.Cancel(UserRole.Client, null, Before);
        Assert.Equal("appointment.already_cancelled",
            Assert.Throws<DomainException>(() => appointment.Cancel(UserRole.Client, null, Before)).Code);

        var started = Appointment(Start);
        Assert.Equal("appointment.started",
            Assert.Throws<DomainException>(() => started.Cancel(UserRole.Client, null, Start.AddMinutes(5))).Code);
    }
}
