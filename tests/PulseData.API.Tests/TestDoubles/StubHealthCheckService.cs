using PulseData.API.Models;
using PulseData.API.Services;

namespace PulseData.API.Tests;

internal static class HealthCheckResults
{
  public static HealthCheckResult Create(
      bool isHealthy,
      IReadOnlyDictionary<string, ServiceHealth>? services = null)
  {
    return new HealthCheckResult(isHealthy, services ?? new Dictionary<string, ServiceHealth>());
  }
}

internal sealed class StubHealthCheckService : IHealthCheckService
{
  private readonly HealthCheckResult _liveness;
  private readonly HealthCheckResult _readiness;

  public StubHealthCheckService(HealthCheckResult liveness, HealthCheckResult readiness)
  {
    _liveness = liveness;
    _readiness = readiness;
  }

  public Task<HealthCheckResult> CheckLivenessAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_liveness);
  }

  public Task<HealthCheckResult> CheckReadinessAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_readiness);
  }
}
