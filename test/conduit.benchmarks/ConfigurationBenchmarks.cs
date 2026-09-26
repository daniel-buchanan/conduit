using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.benchmarks;

/// <summary>
/// Cost of wiring up Conduit: assembly-scan registration plus building the
/// <see cref="IServiceProvider"/>. Runs once per app/test-host startup, not per request,
/// but the reflection scan in <c>RegisterHandlersAsImplementedFrom</c> makes it worth tracking
/// separately from steady-state dispatch cost (see <see cref="DispatchBenchmarks"/>).
/// </summary>
[MemoryDiagnoser]
public class ConfigurationBenchmarks
{
    [Benchmark]
    public IServiceProvider RegisterHandlersAsImplementedFrom()
    {
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterHandlersAsImplementedFrom<BenchmarkAssemblyMarker>(), new NullLog());
        return services.BuildServiceProvider();
    }

    [Benchmark]
    public IServiceProvider RegisterHandler_Explicit()
    {
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterHandler<NoOpRequest, NoOpResponse, NoOpRequestHandler>(), new NullLog());
        return services.BuildServiceProvider();
    }
}
