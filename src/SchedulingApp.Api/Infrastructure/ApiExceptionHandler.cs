using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SchedulingApp.Application.Common;
using SchedulingApp.Domain.Common;

namespace SchedulingApp.Api.Infrastructure;

/// <summary>Translates application/domain exceptions into RFC 7807 ProblemDetails responses.</summary>
internal sealed partial class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", "validation_failed"),
            NotFoundException e => (StatusCodes.Status404NotFound, "Not found", e.Code),
            ForbiddenException e => (StatusCodes.Status403Forbidden, "Forbidden", e.Code),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, "Unauthorized", e.Code),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflict", e.Code),
            DomainException e => (StatusCodes.Status422UnprocessableEntity, "Business rule violated", e.Code),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad request", "bad_request"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "internal_error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            LogUnhandled(exception);

        var details = exception is ValidationException validation
            ? new ValidationProblemDetails(validation.Errors.ToDictionary(e => e.Key, e => e.Value))
            : new ProblemDetails();

        details.Status = status;
        details.Title = title;
        details.Detail = status == StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred. Please try again."
            : exception.Message;
        details.Extensions["code"] = code;

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = details,
            Exception = exception
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private partial void LogUnhandled(Exception exception);
}
