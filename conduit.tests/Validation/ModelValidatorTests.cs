using conduit.tests.Handlers;
using conduit.validation;
using Xunit;

namespace conduit.tests.Validation;

public class ModelValidatorTests
{
    [Fact]
    public void ModelValidator_Should_Have_Rules()
    {
        // Arrange
        var validator = new TestModelValidator();
        var request = new TestRequest { Message = null };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Message cannot be Null.", result.Errors[0].Message);
    }

    public class TestModelValidator : ModelValidator<TestRequest, TestResponse>
    {
        protected override Task AddRules(IRuleBuilder<TestRequest> ruleBuilder)
        {
            ruleBuilder.Should(x => x.Message)
                .NotBe()
                .Null("Message cannot be Null.");

            ruleBuilder.Should(x => x.Message)
                .Be()
                .Null("Message should be null");
                 
            return Task.CompletedTask;
        }
    }
}