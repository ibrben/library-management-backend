using LibraryManagement.Business.Exceptions;

namespace LibraryManagement.Api.Middleware;

internal sealed partial class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException validation =>
                (StatusCodes.Status400BadRequest, validation.Message, validation.Errors),
            UnauthorizedException unauthorized =>
                (StatusCodes.Status401Unauthorized, unauthorized.Message, EmptyErrors()),
            ConflictException conflict =>
                (StatusCodes.Status409Conflict, conflict.Message, EmptyErrors()),
            _ =>
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", EmptyErrors())
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception);
        }
        else
        {
            LogRequestFailure(logger, exception, statusCode);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { success = false, message, errors });
    }

    private static Dictionary<string, string[]> EmptyErrors() =>
        new Dictionary<string, string[]>();

    [LoggerMessage(LogLevel.Error, "Unhandled request exception.")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Request failed with status {StatusCode}.")]
    private static partial void LogRequestFailure(ILogger logger, Exception exception, int statusCode);
}
