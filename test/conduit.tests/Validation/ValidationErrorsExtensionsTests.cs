using conduit.Pipes.Stages;
using conduit.validation;
using Xunit;

namespace conduit.tests.Validation;

public class ValidationErrorsExtensionsTests
{
    [Fact]
    public void ToModelState_Should_Add_One_Model_Error_Per_Validation_Error()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Name", "Name is required."),
            new ValidationError("Age", "Age must be positive."),
        };

        // Act
        var modelState = errors.ToModelState();

        // Assert
        Assert.True(modelState.ContainsKey("Name"));
        Assert.True(modelState.ContainsKey("Age"));
        Assert.Equal("Name is required.", modelState["Name"]!.Errors[0].ErrorMessage);
        Assert.Equal("Age must be positive.", modelState["Age"]!.Errors[0].ErrorMessage);
    }

    [Fact]
    public void ToModelState_Should_Add_Multiple_Errors_For_The_Same_Property()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Name", "Name is required."),
            new ValidationError("Name", "Name must be shorter than 50 characters."),
        };

        // Act
        var modelState = errors.ToModelState();

        // Assert
        Assert.Equal(2, modelState["Name"]!.Errors.Count);
    }

    [Fact]
    public void ToModelState_Should_Return_An_Empty_State_For_No_Errors()
    {
        // Arrange
        var errors = Array.Empty<ValidationError>();

        // Act
        var modelState = errors.ToModelState();

        // Assert
        Assert.True(modelState.IsValid);
    }
}
