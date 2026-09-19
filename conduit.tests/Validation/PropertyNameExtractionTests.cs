using conduit.validation;
using Xunit;

namespace conduit.tests.Validation;

public class PropertyNameExtractionTests
{
    public class Address
    {
        public string? City { get; set; }
    }

    // Kept separate from the validators' own request types below: this one backs only the direct
    // RuleBuilder expression test (no ModelValidator involved), while each validator gets its own request
    // type so a WithValidatorsFromAssembly scan elsewhere in the suite never sees two of them target the
    // same (TRequest, TResponse) pair and trips the ValidatorAlreadyRegisteredException guard from ADR-0003.
    public class RequestWithAddress : IRequest<object>
    {
        public string? Name { get; set; }
        public Address? Address { get; set; }
    }

    public class TopLevelRequest : IRequest<object>
    {
        public Address? Address { get; set; }
    }

    [Fact]
    public void Should_Sets_PropertyName_From_Top_Level_Member_Access()
    {
        // Arrange
        var validator = new TopLevelValidator();
        var request = new TopLevelRequest { Address = null };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Address", result.Errors![0].PropertyName);
    }

    public class TopLevelValidator : ModelValidator<TopLevelRequest, object>
    {
        protected override void AddRules(IRuleBuilder<TopLevelRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Address).NotBe().Null();
    }

    public class NestedRequest : IRequest<object>
    {
        public Address? Address { get; set; }
    }

    [Fact]
    public void Should_Sets_PropertyName_To_Full_Dotted_Path_For_Nested_Member_Access()
    {
        // Arrange
        var validator = new NestedValidator();
        var request = new NestedRequest { Address = new Address { City = null } };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Address.City", result.Errors![0].PropertyName);
    }

    public class NestedValidator : ModelValidator<NestedRequest, object>
    {
        protected override void AddRules(IRuleBuilder<NestedRequest> ruleBuilder)
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

    public class NullableStringRequest : IRequest<object>
    {
        public string? Name { get; set; }
    }

    [Fact]
    public void Should_String_Overload_Accepts_Nullable_Property_Without_Null_Forgiving_Operator()
    {
        // Arrange
        var validator = new NullableStringValidator();
        var request = new NullableStringRequest { Name = null };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Name", result.Errors![0].PropertyName);
    }

    public class NullableStringValidator : ModelValidator<NullableStringRequest, object>
    {
        protected override void AddRules(IRuleBuilder<NullableStringRequest> ruleBuilder)
            => ruleBuilder.Should(x => x.Name).NotBe().Null();
    }
}
