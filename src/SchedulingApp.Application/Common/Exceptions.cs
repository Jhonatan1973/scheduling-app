namespace SchedulingApp.Application.Common;

/// <summary>Base type for errors the API turns into ProblemDetails responses.</summary>
public abstract class AppException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class NotFoundException(string resource)
    : AppException("not_found", $"{resource} was not found.");

public sealed class ForbiddenException(string message)
    : AppException("forbidden", message);

public sealed class ConflictException(string code, string message)
    : AppException(code, message);

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : AppException("validation_failed", "One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public static ValidationException For(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}

public sealed class UnauthorizedException(string message)
    : AppException("unauthorized", message);
