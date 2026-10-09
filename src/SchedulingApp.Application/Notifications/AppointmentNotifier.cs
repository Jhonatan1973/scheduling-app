using System.Net;
using Microsoft.Extensions.Options;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Application.Common;
using SchedulingApp.Domain;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Domain.Enums;

namespace SchedulingApp.Application.Notifications;

/// <summary>
/// Composes appointment emails and writes them to the outbox (same unit of work as the business change).
/// The background dispatcher in Infrastructure delivers them.
/// </summary>
public sealed class AppointmentNotifier(IAppDbContext db, TimeProvider clock, IOptions<NotificationOptions> options)
{
    private readonly string _webUrl = options.Value.PublicWebUrl.TrimEnd('/');

    public void Requested(Appointment appointment, Professional professional)
    {
        var when = Describe(appointment, professional);

        Enqueue(appointment.ClientEmail, appointment.ClientName,
            $"Booking request sent: {professional.DisplayName}",
            $"Your booking request with {professional.DisplayName} ({professional.Specialty}) for {when} was received. " +
            "You will get another email as soon as it is confirmed.",
            "View my appointments", $"{_webUrl}/my/appointments");

        Enqueue(professional.Email, professional.DisplayName,
            $"New booking request from {appointment.ClientName}",
            $"{appointment.ClientName} requested an appointment for {when}." +
            (appointment.Notes is null ? "" : $" Notes: \"{appointment.Notes}\"."),
            "Review request", $"{_webUrl}/pro/schedule");
    }

    public void Confirmed(Appointment appointment, Professional professional)
    {
        Enqueue(appointment.ClientEmail, appointment.ClientName,
            $"Appointment confirmed: {professional.DisplayName}",
            $"Good news! {professional.DisplayName} confirmed your appointment for {Describe(appointment, professional)}.",
            "View my appointments", $"{_webUrl}/my/appointments");
    }

    public void Cancelled(Appointment appointment, Professional professional)
    {
        var when = Describe(appointment, professional);
        var reason = appointment.CancellationReason is null ? "" : $" Reason: \"{appointment.CancellationReason}\".";

        if (appointment.CancelledBy == UserRole.Client)
        {
            Enqueue(professional.Email, professional.DisplayName,
                $"Appointment cancelled by {appointment.ClientName}",
                $"{appointment.ClientName} cancelled the appointment for {when}.{reason} The slot is free again.",
                "Open schedule", $"{_webUrl}/pro/schedule");
        }
        else
        {
            Enqueue(appointment.ClientEmail, appointment.ClientName,
                $"Appointment cancelled: {professional.DisplayName}",
                $"{professional.DisplayName} cancelled your appointment for {when}.{reason}",
                "Book another time", $"{_webUrl}/professionals/{professional.Id}");
        }
    }

    private static string Describe(Appointment appointment, Professional professional)
    {
        var tz = TimeZoneResolver.Resolve(professional.TimeZoneId);
        var local = appointment.StartUtc.ToZoned(tz);
        return $"{local:dddd, MMMM d, yyyy} at {local:HH:mm} ({professional.TimeZoneId})";
    }

    private void Enqueue(string toEmail, string toName, string subject, string message, string actionLabel, string actionUrl)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var greeting = $"Hi {toName},";

        var text = $"{greeting}\n\n{message}\n\n{actionLabel}: {actionUrl}\n\n— Scheduling App";
        var html = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:auto;color:#1f2937">
              <h2 style="color:#4f46e5;margin-bottom:4px">Scheduling App</h2>
              <p>{Encode(greeting)}</p>
              <p>{Encode(message)}</p>
              <p><a href="{Encode(actionUrl)}" style="display:inline-block;background:#4f46e5;color:#fff;padding:10px 18px;border-radius:8px;text-decoration:none">{Encode(actionLabel)}</a></p>
              <p style="color:#6b7280;font-size:12px">You received this email because of activity on your Scheduling App account.</p>
            </div>
            """;

        db.OutboxEmails.Add(new OutboxEmail
        {
            ToEmail = toEmail,
            ToName = toName,
            Subject = subject,
            TextBody = text,
            HtmlBody = html,
            CreatedAtUtc = now,
            NextAttemptAtUtc = now
        });
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
