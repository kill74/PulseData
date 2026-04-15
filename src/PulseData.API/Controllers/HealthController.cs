using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PulseData.API.Models;
using PulseData.API.Services;

namespace PulseData.API.Controllers;

/// <summary>
/// Health check endpoint for monitoring and load balancer integration.
/// Essential for production deployments with container orchestration (Kubernetes, Docker Swarm).
/// </summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
  private static readonly string ApiVersion =
      typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";

  private readonly IHealthCheckService _healthCheck;

  public HealthController(IHealthCheckService healthCheck)
  {
    ArgumentNullException.ThrowIfNull(healthCheck);
    _healthCheck = healthCheck;
  }

  /// <summary>
  /// Get application liveness status.
  /// Returns 200 while the API process is alive.
  /// Used by load balancers and orchestrators (Kubernetes, Docker Swarm) to detect container/process availability.
  /// </summary>
  [HttpGet("live")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
  public async Task<ActionResult<HealthCheckResponse>> GetLiveness(CancellationToken cancellationToken)
  {
    var result = await _healthCheck.CheckLivenessAsync(cancellationToken);
    var response = CreateHealthResponse(
        result.IsHealthy ? HealthStatuses.Healthy : HealthStatuses.Unhealthy,
        result.Services);

    return result.IsHealthy ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
  }

  /// <summary>
  /// Get application readiness status.
  /// Indicates whether the service is ready to accept requests.
  /// Returns 200 when ready, 408 on timeout, or 503 when required dependencies are unavailable.
  /// </summary>
  [HttpGet("ready")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
  [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
  public async Task<ActionResult<HealthCheckResponse>> GetReadiness(CancellationToken cancellationToken)
  {
    var result = await _healthCheck.CheckReadinessAsync(cancellationToken);
    var response = CreateHealthResponse(
        result.IsHealthy ? HealthStatuses.Ready : HealthStatuses.NotReady,
        result.Services);

    if (result.IsHealthy)
      return Ok(response);

    return StatusCode(GetReadinessFailureStatusCode(result.Services), response);
  }

  private static int GetReadinessFailureStatusCode(IReadOnlyDictionary<string, ServiceHealth> services)
  {
    foreach (var service in services.Values)
    {
      if (string.Equals(service.ErrorCode, HealthFailureCodes.Timeout, StringComparison.OrdinalIgnoreCase))
        return StatusCodes.Status408RequestTimeout;
    }

    return StatusCodes.Status503ServiceUnavailable;
  }

  private static HealthCheckResponse CreateHealthResponse(string status, IReadOnlyDictionary<string, ServiceHealth> services)
  {
    return new HealthCheckResponse(
        Status: status,
        Timestamp: DateTime.UtcNow,
        Version: ApiVersion,
        Services: services
    );
  }
}
