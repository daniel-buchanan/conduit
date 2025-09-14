namespace conduit;

/// <summary>
/// Represents a request that can be processed by the Conduit system.
/// </summary>
/// <typeparam name="TResult">The type of the result expected from the request.</typeparam>
public interface IRequest<TResult> where TResult : class;
