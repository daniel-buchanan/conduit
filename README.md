# Conduit

A MediatR-style request/response mediator for .NET. Consumers define `IRequest<TResponse>` messages and `IRequestHandler` implementations; `IConduit` routes each request to its handler through a configurable pipeline, without callers taking a direct dependency on the handler.

Targets `net10.0`.

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

## Roadmap

Known gaps versus MediatR, with status and priority, are tracked in [ROADMAP.md](ROADMAP.md).

## Contributing

See [CLAUDE.md](CLAUDE.md) (pointed to by [AGENTS.md](AGENTS.md)) for development practices: test-driven development, verification commands, and commit/documentation conventions.

## License

[MIT](LICENSE)
