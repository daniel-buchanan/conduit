using conduit.Pipes.Stages;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace conduit.validation;

/// <summary>
/// Provides extension methods for working with validation errors in the Conduit system.
/// </summary>
public static class ValidationErrorsExtensions
{
    /// <summary>
    /// Converts an array of validation errors to an ASP.NET Core ModelStateDictionary.
    /// </summary>
    /// <param name="self">The array of validation errors.</param>
    /// <returns>A ModelStateDictionary containing the validation errors.</returns>
    public static ModelStateDictionary ToModelState(this ValidationError[] self)
    {
        var state = new ModelStateDictionary();
        foreach (var error in self)
        {
            state.AddModelError(error.PropertyName, error.Message);
        }

        return state;
    }
}