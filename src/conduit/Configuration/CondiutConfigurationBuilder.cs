using conduit.Pipes;

namespace conduit.Configuration;

public class CondiutConfigurationBuilder : IConduitConfigurationBuilder
{
    private readonly List<Type> _pipeStages = new();
    private readonly List<Type> _handlers = new();

    public IConduitConfigurationBuilder AddPipeStage<TStage, T>()
        where TStage : IPipeStage<T>
        where T : class, IResponse
    {
        _pipeStages.Add(typeof(TStage));
        return this;
    }

    public IConduitConfigurationBuilder RegisterHandler<THandler>()
    {
        _handlers.Add(typeof(THandler));
        return this;
    }
}