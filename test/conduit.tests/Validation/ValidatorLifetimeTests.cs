using conduit.logging;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Validation;

public class ValidatorLifetimeTests
{
    public class LifetimeRequest : IRequest<LifetimeResponse>
    {
        public string? Message { get; set; }
    }

    public class LifetimeResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class LifetimeValidator : ModelValidator<LifetimeRequest, LifetimeResponse>
    {
        protected override void AddRules(IRuleBuilder<LifetimeRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).NotBe().NullOrWhitespace("Message is required.");
    }

    [Fact]
    public void WithValidatorFor_Type_Overload_Should_Resolve_The_Same_Instance_Every_Time()
    {
        // Arrange: a validator registered Transient gets rebuilt (and its rule expressions recompiled)
        // on every resolution — pure overhead, since a validator's rule set never varies per request.
        var services = new ServiceCollection();
        services.AddConduit(c => c.AddValidation(vb => vb.WithValidatorFor<LifetimeRequest, LifetimeResponse, LifetimeValidator>()),
            new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();

        // Act
        var first = provider.GetRequiredService<IModelValidator<LifetimeRequest, LifetimeResponse>>();
        var second = provider.GetRequiredService<IModelValidator<LifetimeRequest, LifetimeResponse>>();

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void WithValidatorsFromAssembly_Should_Resolve_The_Same_Instance_Every_Time()
    {
        // Arrange: same concern as above, for the assembly-scan discovery path.
        var services = new ServiceCollection();
        services.AddConduit(c => c.AddValidation(vb => vb.WithValidatorsFromAssembly<ValidatorLifetimeTests>()),
            new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();

        // Act
        var first = provider.GetRequiredService<IModelValidator<LifetimeRequest, LifetimeResponse>>();
        var second = provider.GetRequiredService<IModelValidator<LifetimeRequest, LifetimeResponse>>();

        // Assert
        Assert.Same(first, second);
    }
}
