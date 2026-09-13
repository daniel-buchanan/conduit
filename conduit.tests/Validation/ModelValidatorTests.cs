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
        Assert.Single(result.Errors!);
        Assert.Equal("Message cannot be Null.", result.Errors![0].Message);
    }

    public class TestModelValidator : ModelValidator<TestRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<TestRequest> ruleBuilder)
        {
            ruleBuilder.Should(x => x.Message!)
                .NotBe()
                .Null("Message cannot be Null.");

            ruleBuilder.Should(x => x.Message!)
                .Be()
                .Null("Message should be null");
        }
    }

    [Fact]
    public void ModelValidator_Should_Aggregate_Errors_From_Multiple_Failing_Rules()
    {
        // Arrange
        var validator = new MultiRuleValidator();
        var request = new TestRequest { Message = "not-hello" };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors!.Length);
        Assert.Contains(result.Errors, e => e.Message == "Message must be null.");
        Assert.Contains(result.Errors, e => e.Message == "Message must equal Hello.");
    }

    public class MultiRuleValidator : ModelValidator<TestRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<TestRequest> ruleBuilder)
        {
            ruleBuilder.Should(x => x.Message!).Be().Null("Message must be null.");
            ruleBuilder.Should(x => x.Message!).Be().EqualTo("Hello", "Message must equal Hello.");
        }
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("b", true)]
    [InlineData("c", false)]
    public void In_Should_Validate_Membership_Of_The_Provided_Values(string message, bool expectValid)
    {
        // Arrange
        var validator = new InRuleValidator();
        var request = new TestRequest { Message = message };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectValid, result.IsValid);
    }

    public class InRuleValidator : ModelValidator<TestRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<TestRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message!).Be().In("Message must be a or b.", ["a", "b"]);
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("c", false)]
    public void OneOf_Should_Behave_As_An_Alias_For_In(string message, bool expectValid)
    {
        // Arrange
        var validator = new OneOfRuleValidator();
        var request = new TestRequest { Message = message };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectValid, result.IsValid);
    }

    public class OneOfRuleValidator : ModelValidator<TestRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<TestRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message!).Be().OneOf("Message must be a or b.", ["a", "b"]);
    }

    [Fact]
    public void EqualTo_Should_Fail_When_Values_Differ()
    {
        // Arrange
        var validator = new EqualToValidator();
        var request = new TestRequest { Message = "not-hello" };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Message must equal Hello.", result.Errors![0].Message);
    }

    public class EqualToValidator : ModelValidator<TestRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<TestRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message!).Be().EqualTo("Hello", "Message must equal Hello.");
    }
}