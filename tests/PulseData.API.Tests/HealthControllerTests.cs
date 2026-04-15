using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PulseData.API.Controllers;
using PulseData.API.Models;
using PulseData.API.Services;

namespace PulseData.API.Tests;

public sealed class HealthControllerTests
{
    [Fact]
    public async Task GetLiveness_WhenHealthy_Returns200WithHealthyPayload()
    {
        var sut = new HealthController(new StubHealthCheckService(
            liveness: HealthCheckResults.Create(
                isHealthy: true,
                services: new Dictionary<string, ServiceHealth>
                {
                    ["Application"] = ServiceHealth.Healthy("API process is running")
                }),
            readiness: HealthCheckResults.Create(isHealthy: true)));

        var action = await sut.GetLiveness(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var payload = Assert.IsType<HealthCheckResponse>(ok.Value);
        Assert.Equal(HealthStatuses.Healthy, payload.Status);
        Assert.NotNull(payload.Services);
        Assert.True(payload.Services!.ContainsKey("Application"));
    }

    [Fact]
    public async Task GetReadiness_WhenHealthy_Returns200WithReadyPayload()
    {
        var sut = new HealthController(new StubHealthCheckService(
            liveness: HealthCheckResults.Create(isHealthy: true),
            readiness: HealthCheckResults.Create(
                isHealthy: true,
                services: new Dictionary<string, ServiceHealth>
                {
                    ["Database"] = ServiceHealth.Healthy("PostgreSQL connection successful", 8)
                })));

        var action = await sut.GetReadiness(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var payload = Assert.IsType<HealthCheckResponse>(ok.Value);
        Assert.Equal(HealthStatuses.Ready, payload.Status);
        Assert.NotNull(payload.Services);
        Assert.True(payload.Services!.ContainsKey("Database"));
    }

    [Fact]
    public async Task GetReadiness_WhenDependencyTimesOut_Returns408()
    {
        var sut = new HealthController(new StubHealthCheckService(
            liveness: HealthCheckResults.Create(isHealthy: true),
            readiness: HealthCheckResults.Create(
                isHealthy: false,
                services: new Dictionary<string, ServiceHealth>
                {
                    ["Database"] = ServiceHealth.Unhealthy(
                        details: "Database connectivity check timed out",
                        errorCode: HealthFailureCodes.Timeout)
                })));

        var action = await sut.GetReadiness(CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status408RequestTimeout, status.StatusCode);

        var payload = Assert.IsType<HealthCheckResponse>(status.Value);
        Assert.Equal(HealthStatuses.NotReady, payload.Status);
    }

    [Fact]
    public async Task GetReadiness_WhenDependencyIsUnavailable_Returns503()
    {
        var sut = new HealthController(new StubHealthCheckService(
            liveness: HealthCheckResults.Create(isHealthy: true),
            readiness: HealthCheckResults.Create(
                isHealthy: false,
                services: new Dictionary<string, ServiceHealth>
                {
                    ["Database"] = ServiceHealth.Unhealthy(
                        details: "Database connectivity check failed",
                        errorCode: HealthFailureCodes.DependencyUnavailable)
                })));

        var action = await sut.GetReadiness(CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status.StatusCode);

        var payload = Assert.IsType<HealthCheckResponse>(status.Value);
        Assert.Equal(HealthStatuses.NotReady, payload.Status);
    }
}
