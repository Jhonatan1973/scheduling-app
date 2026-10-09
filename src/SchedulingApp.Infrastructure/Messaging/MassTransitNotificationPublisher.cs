using MassTransit;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Application.Contracts.Messaging;

namespace SchedulingApp.Infrastructure.Messaging;

public class MassTransitNotificationPublisher(IPublishEndpoint publishEndpoint) : INotificationPublisher
{
    public Task PublishAsync(AppointmentNotificationMessage message, CancellationToken cancellationToken = default) =>
        publishEndpoint.Publish(message, cancellationToken);
}
