using conduit.common;
using conduit.Exceptions;
using conduit.Pipes;

namespace conduit;

/// <summary>
/// Represents the main entry point for sending requests through the Conduit system.
/// This class orchestrates the discovery and execution of appropriate pipes for incoming requests.
/// </summary>
/// <param name="provider">The service provider used to resolve pipe instances.</param>
public class Conduit(IServiceProvider provider) : IConduit
{
    /// <inheritdoc/>
    public async Task<TResponse?> PushAsync<TResponse>(
        IRequest<TResponse> request, 
        CancellationToken cancellationToken = default)
        where TResponse : class
        => await PushInternalAsync<TResponse, TResponse>(
            nameof(IPipe<IRequest<TResponse>,TResponse>.PushAsync), 
            request,
            cancellationToken);

    /// <inheritdoc/>
    public async Task<DebugResult<TResponse?>> PushWithDebugAsync<TResponse>(
        IRequest<TResponse> request, 
        CancellationToken cancellationToken = default) where TResponse : class 
        => (await PushInternalAsync<DebugResult<TResponse>, TResponse>(
            nameof(IPipe<IRequest<TResponse>,TResponse>.PushWithDebugAsync), 
            request,
            cancellationToken))!;

    private async Task<T?> PushInternalAsync<T, TResponse>(
        string sendMethod, 
        IRequest<TResponse> request, 
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var pipeType = typeof(IPipe<,>);
        var pipeGenericType = pipeType.MakeGenericType(requestType, responseType);
        
        var pipe = provider.GetService(pipeGenericType);
        if (pipe is null)
            throw new PipeNotFoundException($"Implementation for Pipe {pipeGenericType.GetGenericName()} not found");

        var method = pipeGenericType.GetMethod(sendMethod);
        var result = await ((Task<T?>)method?.Invoke(pipe, [request, cancellationToken])!)!;
        return result;
    }
}