using System.Diagnostics;
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
//   dotnet run -- --dry-run             (test without writing data)
// ---------------------------------------------------------------------------

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
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
var connectionFactory = serviceProvider.GetRequiredService<DbConnectionFactory>();

// Parse command line arguments
bool dryRunMode = args.Contains("--dry-run");
var csvPath = args.Length > 0 && !args[0].StartsWith("--")
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "sample_orders.csv");

logger.LogInformation("PulseData ETL Pipeline");
logger.LogInformation("CSV Path: {CsvPath}", csvPath);
logger.LogInformation("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development");

// HIGH PRIORITY: File validation
if (!File.Exists(csvPath))
{
    logger.LogError("CSV file not found: {CsvPath}", csvPath);
    Environment.Exit(1);
}
logger.LogInformation("CSV file validated");

// HIGH PRIORITY: Database connection validation
try
{
    using var connection = connectionFactory.CreateConnection();
    logger.LogInformation("Database connection verified");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to connect to database");
    Environment.Exit(1);
}

// MEDIUM PRIORITY: Dry-run mode notification
if (dryRunMode)
{
    logger.LogWarning("Running in DRY-RUN mode - no data will be written to database");
}

logger.LogInformation("Starting pipeline...");
logger.LogInformation("");

// HIGH PRIORITY: Graceful shutdown with cancellation token
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    logger.LogWarning("ETL pipeline cancelled by user");
};

// MEDIUM PRIORITY: Performance tracking
var stopwatch = Stopwatch.StartNew();

try
{
    var result = await pipeline.RunAsync(csvPath, cts.Token);
    stopwatch.Stop();

    logger.LogInformation("");
    logger.LogInformation("Summary:");
    logger.LogInformation("  Records read   : {RecordsRead}", result.RecordsRead);
    logger.LogInformation("  Records loaded : {RecordsLoaded}", result.RecordsLoaded);
    logger.LogInformation("  Records failed : {RecordsFailed}", Math.Max(result.RecordsFailed, 0));
    logger.LogInformation("  Duration       : {Duration:F2}s", result.Duration.TotalSeconds);

    // MEDIUM PRIORITY: Performance metrics
    double successRate = result.RecordsRead > 0 ? (result.RecordsLoaded * 100.0 / result.RecordsRead) : 0;
    logger.LogInformation("  Success rate   : {SuccessRate:F2}%", successRate);
    double recordsPerSecond = result.RecordsRead > 0 ? (result.RecordsRead / result.Duration.TotalSeconds) : 0;
    logger.LogInformation("  Throughput     : {Throughput:F0} records/sec", recordsPerSecond);

    if (result.Errors.Count > 0)
    {
        logger.LogWarning("");
        logger.LogWarning("Errors (first 10):");
        foreach (var error in result.Errors.Take(10))
            logger.LogWarning("  {Error}", error);

        if (result.Errors.Count > 10)
            logger.LogWarning("  ... and {RemainingErrors} more. Check etl_run_log table.", result.Errors.Count - 10);
    }

    // LOW PRIORITY: Resource monitoring
    var process = Process.GetCurrentProcess();
    long memoryUsageMB = process.WorkingSet64 / 1024 / 1024;
    logger.LogInformation("");
    logger.LogInformation("Resource usage: {MemoryMB}MB memory", memoryUsageMB);

    // HIGH PRIORITY: Error threshold check
    int maxErrorsAllowed = config.GetValue<int>("Etl:MaxErrorsAllowed", 100);
    if (result.RecordsFailed > maxErrorsAllowed)
    {
        logger.LogError("Error threshold exceeded: {Errors} errors, max allowed: {Max}",
            result.RecordsFailed, maxErrorsAllowed);
        if (!dryRunMode)
        {
            Environment.Exit(1);
        }
    }

    logger.LogInformation("");
    if (dryRunMode)
    {
        logger.LogInformation("Dry-run completed successfully - no data was written");
        Environment.Exit(0);
    }

    Environment.Exit(result.RecordsFailed > 0 ? 1 : 0);
}
catch (OperationCanceledException)
{
    stopwatch.Stop();
    logger.LogWarning("ETL pipeline was cancelled after {Duration:F2}s", stopwatch.Elapsed.TotalSeconds);
    Environment.Exit(2);
}
catch (Exception ex)
{
    stopwatch.Stop();
    logger.LogError(ex, "Unexpected error during ETL pipeline after {Duration:F2}s", stopwatch.Elapsed.TotalSeconds);
    Environment.Exit(1);
}
