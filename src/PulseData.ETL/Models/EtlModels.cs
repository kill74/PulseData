using CsvHelper.Configuration.Attributes;

namespace PulseData.ETL.Models;

/// <summary>
/// Maps directly to a row in the raw orders CSV file.
/// Field names match the CSV headers exactly.
/// </summary>
public class RawOrderRecord
{
    [Name("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [Name("customer_email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [Name("product_sku")]
    public string ProductSku { get; set; } = string.Empty;

    [Name("quantity")]
    public int Quantity { get; set; }

    [Name("unit_price")]
    public decimal UnitPrice { get; set; }

    [Name("order_date")]
    public string OrderDate { get; set; } = string.Empty;

    [Name("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Cleaned and validated version of a RawOrderRecord, ready for DB insert.
/// </summary>
public class CleanedOrder
{
    public string ExternalOrderId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Subtotal => Quantity * UnitPrice;
}

public class EtlResult
{
    public int RecordsRead { get; set; }
    public int RecordsLoaded { get; set; }
    public int RecordsFailed { get; set; }
    public List<string> Errors { get; set; } = [];
    public TimeSpan Duration { get; set; }

    public bool HasErrors => RecordsFailed > 0;
}
