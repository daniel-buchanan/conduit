using conduit.Exceptions;
using conduit.logging;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Validation;

public class WithValidatorForTypeTests
{
    public class TypeValidatedRequest : IRequest<TypeValidatedResponse>
    {
        public string? Message { get; set; }
    }

    public class TypeValidatedResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TypeValidatedRequestHandler(ILog logger) : RequestHandler<TypeValidatedRequest, TypeValidatedResponse>(logger)
    {
        public override Task<TypeValidatedResponse> HandleAsync(TypeValidatedRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new TypeValidatedResponse { Value = request.Message ?? string.Empty });
    }

    public class TypeValidator : ModelValidator<TypeValidatedRequest, TypeValidatedResponse>
    {
        protected override void AddRules(IRuleBuilder<TypeValidatedRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).NotBe().NullOrWhitespace("Message is required.");
    }

    [Fact]
    public async Task WithValidatorFor_Type_Overload_Should_Be_Resolved_By_ValidationStage()
    {
        // Arrange: unlike WithValidatorFor(IModelValidator<,> instance), this overload registers the
        // validator by TYPE, letting the container construct (and inject dependencies into) it.
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.AddValidation(vb => vb.ThrowIfValidatorNotFound().WithValidatorFor<TypeValidatedRequest, TypeValidatedResponse, TypeValidator>());
            c.RegisterHandler<TypeValidatedRequest, TypeValidatedResponse, TypeValidatedRequestHandler>();
        }, new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var conduit = provider.GetRequiredService<IConduit>();

        // Act
        Func<Task> act = () => conduit.PushAsync(new TypeValidatedRequest { Message = null });

        // Assert
        await Assert.ThrowsAsync<ValidationFailedException>(act);
    }
}
