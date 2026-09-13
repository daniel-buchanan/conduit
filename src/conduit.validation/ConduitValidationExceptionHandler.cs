using System.Text.Json;
using System.Linq;
using conduit.Exceptions;
using conduit.logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.validation;

/// <summary>
/// Provides ASP.NET Core middleware for handling Conduit validation and stage exceptions.
/// Catches validation and stage failures and converts them to appropriate HTTP responses.
/// </summary>
/// <param name="next">The next middleware in the request pipeline.</param>
public class ConduitValidationExceptionHandler(RequestDelegate next)
{
    private const string ContentTypeJson = "application/json";
    private static readonly (Func<Exception, bool> Matches, Func<Exception, HttpContext, Task> Handle)[] KnownExceptionHandlers =
    [
        (ex => ex is ValidationFailedException, HandleValidationException),
        (ex => ex is StageFailedException, HandleStageFailedException),
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
    private static async Task HandleValidationException(Exception ex, HttpContext context)
    {
        ProblemDetails problemDetails;
        if (ex is not ValidationFailedException exception)
        {
            problemDetails = new ProblemDetails()
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Instance = context.Request.Path,
                Detail = ex.Message,
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest
            };
        }
        else
        {
            problemDetails = new ValidationProblemDetails(exception.ValidationErrors.ToModelState())
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Status =  StatusCodes.Status400BadRequest,
                Instance = context.Request.Path,
                Title = "Validation Error",
            };    
        }
        
        await WriteResponse(context, StatusCodes.Status400BadRequest, problemDetails);
    }
    
    /// <summary>
    /// Handles stage failures by returning a 500 Internal Server Error response.
    /// </summary>
    private static async Task HandleStageFailedException(Exception ex, HttpContext context)
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "A pipeline stage failed while processing your request.",
            Instance = context.Request.Path,
            Detail = ex.Message
        };

        await WriteResponse(context, StatusCodes.Status500InternalServerError, details);
    }
    
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