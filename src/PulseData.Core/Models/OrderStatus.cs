namespace PulseData.Core.Models;

/// <summary>
/// Represents the lifecycle status of an order.
/// Single source of truth for order status across API, ETL, and database.
/// </summary>
public enum OrderStatus
{
  /// <summary>Order has been placed but not yet confirmed.</summary>
  Pending = 1,

  /// <summary>Order has been confirmed and is being processed.</summary>
  Confirmed = 2,

  /// <summary>Order has been shipped and is in transit.</summary>
  Shipped = 3,

  /// <summary>Order has been delivered to the customer.</summary>
  Delivered = 4,

  /// <summary>Order has been cancelled by customer or merchant.</summary>
  Cancelled = 5,

  /// <summary>Order has been refunded.</summary>
  Refunded = 6
}

/// <summary>
/// Helper methods for OrderStatus conversions and validation.
/// </summary>
public static class OrderStatusExtensions
{
  private static readonly Dictionary<string, OrderStatus> StatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "pending", OrderStatus.Pending },
        { "confirmed", OrderStatus.Confirmed },
        { "shipped", OrderStatus.Shipped },
        { "delivered", OrderStatus.Delivered },
        { "cancelled", OrderStatus.Cancelled },
        { "refunded", OrderStatus.Refunded }
    };

  /// <summary>
  /// Tries to parse a string to OrderStatus.
  /// </summary>
  public static bool TryParse(string? value, out OrderStatus status)
  {
    status = OrderStatus.Pending;
    if (string.IsNullOrWhiteSpace(value))
      return false;

    return StatusMap.TryGetValue(value, out status);
  }

  /// <summary>
  /// Returns all valid status strings for validation.
  /// </summary>
  public static IEnumerable<string> GetValidStatuses()
      => StatusMap.Keys;

  /// <summary>
  /// Converts OrderStatus enum to lowercase string (for database storage).
  /// </summary>
  public static string ToDbString(this OrderStatus status)
      => status.ToString().ToLowerInvariant();
}
