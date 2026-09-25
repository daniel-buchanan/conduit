using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using conduit.validation;
using conduit.validation.Rules;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Validation;

public class WithValidationExtensionTests
{
    public class DoubleValidationRequest : IRequest<DoubleValidationResponse>
    {
        public string? Message { get; set; }
    }

    public class DoubleValidationResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class DoubleValidationRequestHandler(ILog logger) : RequestHandler<DoubleValidationRequest, DoubleValidationResponse>(logger)
    {
        public override Task<DoubleValidationResponse> HandleAsync(DoubleValidationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new DoubleValidationResponse { Value = request.Message ?? string.Empty });
    }

    public class CountingValidator : ModelValidator<DoubleValidationRequest, DoubleValidationResponse>
    {
        public int CallCount;

        protected override void AddRules(IRuleBuilder<DoubleValidationRequest> ruleBuilder)
            => ruleBuilder.AddRule(new Rule<DoubleValidationRequest>(r =>
            {
                CallCount++;
                return ValidationResult.WithSuccess(r);
            }));
    }

    [Fact]
    public async Task WithValidation_Without_ExcludeValidation_Should_Run_Validation_Exactly_Once()
    {
        // Arrange: a pipe that adds an explicit ValidationStage via WithValidation() but never calls
        // ExcludeValidation() would previously also still get the default pre-execution ValidationStage,
        // running the same validator twice for a single request.
        var validator = new CountingValidator();
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.AddValidation(vb => vb.WithValidatorFor(validator));
            c.RegisterPipe<DoubleValidationRequest, DoubleValidationResponse>(p =>
            {
                p.WithValidation();
                p.AddHandler<DoubleValidationRequestHandler>();
            });
        }, new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<DoubleValidationRequest, DoubleValidationResponse>>();

        // Act
        await pipe.PushAsync(new DoubleValidationRequest { Message = "hello" });

        // Assert
        Assert.Equal(1, validator.CallCount);
    }
}
