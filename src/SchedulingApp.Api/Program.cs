using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using SchedulingApp.Api.Endpoints;
using SchedulingApp.Api.Infrastructure;
using SchedulingApp.Application;
using SchedulingApp.Infrastructure;
using SchedulingApp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Render/Railway inject the port through $PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitPerMinute", 20);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermitLimit, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
    await DatabaseInitializer.InitializeAsync(app.Services);

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapGet("/docs", () => Results.Content(SwaggerUi.Html, "text/html")).ExcludeFromDescription();
app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();
app.MapHealthChecks("/health");

app.MapAuthEndpoints();
app.MapProfessionalEndpoints();
app.MapAppointmentEndpoints();

app.Run();

/// <summary>Exposed for WebApplicationFactory in integration tests.</summary>
public partial class Program;
