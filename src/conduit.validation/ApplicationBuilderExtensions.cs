using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;

namespace conduit.validation;

public static class ApplicationBuilderExtensions
{
    public static ApplicationBuilder AddConduitValidation(this ApplicationBuilder builder)
    {
        builder.UseMiddleware<ConduitValidationExceptionHandler>();
        return builder;
    }
}