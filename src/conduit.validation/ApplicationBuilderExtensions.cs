using Microsoft.AspNetCore.Builder;

namespace conduit.validation;

/// <summary>
/// Provides extension methods for <see cref="IApplicationBuilder"/> to add Conduit validation middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Conduit validation exception handler middleware to the application pipeline.
    /// This middleware handles validation failures and other Conduit exceptions.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder AddConduitValidation(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<ConduitValidationExceptionHandler>();
        return builder;
    }
}