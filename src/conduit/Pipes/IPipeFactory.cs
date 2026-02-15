namespace conduit.Pipes;

public interface IPipeFactory
{
    /// <summary>
    /// Creates a new pipe for the specified request and response types, from a registered pipe configuration.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <returns>The constructed pipe.</returns>
    IPipe<TRequest, TResponse> Create<TRequest, TResponse>() 
        where TResponse : class 
        where TRequest : class, IRequest<TResponse>;
    
    /// <summary>
    /// Creates a new pipe for the specified request and response types, from a registered pipe configuration.
    /// </summary>
    /// <param name="requestType">The request type.</param>
    /// <param name="responseType">The response type.</param>
    /// <returns>The constructed pipe.</returns>
    IPipe Create(Type requestType, Type responseType);
}