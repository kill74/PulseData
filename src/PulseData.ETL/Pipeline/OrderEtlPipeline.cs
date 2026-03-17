using System.Globalization;
using CsvHelper;
using Dapper;
using PulseData.ETL.Models;
using PulseData.Infrastructure.Data;

namespace PulseData.ETL.Pipeline;

/// <summary>
/// Reads a raw orders CSV, validates and transforms each row,
/// then loads clean records into PostgreSQL.
///
/// Design decisions:
/// - We validate ALL rows before inserting any of them (fail-fast approach)
/// - Each ETL run is logged to etl_run_log for auditability
/// - Duplicate external order IDs are skipped, not errored (idempotent loads)
/// </summary>
public class OrderEtlPipeline
{
    private readonly DbConnectionFactory _db;

    public OrderEtlPipeline(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<EtlResult> RunAsync(string csvFilePath)
    {
        var result = new EtlResult();
        var startTime = DateTime.UtcNow;

        Console.WriteLine($"[ETL] Starting pipeline for: {csvFilePath}");

        // ----------------------------------------------------------------
        // Step 1: Extract
        // ----------------------------------------------------------------
        List<RawOrderRecord> rawRecords;
        try
        {
            rawRecords = Extract(csvFilePath);
            result.RecordsRead = rawRecords.Count;
            Console.WriteLine($"[ETL] Extracted {rawRecords.Count} records");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Extract failed: {ex.Message}");
            result.RecordsFailed = -1;
            await LogRunAsync(result, csvFilePath, startTime);
            return result;
        }

        // ----------------------------------------------------------------
        // Step 2: Transform
        // ----------------------------------------------------------------
        var (cleanRecords, transformErrors) = Transform(rawRecords);
        result.RecordsFailed += transformErrors.Count;
        result.Errors.AddRange(transformErrors);
        Console.WriteLine($"[ETL] Transformed: {cleanRecords.Count} clean, {transformErrors.Count} rejected");

        // ----------------------------------------------------------------
        // Step 3: Load
        // ----------------------------------------------------------------
        var (loaded, loadErrors) = await LoadAsync(cleanRecords);
        result.RecordsLoaded = loaded;
        result.RecordsFailed += loadErrors.Count;
        result.Errors.AddRange(loadErrors);
        Console.WriteLine($"[ETL] Loaded {loaded} records into database");

        result.Duration = DateTime.UtcNow - startTime;
        await LogRunAsync(result, csvFilePath, startTime);

        Console.WriteLine($"[ETL] Done in {result.Duration.TotalSeconds:F2}s. " +
                          $"Loaded: {result.RecordsLoaded}, Failed: {result.RecordsFailed}");

        return result;
    }

    // ----------------------------------------------------------------
    // Extract: Read CSV into raw record objects
    // ----------------------------------------------------------------
    private List<RawOrderRecord> Extract(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}");

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        return csv.GetRecords<RawOrderRecord>().ToList();
    }

