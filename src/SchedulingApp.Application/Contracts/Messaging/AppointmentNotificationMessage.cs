namespace SchedulingApp.Application.Contracts.Messaging;

public record AppointmentNotificationMessage(
    Guid AppointmentId,
    string EventType,
    string RecipientEmail,
    string RecipientName,
    string ProfessionalName,
    DateTime StartUtc,
    DateTime EndUtc);
