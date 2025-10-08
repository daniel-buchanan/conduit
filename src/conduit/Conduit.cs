using conduit.common;
using conduit.Exceptions;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit;

/// <summary>
/// Represents the main entry point for sending requests through the Conduit system.
/// This class orchestrates the discovery and execution of appropriate pipes for incoming requests.
/// </summary>
/// <param name="provider">The service provider used to resolve pipe instances.</param>
public class Conduit(IServiceProvider provider) : IConduit
{
    /// <summary>
    /// Pushes a request through the Conduit pipeline and awaits a response.
    /// This method dynamically resolves the appropriate pipe for the given request and executes it.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response expected from the request.</typeparam>
    /// <param name="request">The request to be processed.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
    /// <exception cref="PipeNotFoundException">Thrown when a suitable pipe for the request type cannot be found.</exception>
    public async Task<TResponse?> PushAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var pipeType = typeof(IPipe<,>);
        var pipeGenericType = pipeType.MakeGenericType(requestType, responseType);
        
        var pipe = provider.GetService(pipeGenericType);
        if (pipe is null)
            throw new PipeNotFoundException($"Implementation for Pipe {pipeGenericType.GetGenericName()} not found");

        var method = pipeGenericType.GetMethod("SendAsync");
        var result = await ((Task<TResponse?>)method?.Invoke(pipe, [request, cancellationToken])!)!;
        return result;
    }
}