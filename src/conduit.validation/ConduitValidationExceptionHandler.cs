using System.Text.Json;
using conduit.Exceptions;
using conduit.logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.validation;

// ReSharper disable once ClassNeverInstantiated.Global
public class ConduitValidationExceptionHandler(RequestDelegate next)
{
    private const string ContentTypeJson = "application/json";
    private static readonly Dictionary<Type, Func<Exception, HttpContext, Task>> KnownExceptionMap = new()
    {
        { typeof(ValidationFailedException), HandleValidationException },
        { typeof(StageFailedException), HandleStageFailedException },
    };
    
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
        var type = ex.GetType();
        if (!KnownExceptionMap.TryGetValue(type, out var value)) return false;
        
        logger.Error("Exception Type: {0}", type.Name);
        await value.Invoke(ex, context);
        
        return true;
    }
    
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
    
    private static async Task WriteResponse(HttpContext context, int statusCode, object details)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = ContentTypeJson;
        await context.Response.WriteAsync(JsonSerializer.Serialize(details));
    }
}