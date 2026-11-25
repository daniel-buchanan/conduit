using System.Text.Json;
using System.Linq;
using conduit.Exceptions;
using conduit.logging;
using conduit.validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.aspnetcore.validation;

/// <summary>
/// Provides ASP.NET Core middleware for handling Conduit validation and stage exceptions.
/// Catches validation and stage failures and converts them to appropriate HTTP responses.
/// </summary>
/// <param name="next">The next middleware in the ASP.NET Core request pipeline.</param>
public class ConduitValidationExceptionHandler(RequestDelegate next)
{
    private const string ContentTypeJson = "application/json";
    private static readonly (Func<Exception, bool> Matches, Func<Exception, HttpContext, Task> Handle)[] KnownExceptionHandlers =
    [
        (ex => ex is ValidationFailedException, HandleValidationException),
        (ex => ex is StageFailedException, ServerError("A stage failed while processing your request.")),
        (ex => ex is ValidatorNotFoundException, ServerError("No validator was registered for this request.")),
    ];

    /// <summary>
    /// Invokes the middleware to handle exceptions in the request pipeline.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var logger = context.RequestServices.GetRequiredService<ILog>();
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            var didWriteResponse = await HandleException(logger, ex, context);
            if (!didWriteResponse) throw;
        }
    }

    private static async Task<bool> HandleException(ILog logger, Exception ex, HttpContext context)
    {
        var handler = KnownExceptionHandlers.FirstOrDefault(h => h.Matches(ex));
        if (handler.Handle is null) return false;

        logger.Error("Exception Type: {0}", ex.GetType().Name);
        await handler.Handle.Invoke(ex, context);

        return true;
    }

    /// <summary>
    /// Handles validation exceptions by returning a 400 Bad Request response with validation errors.
    /// </summary>
    private static Task HandleValidationException(Exception ex, HttpContext context)
        => WriteResponse(context, StatusCodes.Status400BadRequest,
            new ValidationProblemDetails(((ValidationFailedException)ex).ValidationErrors.ToModelState())
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.Request.Path,
                Title = "Validation Error",
            });

    /// <summary>
    /// Returns a handler that writes a 500 Internal Server Error response with the given title.
    /// </summary>
    private static Func<Exception, HttpContext, Task> ServerError(string title)
        => (ex, context) => WriteResponse(context, StatusCodes.Status500InternalServerError,
            new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = title,
                Instance = context.Request.Path,
                Detail = ex.Message,
            });

    /// <summary>
    /// Writes a JSON response to the HTTP context.
    /// </summary>
    private static async Task WriteResponse(HttpContext context, int statusCode, object details)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = ContentTypeJson;
        await context.Response.WriteAsync(JsonSerializer.Serialize(details));
    }
}
