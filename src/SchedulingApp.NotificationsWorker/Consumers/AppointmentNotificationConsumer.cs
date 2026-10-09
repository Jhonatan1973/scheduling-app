using MassTransit;
using SchedulingApp.Application.Contracts.Messaging;
using SchedulingApp.Infrastructure.Email;

namespace SchedulingApp.NotificationsWorker.Consumers;

public class AppointmentNotificationConsumer(EmailSender emailSender) : IConsumer<AppointmentNotificationMessage>
{
    public Task Consume(ConsumeContext<AppointmentNotificationMessage> context) =>
        emailSender.SendAppointmentNotificationAsync(context.Message, context.CancellationToken);
}
