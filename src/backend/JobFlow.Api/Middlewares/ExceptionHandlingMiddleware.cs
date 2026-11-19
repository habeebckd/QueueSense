using System.Net;
using System.Text.Json;

namespace JobFlow.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier ?? Guid.NewGuid().ToString();

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, errorCode, userMessage) = ClassifyException(exception);

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            success = false,
            error = new
            {
                code = errorCode,
                message = userMessage
            },
            correlationId = correlationId
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static (HttpStatusCode StatusCode, string ErrorCode, string Message) ClassifyException(Exception ex)
    {
        var fullMessage = ex.ToString();
        var isDbConnectionFailure = ex is Npgsql.NpgsqlException
            || ex is System.Net.Sockets.SocketException
            || ex is TimeoutException
            || fullMessage.Contains("transient failure", StringComparison.OrdinalIgnoreCase)
            || fullMessage.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase)
            || fullMessage.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)
            || fullMessage.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);

        if (isDbConnectionFailure)
        {
            return (
                HttpStatusCode.ServiceUnavailable,
                "DATABASE_UNAVAILABLE",
                "The database service is currently unavailable. Please ensure PostgreSQL is running and try again."
            );
        }

        return ex switch
        {
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "UNAUTHORIZED",
                ex.Message
            ),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                ex.Message
            ),
            ArgumentException => (
                HttpStatusCode.BadRequest,
                "BAD_REQUEST",
                ex.Message
            ),
            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                "INVALID_OPERATION",
                ex.Message
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_SERVER_ERROR",
                "An unexpected server error occurred. Please try again later."
            )
        };
    }
}
