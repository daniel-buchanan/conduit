using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Configuration;

/// <summary>
/// Builds a conduit pipe for a specific request and response type.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class ConduitPipeBuilder<TRequest, TResponse> 
    : IConduitPipeBuilder<TRequest, TResponse> 
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    private readonly PipeDescriptor<TRequest, TResponse> _descriptor = new();

    /// <inheritdoc />
    public IConduitPipeBuilder<TRequest, TResponse> AddStage(Type stage)
    {
        _descriptor.Stages.Add(new StageDescriptor(typeof(TRequest), typeof(TResponse),stage));
        return this;
    }

    /// <inheritdoc />
    public IConduitPipeBuilder<TRequest, TResponse> AddHandler<THandler>() 
        where THandler : IRequestHandler<TRequest, TResponse>
    {
        var handlerDef = new StageDescriptor(
            typeof(TRequest), 
            typeof(TResponse), 
            typeof(THandler), 
            typeof(IRequestHandler<TRequest, TResponse>));
        _descriptor.Stages.Add(handlerDef);
        return this;
    }

    /// <inheritdoc />
    public IConduitPipeBuilder<TRequest, TResponse> AddStage<TStage>() 
        where TStage : IPipeStage<TRequest, TResponse> 
        => AddStage(typeof(TStage));

    /// <summary>
    /// Get the descriptor for this builder.
    /// </summary>
    /// <returns>The pipe descriptor built by this builder.</returns>
    public PipeDescriptor GetDescriptor() => _descriptor;
    
    /// <summary>
    /// Exclude this pipeline from Model Validation
    /// </summary>
    internal void ExcludeFromValidation() => _descriptor.ExcludeFromValidation();
}