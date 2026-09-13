using conduit.Exceptions;
using conduit.logging;
using conduit.Pipes;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Validation;

public class ExcludeValidationTests
{
    public class ExcludableRequest : IRequest<ExcludableResponse>
    {
        public string? Message { get; set; }
    }

    public class ExcludableResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ExcludableRequestHandler(ILog logger) : RequestHandler<ExcludableRequest, ExcludableResponse>(logger)
    {
        public override Task<ExcludableResponse> HandleAsync(ExcludableRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ExcludableResponse { Value = request.Message ?? string.Empty });
    }

    public class ExcludableRequestValidator : ModelValidator<ExcludableRequest, ExcludableResponse>
    {
        protected override void AddRules(IRuleBuilder<ExcludableRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message!).NotBe().NullOrWhitespace("Message is required.");
    }

    [Fact]
    public async Task RegisterPipe_Without_ExcludeValidation_Should_Still_Apply_Default_Validation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.AddValidation(vb => vb.WithValidatorsFromAssembly<ExcludeValidationTests>());
            c.RegisterPipe<ExcludableRequest, ExcludableResponse>(p => p.AddHandler<ExcludableRequestHandler>());
        }, new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ExcludableRequest, ExcludableResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new ExcludableRequest { Message = null });

        // Assert
        await Assert.ThrowsAsync<ValidationFailedException>(act);
    }

    [Fact]
    public async Task RegisterPipe_With_ExcludeValidation_Should_Skip_Default_Validation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.AddValidation(vb => vb.WithValidatorsFromAssembly<ExcludeValidationTests>());
            c.RegisterPipe<ExcludableRequest, ExcludableResponse>(p =>
            {
                p.ExcludeValidation();
                p.AddHandler<ExcludableRequestHandler>();
            });
        }, new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ExcludableRequest, ExcludableResponse>>();

        // Act
        var response = await pipe.PushAsync(new ExcludableRequest { Message = null });

        // Assert: the handler still ran even though the request would have failed validation.
        Assert.NotNull(response);
        Assert.Equal(string.Empty, response!.Value);
    }
}
