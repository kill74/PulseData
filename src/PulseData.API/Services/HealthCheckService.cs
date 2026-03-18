using Dapper;
using PulseData.API.Models;
using PulseData.Infrastructure.Data;

namespace PulseData.API.Services;

/// <summary>
/// Service for checking health of application and dependencies.
/// </summary>
public interface IHealthCheckService
{
  Task<(bool IsHealthy, Dictionary<string, ServiceHealth> Services)> CheckHealthAsync();
}

/// <summary>
/// Implementation of health check service.
/// </summary>
public class HealthCheckService : IHealthCheckService
{
  private readonly DbConnectionFactory _dbFactory;
  private readonly ILogger<HealthCheckService> _logger;

  public HealthCheckService(DbConnectionFactory dbFactory, ILogger<HealthCheckService> logger)
  {
    _dbFactory = dbFactory;
    _logger = logger;
  }

  public async Task<(bool IsHealthy, Dictionary<string, ServiceHealth> Services)> CheckHealthAsync()
  {
    var services = new Dictionary<string, ServiceHealth>();
    var isHealthy = true;

    // Check database connectivity
    try
    {
      using var conn = await _dbFactory.CreateConnectionAsync();
      await conn.QuerySingleAsync<int>("SELECT 1");
      services["Database"] = ServiceHealth.Healthy("PostgreSQL connection successful");
    }
    catch (Exception ex)
    {
      _logger.LogWarning("Database health check failed: {Error}", ex.Message);
      services["Database"] = ServiceHealth.Unhealthy(ex.Message);
      isHealthy = false;
    }

    return (isHealthy, services);
  }
}
