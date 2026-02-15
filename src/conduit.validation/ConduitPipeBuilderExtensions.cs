using conduit.Configuration;

namespace conduit.validation;

/// <summary>
/// Provides extension methods for <see cref="IConduitPipeBuilder{TRequest, TResponse}"/> to add validation support.
/// </summary>
public static class ConduitPipeBuilderExtensions
{
    /// <summary>
    /// Explicitly adds validation to the specified pipeline for the request model.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="self">The pipe builder.</param>
    /// <returns>The pipe builder for method chaining.</returns>
    public static IConduitPipeBuilder<TRequest, TResponse> WithValidation<TRequest, TResponse>(this IConduitPipeBuilder<TRequest, TResponse> self) 
        where TRequest : class, IRequest<TResponse> 
        where TResponse : class
    {
        self.AddStage<ValidationStage<TRequest, TResponse>>();
        return self;
    }

    /// <summary>
    /// Specifically excludes the specified pipeline from any global model validation.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="self">The pipe builder.</param>
    /// <returns>The pipe builder for method chaining.</returns>
    public static IConduitPipeBuilder<TRequest, TResponse> ExcludeValidation<TRequest, TResponse>(this IConduitPipeBuilder<TRequest, TResponse> self)
        where TRequest : class, IRequest<TResponse> 
        where TResponse : class
    {
        (self as ConduitPipeBuilder<TRequest, TResponse>)?.ExcludeFromValidation();
        return self;
    }
}