using conduit.tests.Handlers;
using conduit.validation;
using Xunit;

namespace conduit.tests.Validation;

public class ModelValidatorTests
{
    // Each validator below gets its own request type (rather than sharing the common TestRequest fixture)
    // so that a WithValidatorsFromAssembly scan elsewhere in the suite never sees two of these target the
    // same (TRequest, TResponse) pair and trips the ValidatorAlreadyRegisteredException guard from ADR-0003
    // — these validators are only ever constructed directly with `new`, never resolved via scan.

    public class NullCheckRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Fact]
    public void ModelValidator_Should_Have_Rules()
    {
        // Arrange
        var validator = new TestModelValidator();
        var request = new NullCheckRequest { Message = null };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors!);
        Assert.Equal("Message cannot be Null.", result.Errors![0].Message);
    }

    public class TestModelValidator : ModelValidator<NullCheckRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<NullCheckRequest> ruleBuilder)
        {
            ruleBuilder.Should(x => x.Message)
                .NotBe()
                .Null("Message cannot be Null.");

            ruleBuilder.Should(x => x.Message)
                .Be()
                .Null("Message should be null");
        }
    }

    public class MultiRuleRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Fact]
    public void ModelValidator_Should_Aggregate_Errors_From_Multiple_Failing_Rules()
    {
        // Arrange
        var validator = new MultiRuleValidator();
        var request = new MultiRuleRequest { Message = "not-hello" };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors!.Length);
        Assert.Contains(result.Errors, e => e.Message == "Message must be null.");
        Assert.Contains(result.Errors, e => e.Message == "Message must equal Hello.");
    }

    public class MultiRuleValidator : ModelValidator<MultiRuleRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<MultiRuleRequest> ruleBuilder)
        {
            ruleBuilder.Should(x => x.Message).Be().Null("Message must be null.");
            ruleBuilder.Should(x => x.Message).Be().EqualTo("Hello", "Message must equal Hello.");
        }
    }

    public class InRuleRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("b", true)]
    [InlineData("c", false)]
    public void In_Should_Validate_Membership_Of_The_Provided_Values(string message, bool expectValid)
    {
        // Arrange
        var validator = new InRuleValidator();
        var request = new InRuleRequest { Message = message };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectValid, result.IsValid);
    }

    public class InRuleValidator : ModelValidator<InRuleRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<InRuleRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).Be().In("Message must be a or b.", ["a", "b"]);
    }

    public class OneOfRuleRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("c", false)]
    public void OneOf_Should_Behave_As_An_Alias_For_In(string message, bool expectValid)
    {
        // Arrange
        var validator = new OneOfRuleValidator();
        var request = new OneOfRuleRequest { Message = message };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectValid, result.IsValid);
    }

    public class OneOfRuleValidator : ModelValidator<OneOfRuleRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<OneOfRuleRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).Be().OneOf("Message must be a or b.", ["a", "b"]);
    }

    public class EqualToRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Fact]
    public void EqualTo_Should_Fail_When_Values_Differ()
    {
        // Arrange
        var validator = new EqualToValidator();
        var request = new EqualToRequest { Message = "not-hello" };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Message must equal Hello.", result.Errors![0].Message);
    }

    public class EqualToValidator : ModelValidator<EqualToRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<EqualToRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).Be().EqualTo("Hello", "Message must equal Hello.");
    }

    public class NoRulesRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Fact]
    public void ModelValidator_With_No_Rules_Should_Be_Valid_By_Default()
    {
        // Arrange
        var validator = new NoRulesValidator();
        var request = new NoRulesRequest { Message = null };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Errors);
    }

    public class NoRulesValidator : ModelValidator<NoRulesRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<NoRulesRequest> ruleBuilder)
        {
            // Deliberately empty: a validator registered for a pair with nothing to validate must still
            // pass every request through, not fail-closed.
        }
    }

    public class NotBeInRuleRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Theory]
    [InlineData("a", false)]
    [InlineData("c", true)]
    public void NotBe_In_Should_Fail_When_Value_Is_A_Member_Of_The_Provided_Values(string message, bool expectValid)
    {
        // Arrange: existing coverage only exercised Be().In, never the inverted NotBe().In.
        var validator = new NotBeInRuleValidator();
        var request = new NotBeInRuleRequest { Message = message };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectValid, result.IsValid);
    }

    public class NotBeInRuleValidator : ModelValidator<NotBeInRuleRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<NotBeInRuleRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).NotBe().In("Message must not be a or b.", ["a", "b"]);
    }

    public class NotBeOneOfRuleRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Theory]
    [InlineData("a", false)]
    [InlineData("c", true)]
    public void NotBe_OneOf_Should_Behave_As_An_Alias_For_NotBe_In(string message, bool expectValid)
    {
        // Arrange
        var validator = new NotBeOneOfRuleValidator();
        var request = new NotBeOneOfRuleRequest { Message = message };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectValid, result.IsValid);
    }

    public class NotBeOneOfRuleValidator : ModelValidator<NotBeOneOfRuleRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<NotBeOneOfRuleRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).NotBe().OneOf("Message must not be a or b.", ["a", "b"]);
    }

    public class NotBeEqualToRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Fact]
    public void NotBe_EqualTo_Should_Fail_When_Values_Are_Equal()
    {
        // Arrange: existing coverage only exercised the positive Be().EqualTo direction.
        var validator = new NotBeEqualToValidator();
        var request = new NotBeEqualToRequest { Message = "Hello" };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Message must not equal Hello.", result.Errors![0].Message);
    }

    public class NotBeEqualToValidator : ModelValidator<NotBeEqualToRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<NotBeEqualToRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).NotBe().EqualTo("Hello", "Message must not equal Hello.");
    }

    public class InRuleNoMessageRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Fact]
    public void In_Without_A_Message_Should_Produce_A_Null_Error_Message()
    {
        // Arrange: the no-message overload of In/OneOf never sets ValidationError.Message. Confirm that's
        // exactly what happens (null, not an empty string or a generated default) rather than assuming it.
        var validator = new InRuleNoMessageValidator();
        var request = new InRuleNoMessageRequest { Message = "c" };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.Errors![0].Message);
    }

    public class InRuleNoMessageValidator : ModelValidator<InRuleNoMessageRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<InRuleNoMessageRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).Be().In(["a", "b"]);
    }

    public class NullValuesRequest : IRequest<TestResponse>
    {
        public string? Message { get; set; }
    }

    [Fact]
    public async Task In_With_Null_Values_Should_Throw_ArgumentNullException_Naming_The_Values_Parameter()
    {
        // Arrange: null `values` previously reached `values.Contains(...)` unguarded, which does throw
        // ArgumentNullException (Enumerable.Contains null-checks its source) but names LINQ's own "source"
        // parameter rather than the rule's actual "values" argument — confusing for anyone debugging a
        // validator. The validator's constructor only registers the rule (AddRules runs synchronously in
        // ModelValidator's ctor); the null check can only fire once the rule actually executes.
        // Uses ValidateAsync directly (not the synchronous Validate) since Validate's Task.Wait() wraps any
        // exception in AggregateException, which is a separate, broader quirk unrelated to this guard.
        var validator = new NullValuesValidator();
        var request = new NullValuesRequest { Message = "a" };

        // Act
        Func<Task> act = () => validator.ValidateAsync(request);

        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(act);
        Assert.Equal("values", exception.ParamName);
    }

    public class NullValuesValidator : ModelValidator<NullValuesRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<NullValuesRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).Be().In(null!);
    }
}
