using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PulseData.ETL.Pipeline;
using PulseData.Infrastructure.Data;

// ---------------------------------------------------------------------------
// PulseData ETL - Entry Point
// ---------------------------------------------------------------------------
// Usage:
//   dotnet run                          (uses default sample file)
//   dotnet run -- path/to/orders.csv    (explicit file path)
// ---------------------------------------------------------------------------

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Set up dependency injection
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddSingleton<DbConnectionFactory>();
services.AddSingleton<OrderEtlPipeline>();
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var serviceProvider = services.BuildServiceProvider();

// Get pipeline and logger
var pipeline = serviceProvider.GetRequiredService<OrderEtlPipeline>();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var csvPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "sample_orders.csv");

logger.LogInformation("PulseData ETL Pipeline");
logger.LogInformation("CSV Path: {CsvPath}", csvPath);
logger.LogInformation("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development");
logger.LogInformation("Starting pipeline...");
logger.LogInformation("");

var result = await pipeline.RunAsync(csvPath);

logger.LogInformation("");
logger.LogInformation("Summary:");
logger.LogInformation("  Records read   : {RecordsRead}", result.RecordsRead);
logger.LogInformation("  Records loaded : {RecordsLoaded}", result.RecordsLoaded);
logger.LogInformation("  Records failed : {RecordsFailed}", Math.Max(result.RecordsFailed, 0));
logger.LogInformation("  Duration       : {Duration:F2}s", result.Duration.TotalSeconds);

if (result.Errors.Count > 0)
{
    logger.LogWarning("");
    logger.LogWarning("Errors (first 10):");
    foreach (var error in result.Errors.Take(10))
        logger.LogWarning("  {Error}", error);

    if (result.Errors.Count > 10)
        logger.LogWarning("  ... and {RemainingErrors} more. Check etl_run_log table.", result.Errors.Count - 10);
}

logger.LogInformation("");
Environment.Exit(result.RecordsFailed > 0 ? 1 : 0);
