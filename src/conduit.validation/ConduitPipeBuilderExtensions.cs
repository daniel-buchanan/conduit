using conduit.Configuration;

namespace conduit.validation;

/// <summary>
/// Provides extension methods for <see cref="IConduitPipeBuilder{TRequest, TResponse}"/> to add validation support.
/// </summary>
public static class ConduitPipeBuilderExtensions
{
    /// <summary>
    /// Explicitly adds validation to the specified pipeline for the request model, at this exact position
    /// in the stage order. Also excludes the pipe from the default pre-execution validation stage (see
    /// <see cref="IConduitPipeBuilder{TRequest,TResponse}.ExcludeValidation"/>), regardless of whether the
    /// caller already called it — otherwise the request would be validated twice: once by the default stage
    /// and once by this explicit one. See ADR-0009.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="self">The pipe builder.</param>
    /// <returns>The pipe builder for method chaining.</returns>
    public static IConduitPipeBuilder<TRequest, TResponse> WithValidation<TRequest, TResponse>(this IConduitPipeBuilder<TRequest, TResponse> self)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
    {
        self.ExcludeValidation();
        self.AddStage<ValidationStage<TRequest, TResponse>>();
        return self;
    }
}