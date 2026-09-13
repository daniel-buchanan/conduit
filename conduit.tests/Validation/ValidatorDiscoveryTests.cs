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
}
