using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.benchmarks;

/// <summary>
/// conduit.validation's AddValidation wires ValidationStage&lt;,&gt; in as a *default* pre-execution
/// stage: registered once as an open generic (<c>typeof(ValidationStage&lt;,&gt;)</c>) and contributed
/// to every handler-scanned pipe via the shared DefaultPipeConfiguration, materialized to a closed
/// type per request/response pair at pipe-build time. StageOverheadBenchmarks' extra stage is instead
/// added directly to one specific pipe via RegisterPipe/AddStage with an already-closed type. This
/// benchmark reproduces the *default-stage* wiring path exactly, but with NoOpStage instead of
/// ValidationStage, to see whether that wiring mechanism itself — independent of anything the
/// validator does — accounts for DispatchBenchmarks' NoValidation-to-WithValidation delta.
/// </summary>
[MemoryDiagnoser]
public class DefaultStageBenchmarks
{
    private IConduit _withoutDefaultStage = null!;
    private IConduit _withDefaultStage = null!;
    private NoOpRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        var withoutServices = new ServiceCollection();
        withoutServices.AddConduit(c => c.RegisterHandlersAsImplementedFrom<BenchmarkAssemblyMarker>(), new NullLog());
        _withoutDefaultStage = withoutServices.BuildServiceProvider().GetRequiredService<IConduit>();

        var withServices = new ServiceCollection();
        withServices.AddConduit(c =>
        {
            c.AddDescriptor(new ServiceDescriptor(typeof(NoOpStage<,>), typeof(NoOpStage<,>), ServiceLifetime.Transient));
            c.AddDefaultPreExecutionStage(typeof(NoOpStage<,>));
            c.RegisterHandlersAsImplementedFrom<BenchmarkAssemblyMarker>();
        }, new NullLog());
        _withDefaultStage = withServices.BuildServiceProvider().GetRequiredService<IConduit>();

        _request = new NoOpRequest { Message = "hello" };
    }

    [Benchmark(Baseline = true)]
    public Task<NoOpResponse?> WithoutDefaultStage()
        => _withoutDefaultStage.PushAsync(_request);

    [Benchmark]
    public Task<NoOpResponse?> WithNoOpAsDefaultStage()
        => _withDefaultStage.PushAsync(_request);
}
