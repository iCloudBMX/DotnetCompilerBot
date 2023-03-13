using System.Net;

namespace DotnetCompilerBot.Middlewares;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        try
        {
            await this.next(httpContext);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, exception.Message);

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
        }
    }
}
