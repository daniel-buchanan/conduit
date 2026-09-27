using BenchmarkDotNet.Attributes;
using conduit.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.benchmarks;

/// <summary>
/// Isolates <see cref="Conduit"/>'s per-call reflection cost — it looks up the closed
/// <c>IPipe&lt;,&gt;</c> type, does an untyped <c>GetService</c>, then finds and invokes the
/// send method via reflection (<c>Type.GetMethod</c> + <c>MethodInfo.Invoke</c>) — every single
/// dispatch, with no caching. Compares that against resolving and calling the same
/// <see cref="IPipe{TRequest,TResponse}"/> directly, with no facade in between.
/// </summary>
[MemoryDiagnoser]
public class ConduitFacadeBenchmarks
{
    private IConduit _conduit = null!;
    private IPipe<NoOpRequest, NoOpResponse> _pipe = null!;
    private NoOpRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<NoOpRequest, NoOpResponse>(p => p.AddHandler<NoOpRequestHandler>()), new NullLog());
        var provider = services.BuildServiceProvider();

        _conduit = provider.GetRequiredService<IConduit>();
        _pipe = provider.GetRequiredService<IPipe<NoOpRequest, NoOpResponse>>();
        _request = new NoOpRequest { Message = "hello" };
    }

    [Benchmark(Baseline = true)]
    public Task<NoOpResponse?> ViaPipeDirectly()
        => _pipe.PushAsync(_request);

    [Benchmark]
    public Task<NoOpResponse?> ViaConduitFacade()
        => _conduit.PushAsync(_request);
}
