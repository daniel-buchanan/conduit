using conduit.Exceptions;
using conduit.logging;
using conduit.tests.Handlers;
using conduit.tests.Stages;
using conduit.validation;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests;

public class ValidatorTests
{
    private readonly IServiceProvider _provider;
    private readonly Mock<ILog> _loggerMock;

    public ValidatorTests()
    {
        _loggerMock = new Mock<ILog>();
        var services = new ServiceCollection();
        services.AddConduit(c =>
        {
            c.AddValidation(vb => vb.ThrowIfValidatorNotFound().WithValidatorsFromAssembly<ValidatorTests>());
            c.RegisterHandlersAsImplementedFrom<ValidatorTests>();
        }, _loggerMock.Object);
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ValidationFailedShouldThrow()
    {
        // Arrange
        var testRequest = new ValidatingRequest();
        var conduit = _provider.GetRequiredService<IConduit>();
        
        // Act
        Func<Task> method = async () => await conduit.PushWithDebugAsync(testRequest, CancellationToken.None);

        // Assert
        await method.Should().ThrowAsync<ValidationFailedException>();
    }
    
}