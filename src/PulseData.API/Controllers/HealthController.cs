using PulseData.API.Models;
using PulseData.API.Services;

namespace PulseData.API.Controllers;

/// <summary>
/// Health check endpoint for monitoring and load balancer integration.
/// Essential for production deployments with container orchestration (Kubernetes, Docker Swarm).
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
  private readonly IHealthCheckService _healthCheck;
  private readonly ILogger<HealthController> _logger;

  public HealthController(IHealthCheckService healthCheck, ILogger<HealthController> logger)
  {
    _healthCheck = healthCheck;
    _logger = logger;
  }

  /// <summary>
  /// Get application health status.
  /// Returns 200 if healthy, 503 if unhealthy.
  /// Used by load balancers and orchestrators (Kubernetes, Docker Swarm) to determine service readiness.
  /// </summary>
  [HttpGet("live")]
  [ProduceResponseType(StatusCodes.Status200OK)]
  [ProduceResponseType(StatusCodes.Status503ServiceUnavailable)]
  public async Task<ActionResult<HealthCheckResponse>> GetLiveness()
  {
    var (isHealthy, services) = await _healthCheck.CheckHealthAsync();
    var response = new HealthCheckResponse(
        Status: isHealthy ? "Healthy" : "Unhealthy",
        Timestamp: DateTime.UtcNow,
        Version: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
        Services: services
    );

    return isHealthy ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
  }

  /// <summary>
  /// Get application readiness status.
  /// Indicates whether the service is ready to accept requests.
  /// Returns 200 if ready, 503 if not ready.
  /// </summary>
  [HttpGet("ready")]
  [ProduceResponseType(StatusCodes.Status200OK)]
  [ProduceResponseType(StatusCodes.Status503ServiceUnavailable)]
  public async Task<ActionResult<HealthCheckResponse>> GetReadiness()
  {
    var (isHealthy, services) = await _healthCheck.CheckHealthAsync();
    var response = new HealthCheckResponse(
        Status: isHealthy ? "Ready" : "NotReady",
        Timestamp: DateTime.UtcNow,
        Version: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
        Services: services
    );

    return isHealthy ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
  }
}
