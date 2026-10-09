namespace SchedulingApp.Contracts;

/// <summary>Subset of RFC 7807 ProblemDetails used by the front-end to show friendly errors.</summary>
public sealed class ApiProblem
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public int? Status { get; set; }
    public string? Code { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }

    public string Message =>
        Errors is { Count: > 0 }
            ? string.Join(" ", Errors.SelectMany(e => e.Value))
            : Detail ?? Title ?? "Something went wrong.";
}
