using conduit.validation;
using Xunit;

namespace conduit.tests.Validation;

public class ShouldStringBuilderTests
{
    public class WhitespaceRequest : IRequest<object>
    {
        public string? Message { get; set; }
    }

    public class WhitespaceValidator : ModelValidator<WhitespaceRequest, object>
    {
        protected override void AddRules(IRuleBuilder<WhitespaceRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).NotBe().NullOrWhitespace("Message is required.");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("")]
    public void NullOrWhitespace_Should_Fail_For_Whitespace_Only_Input(string message)
    {
        // Arrange: existing coverage only exercised the null case, not a non-null, whitespace-only string.
        var validator = new WhitespaceValidator();
        var request = new WhitespaceRequest { Message = message };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Message is required.", result.Errors![0].Message);
    }

    [Fact]
    public void NullOrWhitespace_Should_Succeed_For_Non_Whitespace_Input()
    {
        // Arrange
        var validator = new WhitespaceValidator();
        var request = new WhitespaceRequest { Message = "hello" };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    public class BeWhitespaceRequest : IRequest<object>
    {
        public string? Message { get; set; }
    }

    public class BeWhitespaceValidator : ModelValidator<BeWhitespaceRequest, object>
    {
        protected override void AddRules(IRuleBuilder<BeWhitespaceRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Message).Be().NullOrWhitespace("Message must be blank.");
    }

    [Fact]
    public void Be_NullOrWhitespace_Should_Fail_For_Non_Whitespace_Input()
    {
        // Arrange: existing coverage only exercised the inverted NotBe().NullOrWhitespace direction.
        var validator = new BeWhitespaceValidator();
        var request = new BeWhitespaceRequest { Message = "hello" };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Message must be blank.", result.Errors![0].Message);
    }

    [Fact]
    public void Be_NullOrWhitespace_Should_Succeed_For_Null_Input()
    {
        // Arrange
        var validator = new BeWhitespaceValidator();
        var request = new BeWhitespaceRequest { Message = null };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }
}
