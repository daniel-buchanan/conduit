using conduit.Exceptions;
using conduit.logging;
using conduit.Pipes;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Validation;

public class RegisterHandlerExcludeValidationTests
{
    public class HandlerExcludableRequest : IRequest<HandlerExcludableResponse>
    {
        public string? Message { get; set; }
    }

    public class HandlerExcludableResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class HandlerExcludableRequestHandler(ILog logger) : RequestHandler<HandlerExcludableRequest, HandlerExcludableResponse>(logger)
    {
        public override Task<HandlerExcludableResponse> HandleAsync(HandlerExcludableRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HandlerExcludableResponse { Value = request.Message ?? string.Empty });
    }

    public class HandlerExcludableRequestValidator : ModelValidator<HandlerExcludableRequest, HandlerExcludableResponse>
    {
        protected override void AddRules(IRuleBuilder<HandlerExcludableRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).NotBe().NullOrWhitespace("Message is required.");
    }

    [Fact]
    public async Task RegisterHandler_Without_ExcludeValidation_Should_Still_Apply_Default_Validation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.AddValidation(vb => vb.WithValidatorsFromAssembly<RegisterHandlerExcludeValidationTests>());
            c.RegisterHandler<HandlerExcludableRequest, HandlerExcludableResponse, HandlerExcludableRequestHandler>();
        }, new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<HandlerExcludableRequest, HandlerExcludableResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new HandlerExcludableRequest { Message = null });

        // Assert
        await Assert.ThrowsAsync<ValidationFailedException>(act);
    }

    [Fact]
    public async Task RegisterHandler_With_ExcludeValidation_Should_Skip_Default_Validation()
    {
        // Arrange: RegisterHandler previously had no way to opt out of default validation at all
        // (no configure callback existed), unlike RegisterPipe's ExcludeValidation(). This proves the gap
        // is closed.
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.AddValidation(vb => vb.WithValidatorsFromAssembly<RegisterHandlerExcludeValidationTests>());
            c.RegisterHandler<HandlerExcludableRequest, HandlerExcludableResponse, HandlerExcludableRequestHandler>(
                o => o.ExcludeValidation());
        }, new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<HandlerExcludableRequest, HandlerExcludableResponse>>();

        // Act
        var response = await pipe.PushAsync(new HandlerExcludableRequest { Message = null });

        // Assert: the handler still ran even though the request would have failed validation.
        Assert.NotNull(response);
        Assert.Equal(string.Empty, response!.Value);
    }
}
