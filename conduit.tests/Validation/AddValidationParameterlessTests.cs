using conduit.Exceptions;
using conduit.logging;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Validation;

public class AddValidationParameterlessTests
{
    public class ParameterlessAddValidationRequest : IRequest<ParameterlessAddValidationResponse>
    {
        public string? Message { get; set; }
    }

    public class ParameterlessAddValidationResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ParameterlessAddValidationHandler(ILog logger)
        : RequestHandler<ParameterlessAddValidationRequest, ParameterlessAddValidationResponse>(logger)
    {
        public override Task<ParameterlessAddValidationResponse> HandleAsync(ParameterlessAddValidationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ParameterlessAddValidationResponse { Value = request.Message ?? string.Empty });
    }

    public class ParameterlessAddValidationValidator : ModelValidator<ParameterlessAddValidationRequest, ParameterlessAddValidationResponse>
    {
        protected override void AddRules(IRuleBuilder<ParameterlessAddValidationRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).NotBe().NullOrWhitespace("Message is required.");
    }

    [Fact]
    public async Task AddValidation_Parameterless_Overload_Should_Discover_Validators_From_All_Loaded_Assemblies()
    {
        // Arrange: unlike AddValidation(vb => vb.WithValidatorsFromAssembly<T>()), which every other test in
        // this suite uses, the parameterless overload scans every currently loaded assembly — including
        // conduit.validation's own assembly, which contains the abstract ModelValidator<,> base class itself.
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.AddValidation();
            c.RegisterHandler<ParameterlessAddValidationRequest, ParameterlessAddValidationResponse, ParameterlessAddValidationHandler>();
        }, new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var conduit = provider.GetRequiredService<IConduit>();

        // Act
        Func<Task> act = () => conduit.PushAsync(new ParameterlessAddValidationRequest { Message = null });

        // Assert
        await Assert.ThrowsAsync<ValidationFailedException>(act);
    }
}
