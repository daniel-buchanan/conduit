using conduit.Exceptions;
using conduit.logging;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Validation;

public class WithValidatorForInstanceTests
{
    public class InstanceValidatedRequest : IRequest<InstanceValidatedResponse>
    {
        public string? Message { get; set; }
    }

    public class InstanceValidatedResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class InstanceValidatedRequestHandler(ILog logger) : RequestHandler<InstanceValidatedRequest, InstanceValidatedResponse>(logger)
    {
        public override Task<InstanceValidatedResponse> HandleAsync(InstanceValidatedRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new InstanceValidatedResponse { Value = request.Message ?? string.Empty });
    }

    public class InstanceValidator : ModelValidator<InstanceValidatedRequest, InstanceValidatedResponse>
    {
        protected override void AddRules(IRuleBuilder<InstanceValidatedRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).NotBe().NullOrWhitespace("Message is required.");
    }

    [Fact]
    public async Task WithValidatorFor_Instance_Overload_Should_Be_Resolved_By_ValidationStage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.AddValidation(vb => vb.ThrowIfValidatorNotFound().WithValidatorFor(new InstanceValidator()));
            c.RegisterHandler<InstanceValidatedRequest, InstanceValidatedResponse, InstanceValidatedRequestHandler>();
        }, new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var conduit = provider.GetRequiredService<IConduit>();

        // Act
        Func<Task> act = () => conduit.PushWithDebugAsync(new InstanceValidatedRequest { Message = null }, CancellationToken.None);

        // Assert: the registered validator instance must actually be found and run (and reject the null
        // Message), rather than ValidationStage failing to resolve it and either silently passing or
        // throwing ValidatorNotFoundException.
        await Assert.ThrowsAsync<ValidationFailedException>(act);
    }
}
