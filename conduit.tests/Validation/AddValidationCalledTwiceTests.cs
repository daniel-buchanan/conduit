using conduit.logging;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.Validation;

public class AddValidationCalledTwiceTests
{
    [Fact]
    public void AddValidation_Called_Twice_On_The_Same_ConfigurationBuilder_Should_Throw()
    {
        // Arrange: each AddValidation call builds its own ValidationBuilder, so scan-collision detection
        // and configuration (ThrowIfValidatorNotFound, etc.) don't carry over between calls — calling it
        // twice on the same configuration is unsupported, not merely last-config-wins.
        var services = new ServiceCollection();

        // Act
        void Act() => services.AddConduit(c =>
        {
            c.AddValidation();
            c.AddValidation();
        }, new Mock<ILog>().Object);

        // Assert
        Assert.Throws<ValidationAlreadyConfiguredException>(Act);
    }
}
