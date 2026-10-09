using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SchedulingApp.Application.Abstractions;
using Testcontainers.PostgreSql;

namespace SchedulingApp.IntegrationTests;

/// <summary>Boots the real API against a throw-away PostgreSQL container (requires Docker).</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public CapturingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "integration-tests-only-secret-0123456789abcdef");
        builder.UseSetting("Seed:DemoData", "false");
        builder.UseSetting("Email:Provider", "Log");
        builder.UseSetting("Email:Dispatcher:Enabled", "false");
        builder.UseSetting("RateLimiting:AuthPermitPerMinute", "10000");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

public sealed class CapturingEmailSender : IEmailSender
{
    public ConcurrentBag<EmailMessage> Sent { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
