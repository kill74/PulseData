using Microsoft.Extensions.Configuration;
using PulseData.ETL.Pipeline;
using PulseData.Infrastructure.Data;

// ---------------------------------------------------------------------------
// PulseData ETL — Entry Point
// ---------------------------------------------------------------------------
// Usage:
//   dotnet run                          (uses default sample file)
//   dotnet run -- path/to/orders.csv    (explicit file path)
// ---------------------------------------------------------------------------

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var dbFactory = new DbConnectionFactory(config);
var pipeline  = new OrderEtlPipeline(dbFactory);

var csvPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "sample_orders.csv");

Console.WriteLine("╔══════════════════════════════════╗");
Console.WriteLine("║     PulseData ETL Pipeline       ║");
Console.WriteLine("╚══════════════════════════════════╝");
Console.WriteLine();

var result = await pipeline.RunAsync(csvPath);

Console.WriteLine();
Console.WriteLine("─── Summary ─────────────────────────");
Console.WriteLine($"  Records read   : {result.RecordsRead}");
Console.WriteLine($"  Records loaded : {result.RecordsLoaded}");
Console.WriteLine($"  Records failed : {Math.Max(result.RecordsFailed, 0)}");
Console.WriteLine($"  Duration       : {result.Duration.TotalSeconds:F2}s");

if (result.Errors.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("─── Errors ──────────────────────────");
    foreach (var error in result.Errors.Take(10))
        Console.WriteLine($"  ! {error}");

    if (result.Errors.Count > 10)
        Console.WriteLine($"  ... and {result.Errors.Count - 10} more. Check etl_run_log table.");
}

Console.WriteLine();
return result.RecordsFailed > 0 ? 1 : 0;
