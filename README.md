# Conduit

A MediatR-style request/response mediator for .NET. Consumers define `IRequest<TResponse>` messages and `IRequestHandler` implementations; `IConduit` routes each request to its handler through a configurable pipeline, without callers taking a direct dependency on the handler.

`conduit`, `conduit.common`, `conduit.logging`, and `conduit.validation` target `netstandard2.0;net10.0`. `conduit.aspnetcore.validation` targets `net10.0` only.

## Install

```
dotnet add package Conduit
```

> Not yet published to NuGet — this is the intended install path once it is. Until then, reference the projects under `src/` directly.

## Quick start

Define a request and its handler:

```csharp
public class GetGreeting : IRequest<GreetingResponse>
{
    public required string Name { get; init; }
}

public class GreetingResponse
{
    public required string Message { get; init; }
}

public class GetGreetingHandler(ILog logger) : RequestHandler<GetGreeting, GreetingResponse>(logger)
{
    public override Task<GreetingResponse> HandleAsync(GetGreeting request, CancellationToken cancellationToken = default)
        => Task.FromResult(new GreetingResponse { Message = $"Hello, {request.Name}!" });
}
```

Register it and push a request through the pipeline:

```csharp
services.AddConduit(config =>
{
    config.RegisterHandler<GetGreeting, GreetingResponse, GetGreetingHandler>();
    // or scan an assembly for every handler at once:
    // config.RegisterHandlersAsImplementedFrom<Program>();
});

var response = await conduit.PushAsync(new GetGreeting { Name = "World" });
```

`PushAsync` throws `PipeNotFoundException` if no pipe is registered for the request/response pair. `PushWithDebugAsync` returns the same response wrapped with per-stage timing.

## Validation

The optional `conduit.validation` package adds a `ValidationStage` that runs before your handler:

```csharp
public class GetGreetingValidator : ModelValidator<GetGreeting, GreetingResponse>
{
    protected override void AddRules(IRuleBuilder<GetGreeting> ruleBuilder)
    {
        ruleBuilder.Should(x => x.Name).NotBe().Null();
        ruleBuilder.Should(x => x.Name).NotBe().NullOrWhitespace();
    }
}
```

```csharp
services.AddConduit(config =>
{
    config.RegisterHandlersAsImplementedFrom<Program>();
    config.AddValidation(); // scans loaded assemblies for ModelValidator<,> implementations
});

app.AddConduitValidation(); // ASP.NET Core middleware: turns ValidationFailedException into a ProblemDetails response
```

A failing validator throws `ValidationFailedException` (aggregating every failed rule, not just the first). Opt a specific pipe out with `ExcludeValidation()` on `RegisterHandler`'s `configure` callback or `RegisterPipe`'s builder.

## Concepts and vocabulary

`Conduit` → `Pipe` → `Stage` is the core plumbing metaphor (deliberately not MediatR's `Send`/pipeline behavior naming). Full canonical vocabulary, including terms to avoid, lives in [CONTEXT.md](CONTEXT.md).

## Design decisions

Significant design decisions are recorded as ADRs in [docs/adr/](docs/adr/).

## Benchmarks

Measured with [BenchmarkDotNet](https://benchmarkdotnet.org/) v0.14.0 on an Apple M1 (8 cores), .NET 10.0.1, macOS 27.0. Reproduce with:

```
dotnet run -c Release --project test/conduit.benchmarks -- --filter '*'
```

**Dispatch path** — cost of each layer between calling code and a handler:

| Method | Mean | Allocated | vs. baseline |
|---|---:|---:|---:|
| Direct `Pipe` call (no facade) | 864.2 ns | 1.84 KB | 1.00x |
| Via `IConduit` facade | 1,184.6 ns | 2.20 KB | 1.37x |
| One stage, no validation | 1.131 μs | 2.17 KB | 1.00x |
| + `ValidationStage` | 1.730 μs | 4.17 KB | 1.53x |
| One stage | 1.100 μs | 2.20 KB | 1.00x |
| + a second no-op stage | 1.621 μs | 3.50 KB | 1.47x |
| No default stage wired in | 1.140 μs | 2.17 KB | 1.00x |
| + a no-op default stage | 1.616 μs | 3.48 KB | 1.42x |

**Configuration** — one-time cost of registering handlers at startup:

| Method | Mean | Allocated |
|---|---:|---:|
| `RegisterHandlersAsImplementedFrom` (assembly scan) | 4.062 μs | 12.64 KB |
| `RegisterHandler` (explicit) | 5.219 μs | 15.24 KB |

**Validation**:

| Method | Mean | Allocated |
|---|---:|---:|
| `ValidateAsync` (run rules) | 68.60 ns | 344 B |
| Construct a validator | 16,615.35 ns | 5.48 KB |

## Roadmap

Known gaps versus MediatR, with status and priority, are tracked in [ROADMAP.md](ROADMAP.md).

## Contributing

See [CLAUDE.md](CLAUDE.md) (pointed to by [AGENTS.md](AGENTS.md)) for development practices: test-driven development, verification commands, and commit/documentation conventions.

## License

[MIT](LICENSE)
