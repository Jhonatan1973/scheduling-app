namespace SchedulingApp.Domain.Common;

/// <summary>
/// Raised when a business rule is violated (e.g. confirming a cancelled appointment).
/// Mapped to HTTP 422/409 by the API.
/// </summary>
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
