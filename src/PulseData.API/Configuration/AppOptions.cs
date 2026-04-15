using System.ComponentModel.DataAnnotations;

namespace PulseData.API.Configuration;

/// <summary>
/// CORS configuration that varies by deployment environment.
/// Keeps credentials and origins out of code; uses appsettings instead.
/// </summary>
public class CorsOptions
{
  public static readonly string SectionName = "Cors";

  /// <summary>
  /// List of allowed origins (e.g., https://app.example.com)
  /// </summary>
  public string[] AllowedOrigins { get; set; } = [];

  /// <summary>
  /// List of allowed HTTP methods (default: GET, POST, PUT, DELETE)
  /// </summary>
  public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE"];

  /// <summary>
  /// List of allowed headers (default: Content-Type, Authorization)
  /// </summary>
  public string[] AllowedHeaders { get; set; } = ["Content-Type", "Authorization"];

  /// <summary>
  /// whether to allow credentials (cookies, authorization headers)
  /// </summary>
  public bool AllowCredentials { get; set; } = false;

  /// <summary>
  /// Cache validity in seconds
  /// </summary>
  public int MaxAge { get; set; } = 3600;
}

/// <summary>
/// JWT authentication configuration.
/// </summary>
public class JwtOptions
{
  public static readonly string SectionName = "Jwt";

  /// <summary>Issuing authority (e.g., your auth server)</summary>
  public string Issuer { get; set; } = string.Empty;

  /// <summary>Audience claim (who the token is for)</summary>
  public string Audience { get; set; } = string.Empty;

  /// <summary>Secret key for signing/validating tokens</summary>
  public string SecretKey { get; set; } = string.Empty;

  /// <summary>Token expiration in minutes</summary>
  public int ExpirationMinutes { get; set; } = 60;
}

/// <summary>
/// Application logging configuration.
/// </summary>
public class LoggingOptions
{
  public static readonly string SectionName = "Logging";

  /// <summary>Minimum log level (Trace, Debug, Information, Warning, Error, Critical)</summary>
  public string MinimumLevel { get; set; } = "Information";

  /// <summary>Whether to include request/response bodies in logs</summary>
  public bool LogRequestBody { get; set; } = false;

  /// <summary>Whether to include response bodies in logs</summary>
  public bool LogResponseBody { get; set; } = false;

  /// <summary>PostgreSQL connection string for table logging (optional)</summary>
  public string? PostgreSqlTableConnectionString { get; set; }

  /// <summary>Name of the logs table for PostgreSQL sink</summary>
  public string? TableName { get; set; } = "application_logs";
}

/// <summary>
/// Health endpoint behavior configuration.
/// </summary>
public class HealthCheckOptions
{
  public static readonly string SectionName = "HealthCheck";

  /// <summary>
  /// Maximum execution time for readiness dependency checks in milliseconds.
  /// </summary>
  [Range(100, 120000)]
  public int ReadinessTimeoutMs { get; set; } = 5000;

  /// <summary>
  /// Short cache window for readiness results to reduce probe load on dependencies.
  /// Set to 0 to disable caching.
  /// </summary>
  [Range(0, 60000)]
  public int ReadinessCacheTtlMs { get; set; } = 5000;

  /// <summary>
  /// Whether to expose low-level dependency error details in health responses.
  /// </summary>
  public bool IncludeErrorDetails { get; set; } = false;
}
