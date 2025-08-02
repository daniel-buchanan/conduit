using conduit.Pipes;

namespace conduit;

public interface IConduitConfigurationBuilder
{
    IConduitConfigurationBuilder AddPipeStage<TStage, T>() 
        where TStage: IPipeStage<T> 
        where T : IResponse;

    IConduitConfigurationBuilder RegisterHandler<THandler>();
}