using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.benchmarks;

/// <summary>
/// Isolates the fixed per-stage cost of the pipe loop (DI resolution, logging calls, stage-type
/// name formatting in <c>Pipe.ExecuteStage</c>) from anything validation-specific, by comparing a
/// one-stage pipe against an otherwise-identical two-stage pipe where the second stage is a no-op.
/// Compare the OneStage-to-TwoStage delta here against DispatchBenchmarks' NoValidation-to-
/// WithValidation delta: if they're close, the extra cost is "one more stage," not validation
/// logic; if WithValidation costs much more than one extra no-op stage, the validator/rule path
/// itself is where to look.
/// </summary>
[MemoryDiagnoser]
public class StageOverheadBenchmarks
{
    private IConduit _oneStageConduit = null!;
    private IConduit _twoStageConduit = null!;
    private NoOpRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        var oneStageServices = new ServiceCollection();
        oneStageServices.AddConduit(c => c.RegisterPipe<NoOpRequest, NoOpResponse>(p => p.AddHandler<NoOpRequestHandler>()), new NullLog());
        _oneStageConduit = oneStageServices.BuildServiceProvider().GetRequiredService<IConduit>();

        var twoStageServices = new ServiceCollection();
        twoStageServices.AddConduit(c => c.RegisterPipe<NoOpRequest, NoOpResponse>(p => p
            .AddStage<NoOpStage<NoOpRequest, NoOpResponse>>()
            .AddHandler<NoOpRequestHandler>()), new NullLog());
        _twoStageConduit = twoStageServices.BuildServiceProvider().GetRequiredService<IConduit>();

        _request = new NoOpRequest { Message = "hello" };
    }

    [Benchmark(Baseline = true)]
    public Task<NoOpResponse?> OneStage()
        => _oneStageConduit.PushAsync(_request);

    [Benchmark]
    public Task<NoOpResponse?> TwoStages_SecondIsNoOp()
        => _twoStageConduit.PushAsync(_request);
}
