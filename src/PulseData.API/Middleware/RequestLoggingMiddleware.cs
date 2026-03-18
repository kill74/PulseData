using System.Diagnostics;
using System.Text;

namespace PulseData.API.Middleware;

/// <summary>
/// Logs all incoming HTTP requests and outgoing responses with timing information.
/// Useful for performance monitoring and debugging.
/// </summary>
public class RequestLoggingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<RequestLoggingMiddleware> _logger;

  public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var stopwatch = Stopwatch.StartNew();
    var originalBodyStream = context.Response.Body;
    var responseBody = new MemoryStream();
    context.Response.Body = responseBody;

    try
    {
      _logger.LogInformation(
          "{Method} {Path} (Query: {Query})",
          context.Request.Method,
          context.Request.Path,
          context.Request.QueryString);

      await _next(context);

      stopwatch.Stop();
      _logger.LogInformation(
          "{StatusCode} {Duration}ms",
          context.Response.StatusCode,
          stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      _logger.LogWarning(ex, "Error {Method} {Path} ({Duration}ms): {Error}",
          context.Request.Method,
          context.Request.Path,
          stopwatch.ElapsedMilliseconds,
          ex.Message);
      throw;
    }
    finally
    {
      await responseBody.CopyToAsync(originalBodyStream);
      context.Response.Body = originalBodyStream;
    }
  }
}
