using System.Linq;
using conduit.validation;
using Xunit;

namespace conduit.tests.Validation;

public class ValidatorDiscoveryTests
{
    [Fact]
    public void GetRequestResponseTypes_Should_Throw_A_Clear_Error_For_A_Type_That_Does_Not_Derive_From_ModelValidator()
    {
        // Arrange: any type whose base type isn't a closed ModelValidator<TRequest, TResponse> stands in here
        // for a validator that implements IModelValidator<,> directly instead of deriving from the base class.

        // Act
        void Act() => ValidationBuilder.GetRequestResponseTypes(typeof(string));

        // Assert: this previously caused an opaque IndexOutOfRangeException instead of a clear error.
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains("String", exception.Message);
    }

    [Fact]
    public void WithValidatorsFromAssembly_Generic_Overload_Should_Discover_The_Same_Descriptors_As_The_Assembly_Overload()
    {
        // Arrange: the generic <TLocator> overload is documented as delegating to the assembly overload via
        // typeof(TLocator).Assembly. Confirm that directly rather than only ever exercising it indirectly
        // through other tests' pipe/handler setups.
        var byType = new ValidationBuilder();
        var byAssembly = new ValidationBuilder();

        // Act
        byType.WithValidatorsFromAssembly<ValidatorDiscoveryTests>();
        byAssembly.WithValidatorsFromAssembly(typeof(ValidatorDiscoveryTests).Assembly);

        // Assert
        var byTypeDescriptors = byType.Build().Select(d => (d.ServiceType, d.ImplementationType)).ToArray();
        var byAssemblyDescriptors = byAssembly.Build().Select(d => (d.ServiceType, d.ImplementationType)).ToArray();
        Assert.Equal(byAssemblyDescriptors, byTypeDescriptors);
        Assert.NotEmpty(byTypeDescriptors);
    }
}
