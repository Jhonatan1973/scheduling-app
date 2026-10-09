using Microsoft.Extensions.DependencyInjection;
using SchedulingApp.Application.Appointments;
using SchedulingApp.Application.Availability;
using SchedulingApp.Application.Notifications;
using SchedulingApp.Application.Professionals;

namespace SchedulingApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ProfessionalService>();
        services.AddScoped<AvailabilityService>();
        services.AddScoped<AppointmentService>();
        services.AddScoped<AppointmentNotifier>();
        return services;
    }
}
