namespace PulseData.API.Models;

/// <summary>
/// Standard API error response for consistent error handling across all endpoints.
/// </summary>
public record ApiErrorResponse(
    string Code,
    string Message,
    string? Detail = null,
    string? TraceId = null,
    Dictionary<string, string[]>? Errors = null
)
{
  /// <summary>
  /// Error codes used throughout the API for programmatic handling.
  /// </summary>
  public static class ErrorCodes
  {
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string BadRequest = "BAD_REQUEST";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string DatabaseError = "DATABASE_ERROR";
    public const string ResourceConflict = "RESOURCE_CONFLICT";
  }
}

/// <summary>
/// Standard API success response wrapper for consistent response formatting.
/// </summary>
public record ApiResponse<T>(T Data, string? Message = null);

/// <summary>
/// Health check response indicating service readiness.
/// </summary>
public record HealthCheckResponse(
    string Status,
    DateTime Timestamp,
    string Version,
    Dictionary<string, ServiceHealth>? Services = null
);

/// <summary>
/// Individual service health information.
/// </summary>
public record ServiceHealth(
    string Status,
    string? Details = null
)
{
  public static ServiceHealth Healthy(string? details = null) =>
      new ServiceHealth("Healthy", details);

  public static ServiceHealth Unhealthy(string? details = null) =>
      new ServiceHealth("Unhealthy", details);
}
