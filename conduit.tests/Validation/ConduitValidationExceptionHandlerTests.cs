using System.IO;
using conduit.Exceptions;
using conduit.logging;
using conduit.Pipes.Stages;
using conduit.validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Validation;

public class ConduitValidationExceptionHandlerTests
{
    private static HttpContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<ILog>().Object);
        return new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
        };
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_400_For_A_ValidationFailedException()
    {
        // Arrange
        var context = CreateContext();
        var validationResult = ValidationResult.WithFailure(new object(), [new ValidationError("Name", "Name is required.")]);
        RequestDelegate next = _ => throw new ValidationFailedException(validationResult);
        var handler = new ConduitValidationExceptionHandler(next);

        // Act
        await handler.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_500_For_A_StageFailedException()
    {
        // Arrange
        var context = CreateContext();
        RequestDelegate next = _ => throw new StageFailedException("stage failed");
        var handler = new ConduitValidationExceptionHandler(next);

        // Act
        await handler.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_500_For_A_ValidatorNotFoundException()
    {
        // Arrange: previously ValidatorNotFoundException wasn't a known exception at all, so it would
        // rethrow past this middleware entirely instead of getting its own ProblemDetails response
        // (Q5 in the edge-case review).
        var context = CreateContext();
        RequestDelegate next = _ => throw new ValidatorNotFoundException("no validator registered");
        var handler = new ConduitValidationExceptionHandler(next);

        // Act
        await handler.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Should_Rethrow_An_Unknown_Exception()
    {
        // Arrange
        var context = CreateContext();
        RequestDelegate next = _ => throw new InvalidOperationException("boom");
        var handler = new ConduitValidationExceptionHandler(next);

        // Act
        Func<Task> act = () => handler.InvokeAsync(context);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }
}
