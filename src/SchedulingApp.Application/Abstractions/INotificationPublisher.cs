using SchedulingApp.Application.Contracts.Messaging;

namespace SchedulingApp.Application.Abstractions;

public interface INotificationPublisher
{
    Task PublishAsync(AppointmentNotificationMessage message, CancellationToken cancellationToken = default);
}
