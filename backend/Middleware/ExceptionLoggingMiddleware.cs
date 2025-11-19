using backend.Exceptions;
using System.Text.Json;

namespace backend.Middleware;

public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _logFilePath;
    private readonly SemaphoreSlim _logSemaphore = new(1, 1);

    public ExceptionLoggingMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _logFilePath = Path.Combine(env.ContentRootPath, "logs", "exceptions.log");
        
        // Ensure log directory exists
        var logDirectory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (QuizValidationException ex)
        {
            await LogExceptionAsync(ex, context);
            await HandleQuizValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await LogExceptionAsync(ex, context);
            await HandleGenericExceptionAsync(context, ex);
        }
    }

    private async Task LogExceptionAsync(Exception exception, HttpContext context)
    {
        // Use semaphore to ensure thread-safe file writing
        await _logSemaphore.WaitAsync();
        try
        {
            var logEntry = new
            {
                Timestamp = DateTimeOffset.UtcNow.ToString("o"),
                ExceptionType = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.Value,
                UserId = context.User?.Identity?.Name,
                AdditionalInfo = exception is QuizValidationException qve 
                    ? new { qve.ValidationField, qve.InvalidValue } 
                    : null
            };

            var logLine = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.AppendAllTextAsync(_logFilePath, logLine + Environment.NewLine + "---" + Environment.NewLine);
        }
        finally
        {
            _logSemaphore.Release();
        }
    }

    private static async Task HandleQuizValidationExceptionAsync(HttpContext context, QuizValidationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Validation Error",
            message = exception.Message,
            field = exception.ValidationField,
            value = exception.InvalidValue
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static async Task HandleGenericExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Internal Server Error",
            message = "An unexpected error occurred. Please try again later."
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}


