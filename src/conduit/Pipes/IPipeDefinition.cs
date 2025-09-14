namespace conduit.Pipes;

/// <summary>
/// Defines the contract for a pipe definition, specifying the request type, response type, and the stages within the pipe.
/// </summary>
/// <typeparam name="TRequest">The type of the request handled by this pipe definition.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by this pipe definition.</typeparam>
public interface IPipeDefinition<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Gets the type of the request.
    /// </summary>
    Type RequestType { get; }
    /// <summary>
    /// Gets the type of the response.
    /// </summary>
    Type ResponseType { get; }
    /// <summary>
    /// Gets an enumerable collection of types representing the stages within the pipe.
    /// </summary>
    IEnumerable<Type> Stages { get; }
}

/// <summary>
/// Represents a concrete implementation of <see cref="IPipeDefinition{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class PipeDefinition<TRequest, TResponse> : IPipeDefinition<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Gets the type of the request.
    /// </summary>
    public Type RequestType { get; } = typeof(TRequest);
    /// <summary>
    /// Gets the type of the response.
    /// </summary>
    public Type ResponseType { get; } = typeof(TResponse);
    /// <summary>
    /// Gets an enumerable collection of types representing the stages within the pipe.
    /// </summary>
    public IEnumerable<Type> Stages { get; } = [];
}