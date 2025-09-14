using conduit.Pipes;

namespace conduit.Configuration;

/// <summary>
/// Defines the contract for the Conduit configuration.
/// </summary>
public interface IConduitConfiguration
{
    /// <summary>
    /// Gets an enumerable collection of pipe descriptors, each representing a configured pipe.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="PipeDescriptor"/> objects.</returns>
    IEnumerable<PipeDescriptor> GetPipes();

    /// <summary>
    /// Adds a pipe stage to the Conduit configuration.
    /// </summary>
    /// <typeparam name="TStage">The type of the pipe stage to add.</typeparam>
    /// <typeparam name="TRequest">The type of the request handled by the pipe stage.</typeparam>
    /// <typeparam name="TResponse">The type of the response produced by the pipe stage.</typeparam>
    void AddPipeStage<TStage, TRequest, TResponse>()
        where TStage : IPipeStage<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
        where TResponse : class;

    /// <summary>
    /// Adds a pipe stage to the Conduit configuration using non-generic types.
    /// </summary>
    /// <param name="requestType">The type of the request.</param>
    /// <param name="implementationType">The concrete implementation type of the pipe stage.</param>
    /// <param name="interfaceType">The interface type of the pipe stage (optional).</param>
    void AddPipeStage(Type requestType, Type implementationType, Type? interfaceType = null);
}