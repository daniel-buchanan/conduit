using conduit.Pipes.Stages;
using conduit.tests.Handlers;
using conduit.validation;
using conduit.validation.Rules;
using Xunit;

namespace conduit.tests.Validation;

public class CustomRuleTests
{
    public class BookingRequest : IRequest<TestResponse>
    {
        public string? Guest { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    /// <summary>A cross-field rule, which <c>Should(...)</c> can't express because it reads two properties.</summary>
    public class EndsAfterStartRule : IRule<BookingRequest>
    {
        public ValidationResult<BookingRequest> Validate(BookingRequest request)
            => request.To > request.From
                ? ValidationResult.WithSuccess(request)
                : ValidationResult.WithFailure(request, [new ValidationError(nameof(BookingRequest.To), "To must be after From.")]);

        public Task<ValidationResult<BookingRequest>> ValidateAsync(BookingRequest request)
            => Task.FromResult(Validate(request));
    }

    public class BookingValidator : ModelValidator<BookingRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<BookingRequest> ruleBuilder)
        {
            ruleBuilder.Should(x => x.Guest).NotBe().NullOrWhitespace("Guest is required.");
            ruleBuilder.AddRule(new EndsAfterStartRule());
        }
    }

    [Fact]
    public async Task A_Custom_Rule_Added_With_AddRule_Runs_Alongside_Should_Rules()
    {
        // Arrange
        var validator = new BookingValidator();
        var request = new BookingRequest { Guest = null, From = new DateTime(2026, 1, 2), To = new DateTime(2026, 1, 1) };

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(["Guest is required.", "To must be after From."], result.Errors!.Select(e => e.Message));
    }

    [Fact]
    public async Task A_Passing_Custom_Rule_Adds_No_Error()
    {
        // Arrange
        var validator = new BookingValidator();
        var request = new BookingRequest { Guest = "Ada", From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 2) };

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    // Own request type: a validator scan rejects two validators for one request/response pair.
    public class NullRuleRequest : IRequest<TestResponse>;

    public class NullRuleValidator : ModelValidator<NullRuleRequest, TestResponse>
    {
        protected override void AddRules(IRuleBuilder<NullRuleRequest> ruleBuilder)
            => ruleBuilder.AddRule(null!);
    }

    [Fact]
    public void Adding_A_Null_Rule_Throws_When_The_Validator_Is_Built()
    {
        // Act
        var exception = Record.Exception(() => new NullRuleValidator());

        // Assert: fail at construction, not with a NullReferenceException on the first request.
        var argumentNull = Assert.IsType<ArgumentNullException>(exception);
        Assert.Equal("rule", argumentNull.ParamName);
    }
}
