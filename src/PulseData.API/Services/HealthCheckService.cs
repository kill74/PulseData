using System.Diagnostics;
using System.Collections.ObjectModel;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PulseData.API.Configuration;
using PulseData.API.Models;
using PulseData.Infrastructure.Data;

namespace PulseData.API.Services;

/// <summary>
/// Service for checking health of application and dependencies.
/// </summary>
public sealed record HealthCheckResult(bool IsHealthy, IReadOnlyDictionary<string, ServiceHealth> Services);

/// <summary>
/// Service abstraction for liveness and readiness probes.
/// </summary>
public interface IHealthCheckService
{
  Task<HealthCheckResult> CheckLivenessAsync(CancellationToken cancellationToken = default);
  Task<HealthCheckResult> CheckReadinessAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of health check service.
/// </summary>
public sealed class HealthCheckService : IHealthCheckService
{
  private const string ApplicationDependencyName = "Application";
  private const string DatabaseDependencyName = "Database";
  private const string ReadinessCacheKey = "health:readiness";

  private readonly DbConnectionFactory _dbFactory;
  private readonly IMemoryCache _cache;
  private readonly ILogger<HealthCheckService> _logger;
  private readonly int _readinessTimeoutMs;
  private readonly int _readinessCacheTtlMs;
  private readonly bool _includeErrorDetails;

  private static readonly IReadOnlyDictionary<string, ServiceHealth> EmptyServices =
      new ReadOnlyDictionary<string, ServiceHealth>(new Dictionary<string, ServiceHealth>(StringComparer.Ordinal));

  public HealthCheckService(
      DbConnectionFactory dbFactory,
      IMemoryCache cache,
      IOptions<HealthCheckOptions> options,
      ILogger<HealthCheckService> logger)
  {
    ArgumentNullException.ThrowIfNull(dbFactory);
    ArgumentNullException.ThrowIfNull(cache);
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(logger);

    var healthOptions = options.Value;

    _dbFactory = dbFactory;
    _cache = cache;
    _logger = logger;
    _readinessTimeoutMs = healthOptions.ReadinessTimeoutMs;
    _readinessCacheTtlMs = healthOptions.ReadinessCacheTtlMs;
    _includeErrorDetails = healthOptions.IncludeErrorDetails;
  }

  public Task<HealthCheckResult> CheckLivenessAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var services = new Dictionary<string, ServiceHealth>(StringComparer.Ordinal)
    {
      [ApplicationDependencyName] = ServiceHealth.Healthy("API process is running")
    };

    return Task.FromResult(new HealthCheckResult(true, ToReadOnly(services)));
  }

  public async Task<HealthCheckResult> CheckReadinessAsync(CancellationToken cancellationToken = default)
  {
    if (TryGetCachedReadiness(out var cachedResult))
      return cachedResult;

    var timeoutMs = _readinessTimeoutMs;
    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

    var services = new Dictionary<string, ServiceHealth>(StringComparer.Ordinal);

    var (databaseHealthy, databaseStatus) = await CheckDatabaseAsync(linkedCts.Token, cancellationToken, timeoutMs);
    services[DatabaseDependencyName] = databaseStatus;

    var result = new HealthCheckResult(databaseHealthy, ToReadOnly(services));
    CacheReadiness(result);

    return result;
  }

  private async Task<(bool IsHealthy, ServiceHealth Service)> CheckDatabaseAsync(
      CancellationToken healthCheckToken,
      CancellationToken requestCancellationToken,
      int timeoutMs)
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      using var conn = await _dbFactory.CreateConnectionAsync(healthCheckToken);
      var command = new CommandDefinition("SELECT 1", cancellationToken: healthCheckToken);
      await conn.QuerySingleAsync<int>(command);

      stopwatch.Stop();
      var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;
      _logger.LogDebug("Readiness check succeeded for {Dependency} in {DurationMs}ms", DatabaseDependencyName, responseTimeMs);

      return (true, ServiceHealth.Healthy("PostgreSQL connection successful", responseTimeMs));
    }
    catch (OperationCanceledException ex)
    {
      if (requestCancellationToken.IsCancellationRequested)
        throw;

      stopwatch.Stop();
      var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;
      var details = $"Database connectivity check timed out after {timeoutMs}ms";
      var error = _includeErrorDetails ? ex.Message : null;

      _logger.LogWarning(ex,
          "Readiness check timed out for {Dependency} after {DurationMs}ms",
          DatabaseDependencyName,
          responseTimeMs);

      return (false, ServiceHealth.Unhealthy(details, HealthFailureCodes.Timeout, error, responseTimeMs));
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;
      var details = "Database connectivity check failed";
      var error = _includeErrorDetails ? ex.Message : null;

      _logger.LogWarning(ex,
          "Readiness check failed for {Dependency} after {DurationMs}ms: {Error}",
          DatabaseDependencyName,
          responseTimeMs,
          ex.Message);

      return (false, ServiceHealth.Unhealthy(details, HealthFailureCodes.DependencyUnavailable, error, responseTimeMs));
    }
  }

  private bool TryGetCachedReadiness(out HealthCheckResult result)
  {
    result = new HealthCheckResult(false, EmptyServices);

    if (_readinessCacheTtlMs <= 0)
      return false;

    if (!_cache.TryGetValue(ReadinessCacheKey, out HealthCheckResult? cached) || cached is null)
      return false;

    _logger.LogDebug("Readiness check result served from cache");
    result = new HealthCheckResult(cached.IsHealthy, ToReadOnly(cached.Services));
    return true;
  }

  private void CacheReadiness(HealthCheckResult result)
  {
    if (!result.IsHealthy || _readinessCacheTtlMs <= 0)
      return;

    var cachedValue = new HealthCheckResult(result.IsHealthy, ToReadOnly(result.Services));
    _cache.Set(ReadinessCacheKey, cachedValue, TimeSpan.FromMilliseconds(_readinessCacheTtlMs));
  }

  private static IReadOnlyDictionary<string, ServiceHealth> ToReadOnly(IEnumerable<KeyValuePair<string, ServiceHealth>> services)
  {
    var copy = new Dictionary<string, ServiceHealth>(services, StringComparer.Ordinal);
    return new ReadOnlyDictionary<string, ServiceHealth>(copy);
  }
}
