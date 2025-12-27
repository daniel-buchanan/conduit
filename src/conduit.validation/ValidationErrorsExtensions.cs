using conduit.Pipes.Stages;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace conduit.validation;

public static class ValidationErrorsExtensions
{
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