    // ----------------------------------------------------------------
    // Transform: Validate and clean raw records
    // ----------------------------------------------------------------
    private (List<CleanedOrder> Clean, List<string> Errors) Transform(List<RawOrderRecord> raw)
    {
        var clean  = new List<CleanedOrder>();
        var errors = new List<string>();

        var validStatuses = new HashSet<string>
            { "pending", "confirmed", "shipped", "delivered", "cancelled", "refunded" };

        for (int i = 0; i < raw.Count; i++)
        {
            var row = raw[i];
            var lineNum = i + 2; // 1-indexed + header row

            // Validate required fields
            if (string.IsNullOrWhiteSpace(row.CustomerEmail))
            {
                errors.Add($"Row {lineNum}: missing customer_email");
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.ProductSku))
            {
                errors.Add($"Row {lineNum}: missing product_sku");
                continue;
            }

            if (row.Quantity <= 0)
            {
                errors.Add($"Row {lineNum}: quantity must be > 0 (got {row.Quantity})");
                continue;
            }

            if (row.UnitPrice < 0)
            {
                errors.Add($"Row {lineNum}: unit_price cannot be negative");
                continue;
            }

            if (!DateTime.TryParse(row.OrderDate, out var parsedDate))
            {
                errors.Add($"Row {lineNum}: cannot parse order_date '{row.OrderDate}'");
                continue;
            }

            var status = row.Status.Trim().ToLower();
            if (!validStatuses.Contains(status))
            {
                errors.Add($"Row {lineNum}: unknown status '{row.Status}'");
                continue;
            }

            clean.Add(new CleanedOrder
            {
                ExternalOrderId = row.OrderId.Trim(),
                CustomerEmail   = row.CustomerEmail.Trim().ToLower(),
                ProductSku      = row.ProductSku.Trim().ToUpper(),
                Quantity        = row.Quantity,
                UnitPrice       = row.UnitPrice,
                OrderDate       = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc),
                Status          = status
            });
        }

        return (clean, errors);
    }

    // ----------------------------------------------------------------
    // Load: Insert clean records into the database
    // ----------------------------------------------------------------
    private async Task<(int Loaded, List<string> Errors)> LoadAsync(List<CleanedOrder> records)
    {
        var errors = new List<string>();
        int loaded = 0;

        using var conn = await _db.CreateConnectionAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            foreach (var record in records)
            {
                // Resolve customer ID from email
                var customerId = await conn.QuerySingleOrDefaultAsync<int?>(
                    "SELECT id FROM customers WHERE email = @Email",
                    new { Email = record.CustomerEmail },
                    transaction
                );

                if (customerId is null)
                {
                    errors.Add($"Skipped order {record.ExternalOrderId}: customer '{record.CustomerEmail}' not found");
                    continue;
                }

                // Resolve product ID from SKU
                var productId = await conn.QuerySingleOrDefaultAsync<int?>(
                    "SELECT id FROM products WHERE sku = @Sku AND is_active = TRUE",
                    new { Sku = record.ProductSku },
                    transaction
                );

                if (productId is null)
                {
                    errors.Add($"Skipped order {record.ExternalOrderId}: product SKU '{record.ProductSku}' not found");
                    continue;
                }

                // Insert order — skip duplicates (idempotent)
                var orderId = await conn.QuerySingleOrDefaultAsync<int?>(
                    """
                    INSERT INTO orders (customer_id, status, total_amount, placed_at)
                    VALUES (@CustomerId, @Status, @Total, @PlacedAt)
                    ON CONFLICT DO NOTHING
                    RETURNING id
                    """,
                    new
                    {
                        CustomerId = customerId,
                        record.Status,
                        Total     = record.Subtotal,
                        PlacedAt  = record.OrderDate
                    },
                    transaction
                );

                if (orderId is null)
                {
                    // ON CONFLICT — already exists, skip silently
                    continue;
                }

                // Insert order item
                await conn.ExecuteAsync(
                    """
                    INSERT INTO order_items (order_id, product_id, quantity, unit_price)
                    VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)
                    """,
                    new
                    {
                        OrderId   = orderId,
                        ProductId = productId,
                        record.Quantity,
                        record.UnitPrice
                    },
                    transaction
                );

                loaded++;
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            errors.Add($"Load aborted — transaction rolled back: {ex.Message}");
        }

        return (loaded, errors);
    }

    private async Task LogRunAsync(EtlResult result, string sourceFile, DateTime startTime)
    {
        try
        {
            using var conn = await _db.CreateConnectionAsync();
            await conn.ExecuteAsync(
                """
                INSERT INTO etl_run_log
                    (run_at, source_file, records_read, records_loaded, records_failed, status, error_message, duration_ms)
                VALUES
                    (@RunAt, @SourceFile, @RecordsRead, @RecordsLoaded, @RecordsFailed, @Status, @ErrorMessage, @DurationMs)
                """,
                new
                {
                    RunAt         = startTime,
                    SourceFile    = sourceFile,
                    result.RecordsRead,
                    result.RecordsLoaded,
                    RecordsFailed = Math.Max(result.RecordsFailed, 0),
                    Status        = result.HasErrors ? "completed" : "completed",
                    ErrorMessage  = result.Errors.Count > 0
                                        ? string.Join("; ", result.Errors.Take(5))
                                        : null,
                    DurationMs    = (int)result.Duration.TotalMilliseconds
                }
            );
        }
        catch (Exception ex)
        {
            // Don't let logging failure crash the pipeline
            Console.WriteLine($"[ETL] Warning: failed to write run log — {ex.Message}");
        }
    }
}
