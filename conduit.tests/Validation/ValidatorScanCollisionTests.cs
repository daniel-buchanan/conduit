using conduit.validation;
using Xunit;

namespace conduit.tests.Validation;

public class ValidatorScanCollisionTests
{
    [Fact]
    public void WithValidatorsFromAssembly_Should_Throw_When_The_Same_Assembly_Is_Scanned_Twice()
    {
        // Arrange: relies on this assembly already containing at least one ModelValidator (e.g.
        // ExcludeValidationTests.ExcludableRequestValidator) rather than a fixture defined in this file,
        // so this test can't accidentally collide with unrelated scan tests elsewhere in the suite.
        var builder = new ValidationBuilder();
        builder.WithValidatorsFromAssembly<ValidatorScanCollisionTests>();

        // Act
        void Act() => builder.WithValidatorsFromAssembly<ValidatorScanCollisionTests>();

        // Assert: re-scanning the same assembly rediscovers the same (TRequest, TResponse) pairs. ADR-0003
        // says that must throw rather than silently re-registering — last-write-wins is reserved for
        // explicit WithValidatorFor calls, not scans.
        Assert.Throws<ValidatorAlreadyRegisteredException>(Act);
    }

    [Fact]
    public void WithValidatorFor_Should_Not_Throw_When_Overriding_A_Scanned_Validator_For_The_Same_Pair()
    {
        // Arrange
        var builder = new ValidationBuilder();
        builder.WithValidatorsFromAssembly<ValidatorScanCollisionTests>();

        // Act: an explicit registration for a pair the scan already discovered is a deliberate override,
        // per ADR-0003, and must never throw regardless of how the existing entry got there.
        void Act() => builder.WithValidatorFor(new ExcludeValidationTests.ExcludableRequestValidator());

        // Assert
        var exception = Record.Exception(Act);
        Assert.Null(exception);
    }
}
