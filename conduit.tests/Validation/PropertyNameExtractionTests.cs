using conduit.validation;
using Xunit;

namespace conduit.tests.Validation;

public class PropertyNameExtractionTests
{
    public class Address
    {
        public string? City { get; set; }
    }

    public class RequestWithAddress : IRequest<object>
    {
        public string? Name { get; set; }
        public Address? Address { get; set; }
    }

    [Fact]
    public void Should_Sets_PropertyName_From_Top_Level_Member_Access()
    {
        // Arrange
        var validator = new TopLevelValidator();
        var request = new RequestWithAddress { Address = null };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Address", result.Errors![0].PropertyName);
    }

    public class TopLevelValidator : ModelValidator<RequestWithAddress, object>
    {
        protected override void AddRules(IRuleBuilder<RequestWithAddress> ruleBuilder)
            => ruleBuilder.Should(x => x.Address).NotBe().Null();
    }

    [Fact]
    public void Should_Sets_PropertyName_To_Full_Dotted_Path_For_Nested_Member_Access()
    {
        // Arrange
        var validator = new NestedValidator();
        var request = new RequestWithAddress { Address = new Address { City = null } };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Address.City", result.Errors![0].PropertyName);
    }

    public class NestedValidator : ModelValidator<RequestWithAddress, object>
    {
        protected override void AddRules(IRuleBuilder<RequestWithAddress> ruleBuilder)
            => ruleBuilder.Should(x => x.Address!.City).NotBe().Null();
    }

    [Fact]
    public void Should_Throws_When_Expression_Contains_A_Method_Call()
    {
        // Arrange
        var ruleBuilder = new RuleBuilder<RequestWithAddress>();

        // Act
        var act = () => ruleBuilder.Should(x => x.Name!.Trim());

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Should_String_Overload_Accepts_Nullable_Property_Without_Null_Forgiving_Operator()
    {
        // Arrange
        var validator = new NullableStringValidator();
        var request = new RequestWithAddress { Name = null };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Name", result.Errors![0].PropertyName);
    }

    public class NullableStringValidator : ModelValidator<RequestWithAddress, object>
    {
        protected override void AddRules(IRuleBuilder<RequestWithAddress> ruleBuilder)
            => ruleBuilder.Should(x => x.Name).NotBe().Null();
    }
}
