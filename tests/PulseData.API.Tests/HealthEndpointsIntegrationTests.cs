using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PulseData.API.Models;
using PulseData.API.Services;

namespace PulseData.API.Tests;

public sealed class HealthEndpointsIntegrationTests
{
  [Fact]
  public async Task Live_WhenHealthy_Returns200()
  {
    var healthService = new StubHealthCheckService(
        liveness: HealthCheckResults.Create(true, new Dictionary<string, ServiceHealth>
        {
          ["Application"] = ServiceHealth.Healthy("API process is running")
        }),
        readiness: HealthCheckResults.Create(true));

    using var factory = new HealthApiFactory(healthService);
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://localhost")
    });

    var response = await client.GetAsync("/api/health/live");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var payload = await response.Content.ReadFromJsonAsync<HealthPayload>();
    Assert.NotNull(payload);
    Assert.Equal(HealthStatuses.Healthy, payload!.Status);
  }

  [Fact]
  public async Task Ready_WhenTimedOut_Returns408()
  {
    var healthService = new StubHealthCheckService(
        liveness: HealthCheckResults.Create(true),
        readiness: HealthCheckResults.Create(false, new Dictionary<string, ServiceHealth>
        {
          ["Database"] = ServiceHealth.Unhealthy(
                details: "Database connectivity check timed out",
                errorCode: HealthFailureCodes.Timeout)
        }));

    using var factory = new HealthApiFactory(healthService);
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://localhost")
    });

    var response = await client.GetAsync("/api/health/ready");

    Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
    var payload = await response.Content.ReadFromJsonAsync<HealthPayload>();
    Assert.NotNull(payload);
    Assert.Equal(HealthStatuses.NotReady, payload!.Status);
  }

  [Fact]
  public async Task Ready_WhenDependencyFails_Returns503()
  {
    var healthService = new StubHealthCheckService(
        liveness: HealthCheckResults.Create(true),
        readiness: HealthCheckResults.Create(false, new Dictionary<string, ServiceHealth>
        {
          ["Database"] = ServiceHealth.Unhealthy(
                details: "Database connectivity check failed",
                errorCode: HealthFailureCodes.DependencyUnavailable)
        }));

    using var factory = new HealthApiFactory(healthService);
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://localhost")
    });

    var response = await client.GetAsync("/api/health/ready");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    var payload = await response.Content.ReadFromJsonAsync<HealthPayload>();
    Assert.NotNull(payload);
    Assert.Equal(HealthStatuses.NotReady, payload!.Status);
  }

  private sealed class HealthApiFactory : WebApplicationFactory<global::Program>
  {
    private readonly IHealthCheckService _healthCheckService;

    public HealthApiFactory(IHealthCheckService healthCheckService)
    {
      _healthCheckService = healthCheckService;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
      builder.ConfigureServices(services =>
      {
        services.RemoveAll<IHealthCheckService>();
        services.AddSingleton(_healthCheckService);
      });
    }
  }
  private sealed record HealthPayload(string Status);
}
