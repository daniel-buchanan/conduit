using BenchmarkDotNet.Attributes;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.benchmarks;

/// <summary>
/// Steady-state cost of <see cref="IConduit.PushAsync{TResponse}"/> for a plain handler pipe
/// versus a pipe with the default validation stage in front of it.
/// </summary>
[MemoryDiagnoser]
public class DispatchBenchmarks
{
    private IConduit _plainConduit = null!;
    private IConduit _validatedConduit = null!;
    private NoOpRequest _noOpRequest = null!;
    private ValidatedRequest _validatedRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        var plainServices = new ServiceCollection();
        plainServices.AddConduit(c => c.RegisterHandlersAsImplementedFrom<BenchmarkAssemblyMarker>(), new NullLog());
        _plainConduit = plainServices.BuildServiceProvider().GetRequiredService<IConduit>();

        var validatedServices = new ServiceCollection();
        validatedServices.AddConduit(c =>
        {
            c.AddValidation(v => v.WithValidatorsFromAssembly<BenchmarkAssemblyMarker>());
            c.RegisterHandlersAsImplementedFrom<BenchmarkAssemblyMarker>();
        }, new NullLog());
        _validatedConduit = validatedServices.BuildServiceProvider().GetRequiredService<IConduit>();

        _noOpRequest = new NoOpRequest { Message = "hello" };
        _validatedRequest = new ValidatedRequest { Message = "hello" };
    }

    [Benchmark(Baseline = true)]
    public Task<NoOpResponse?> Dispatch_NoValidation()
        => _plainConduit.PushAsync(_noOpRequest);

    [Benchmark]
    public Task<ValidatedResponse?> Dispatch_WithValidation()
        => _validatedConduit.PushAsync(_validatedRequest);
}
