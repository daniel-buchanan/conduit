using conduit.common;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Configuration;

/// <summary>
/// Represents a generic descriptor for a Conduit pipe, with strongly typed request and response types.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class PipeDescriptor<TRequest, TResponse>() : PipeDescriptor(typeof(TRequest), typeof(TResponse))
    where TRequest : class, IRequest<TResponse>
    where TResponse : class;

/// <summary>
/// Represents a descriptor for a Conduit pipe, containing information about the request type, response type, and its stages.
/// </summary>
/// <param name="request">The type of the request.</param>
/// <param name="response">The type of the response.</param>
public class PipeDescriptor(Type request, Type response)
{
    /// <summary>
    /// Gets the type of the request.
    /// </summary>
    public Type RequestType { get; } = request;

    /// <summary>
    /// Gets the type of the response.
    /// </summary>
    public Type ResponseType { get; } = response;

    /// <summary>
    /// Gets the list of service descriptors representing the stages in the pipe.
    /// </summary>
    public List<StageDescriptor> Stages { get; } = new();

    /// <summary>
    /// Get the hash for this pipe descriptor.
    /// </summary>
    public string GetHash()
        => HashUtil.Instance.TypeNameHash(RequestType, ResponseType);

    /// <summary>
    /// Whether this pipe is excluded from Model Validation.
    /// </summary>
    public bool ExcludeValidation { get; private set; } = false;
    
    /// <summary>
    /// Exclude this pipe from Model Validation.
    /// </summary>
    public void ExcludeFromValidation() => ExcludeValidation = true;
}