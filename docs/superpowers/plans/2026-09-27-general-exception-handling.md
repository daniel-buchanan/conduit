# General Exception Handling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a general, per-`(TRequest, TException)` exception interception point to `conduit` core, closing ROADMAP.md item #1 — two independent mechanisms (Handler-stage-scoped, and any-other-stage-scoped), each with an observe-only Action flavor and a swallow-and-respond Handler flavor, dispatched from `Pipe<TRequest,TResponse>.ExecuteStage`'s existing catch block.

**Architecture:** Four new public interfaces (`IRequestExceptionHandler`/`IRequestExceptionAction`/`IStageExceptionHandler`/`IStageExceptionAction`) plus `RequestExceptionHandlerState<TResponse>` live in `src/conduit/Exceptions/Handling/`. A new internal `ExceptionHandlerDispatcher` (`src/conduit/Pipes/`) resolves matching registrations via `IServiceProvider`, walking the thrown exception's real type hierarchy, and is invoked from `Pipe<TRequest,TResponse>.ExecuteStage`'s catch block before today's wrap-and-throw. A handler calling `SetHandled` short-circuits the whole pipe; `BuildablePipe.PushInternalAsync`'s loop breaks early, truncates its metrics array, and `DebugResult<TResponse>` gains a `ShortCircuited` flag.

**Tech Stack:** C# / .NET (`conduit` targets `netstandard2.0;net10.0`), xunit + FluentAssertions + Moq, `Microsoft.Extensions.DependencyInjection`.

**Spec:** [docs/superpowers/specs/2026-09-27-general-exception-handling-design.md](../specs/2026-09-27-general-exception-handling-design.md) and [ADR-0016](../../adr/0016-request-and-stage-exception-handlers.md) — both fully grilled, no open design questions.

## Global Constraints

- `dotnet build` then `dotnet test` must both pass clean before any task is considered done — `TreatWarningsAsErrors` is on repo-wide (`Directory.Build.props`).
- `src/conduit/conduit.csproj` targets `netstandard2.0;net10.0` — every new type/method touched by this plan must compile on both. `conduit.tests` is `net10.0`-only, so a netstandard2.0-only compile break is **never caught by `dotnet test`** — Task 1 and Task 9 each include an explicit `dotnet build -f netstandard2.0 src/conduit/conduit.csproj` step for this reason (see ADR-0015's own precedent for this exact gap).
- No C# 8 default interface members (`int Order => 0;` on an interface) anywhere in this feature — .NET Framework cannot dispatch them at runtime, defeating the point of netstandard2.0 targeting (see ADR-0016). `Order` is a required `{ get; }` property on every interface that declares it.
- No `arr[a..b]` range/index slicing on arrays in `conduit` core (the `System.Index`/`System.Range` BCL surface is not guaranteed across netstandard2.0). Use `Array.Resize` instead.
- Conventional Commits, scope `exceptions` (folder `src/conduit/Exceptions`, per CLAUDE.md's scope-to-folder mapping) for every commit in this plan except Task 9's `docs:` commit (no scope, cross-cutting).
- Every test file lives under `test/conduit.tests/ExceptionHandling/`.
- Phase 2 (`RegisterExceptionHandlersAsImplementedFrom<TLocator>()`) is explicitly **out of scope** for this plan — do not add it.

## Review Focus

- **A handled exception in a *post*-execution stage must skip every remaining stage, not just resume normally** — the spec's short-circuit rule is uniform regardless of stage position; Task 6 pins this with a stage registered *after* the one that throws.
- **`Order` must beat exception-type specificity, not the reverse** — a less-specific handler (registered against a base exception type) with a lower `Order` must win over a more-specific one with a higher `Order`; Task 4 pins this explicitly, not just "hierarchy walk finds a match."
- **`CancellationToken` passed into `PushAsync`/`PushWithDebugAsync` must actually reach a registered handler/action**, not a fresh or default token; Task 1 pins this since nothing else in the dispatch path would otherwise catch a copy-paste `CancellationToken.None` mistake.
- **`DebugResult.Metrics` must never contain a `null` element after a short-circuit** (the array is pre-allocated at `stages.Length` before the loop knows it will break early); Task 1 and Task 6 both assert on `Metrics.Length`/`Metrics` contents, not just `ShortCircuited`.
- **The new dispatch path must not silently break netstandard2.0 compilation** — `dotnet test` alone cannot catch this since `conduit.tests` is `net10.0`-only; Task 1 and Task 9 both run `dotnet build -f netstandard2.0` explicitly.

## File Structure

New files:
- `src/conduit/Exceptions/Handling/IOrderedExceptionParticipant.cs` — shared `int Order { get; }` contract all four interfaces below extend, so `ExceptionHandlerDispatcher` can read `Order` via one cast instead of reflecting the property per closed generic type. Additive implementation detail, not a new product-facing decision — the spec's own `Order` requirement is unchanged, this is just how the four interfaces share it.
- `src/conduit/Exceptions/Handling/RequestExceptionHandlerState.cs`
- `src/conduit/Exceptions/Handling/IRequestExceptionHandler.cs`
- `src/conduit/Exceptions/Handling/IRequestExceptionAction.cs`
- `src/conduit/Exceptions/Handling/IStageExceptionHandler.cs`
- `src/conduit/Exceptions/Handling/IStageExceptionAction.cs`
- `src/conduit/Pipes/ExceptionHandlerDispatcher.cs` — internal, resolves + sorts + invokes.
- `test/conduit.tests/ExceptionHandling/RequestScopedHandlerTests.cs`
- `test/conduit.tests/ExceptionHandling/RequestScopedActionTests.cs`
- `test/conduit.tests/ExceptionHandling/ExceptionHandlerOrderingTests.cs`
- `test/conduit.tests/ExceptionHandling/ExceptionHierarchyMatchingTests.cs`
- `test/conduit.tests/ExceptionHandling/StageScopedPreExecutionTests.cs`
- `test/conduit.tests/ExceptionHandling/StageScopedPostExecutionTests.cs`
- `test/conduit.tests/ExceptionHandling/PassthroughExceptionExemptionTests.cs`
- `test/conduit.tests/ExceptionHandling/ExceptionHandlerRethrowTests.cs`

Modified files:
- `src/conduit/Pipes/Pipe.cs` — `ExecuteStage`'s return tuple and catch block.
- `src/conduit/Pipes/BuildablePipe.cs` — `PushInternalAsync`'s loop, metrics truncation, `PushAsync`/`PushWithDebugAsync`.
- `src/conduit/Pipes/DebugResult.cs` — add `ShortCircuited`.
- `CONTEXT.md` — glossary entries (Task 9).
- `ROADMAP.md` — remove item #1, renumber (Task 9).

---

### Task 1: Request-scoped Handler flavor, dispatcher foundation, short-circuit plumbing

**Files:**
- Create: `src/conduit/Exceptions/Handling/IOrderedExceptionParticipant.cs`
- Create: `src/conduit/Exceptions/Handling/RequestExceptionHandlerState.cs`
- Create: `src/conduit/Exceptions/Handling/IRequestExceptionHandler.cs`
- Create: `src/conduit/Exceptions/Handling/IRequestExceptionAction.cs`
- Create: `src/conduit/Exceptions/Handling/IStageExceptionHandler.cs`
- Create: `src/conduit/Exceptions/Handling/IStageExceptionAction.cs`
- Create: `src/conduit/Pipes/ExceptionHandlerDispatcher.cs`
- Modify: `src/conduit/Pipes/DebugResult.cs`
- Modify: `src/conduit/Pipes/Pipe.cs`
- Modify: `src/conduit/Pipes/BuildablePipe.cs`
- Test: `test/conduit.tests/ExceptionHandling/RequestScopedHandlerTests.cs`

**Interfaces:**
- Produces: `IOrderedExceptionParticipant.Order` (`int`, required); `RequestExceptionHandlerState<TResponse>` with `bool Handled`, `TResponse? Response`, `void SetHandled(TResponse response)`; `IRequestExceptionHandler<TRequest,TResponse,TException>.HandleAsync(TRequest, TException, RequestExceptionHandlerState<TResponse>, CancellationToken) : Task`; `IRequestExceptionAction<TRequest,TException>.ExecuteAsync(TRequest, TException, CancellationToken) : Task`; `IStageExceptionHandler<TRequest,TResponse,TException>` / `IStageExceptionAction<TRequest,TException>` with the same shapes; `internal static Task<(bool Handled, TResponse? Response)> ExceptionHandlerDispatcher.DispatchAsync<TRequest,TResponse>(IServiceProvider, TRequest, Exception, bool isHandlerStage, CancellationToken)`; `DebugResult<TResponse>.ShortCircuited` (`bool`).
- Consumes: nothing from earlier tasks (this is the foundation task).

- [ ] **Step 1: Create the contract files (scaffolding, no behavior yet)**

`src/conduit/Exceptions/Handling/IOrderedExceptionParticipant.cs`:
```csharp
namespace conduit.Exceptions.Handling;

/// <summary>
/// Shared ordering contract for the four exception-handling interfaces in this namespace, so
/// <see cref="conduit.Pipes.ExceptionHandlerDispatcher"/> can read a resolved instance's order via a
/// single cast instead of reflecting the property per closed generic type.
/// </summary>
public interface IOrderedExceptionParticipant
{
    /// <summary>
    /// Controls sequencing when multiple handlers/actions match the same exception. Lower values run
    /// first. Ties keep hierarchy-then-registration order (see <see cref="conduit.Pipes.ExceptionHandlerDispatcher"/>).
    /// </summary>
    int Order { get; }
}
```

`src/conduit/Exceptions/Handling/RequestExceptionHandlerState.cs`:
```csharp
namespace conduit.Exceptions.Handling;

/// <summary>
/// Carries the outcome of an <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/> or
/// <see cref="IStageExceptionHandler{TRequest,TResponse,TException}"/> invocation. Calling
/// <see cref="SetHandled"/> short-circuits the whole pipe: no further stage runs, and the pipe returns
/// the supplied response immediately.
/// </summary>
/// <typeparam name="TResponse">The pipe's response type.</typeparam>
public sealed class RequestExceptionHandlerState<TResponse> where TResponse : class
{
    /// <summary>
    /// Gets whether a handler has supplied a response via <see cref="SetHandled"/>.
    /// </summary>
    public bool Handled { get; private set; }

    /// <summary>
    /// Gets the response supplied via <see cref="SetHandled"/>, if any.
    /// </summary>
    public TResponse? Response { get; private set; }

    /// <summary>
    /// Supplies a response for the exception that was caught and marks it as handled. The pipe stops
    /// running further stages and returns this response immediately.
    /// </summary>
    /// <param name="response">The response to return instead of letting the exception propagate.</param>
    public void SetHandled(TResponse response)
    {
        Response = response;
        Handled = true;
    }
}
```

`src/conduit/Exceptions/Handling/IRequestExceptionHandler.cs`:
```csharp
namespace conduit.Exceptions.Handling;

/// <summary>
/// Handles an exception thrown specifically by a <c>Pipe</c>'s <c>Handler</c> stage for a given
/// <typeparamref name="TRequest"/>/<typeparamref name="TException"/> pair, and may supply a response to
/// short-circuit the pipe instead of letting the exception propagate. Named after MediatR's
/// <c>IRequestExceptionHandler</c> for direct cross-reference (see docs/research/mediatr-feature-gap.md).
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The pipe's response type.</typeparam>
/// <typeparam name="TException">The exception type this handler matches (and its subtypes).</typeparam>
public interface IRequestExceptionHandler<TRequest, TResponse, TException> : IOrderedExceptionParticipant
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
    where TException : Exception
{
    /// <summary>
    /// Handles the exception. Call <c>state.SetHandled(response)</c> to short-circuit the pipe with a
    /// response; otherwise the exception falls through to be wrapped as <c>StageFailedException</c> once
    /// every registered handler for this type has run without handling it.
    /// </summary>
    Task HandleAsync(TRequest request, TException exception, RequestExceptionHandlerState<TResponse> state, CancellationToken cancellationToken);
}
```

`src/conduit/Exceptions/Handling/IRequestExceptionAction.cs`:
```csharp
namespace conduit.Exceptions.Handling;

/// <summary>
/// Observes an exception thrown specifically by a <c>Pipe</c>'s <c>Handler</c> stage. Always runs in
/// full, for every match, before any <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/>
/// is attempted, and cannot suppress the eventual throw itself. Named after MediatR's
/// <c>IRequestExceptionAction</c> for direct cross-reference.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TException">The exception type this action matches (and its subtypes).</typeparam>
public interface IRequestExceptionAction<TRequest, TException> : IOrderedExceptionParticipant
    where TRequest : class
    where TException : Exception
{
    /// <summary>
    /// Observes/reacts to the exception. Cannot suppress it — the pipe still falls through to whatever
    /// the Handler-flavor dispatch decides afterward.
    /// </summary>
    Task ExecuteAsync(TRequest request, TException exception, CancellationToken cancellationToken);
}
```

`src/conduit/Exceptions/Handling/IStageExceptionHandler.cs`:
```csharp
namespace conduit.Exceptions.Handling;

/// <summary>
/// Handles an exception thrown by any <c>Stage</c> in a <c>Pipe</c> OTHER than the <c>Handler</c>
/// (pre-execution, post-execution, including framework stages like <c>ValidationStage&lt;,&gt;</c>), and
/// may supply a response to short-circuit the pipe. Deliberately a separate interface from
/// <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/> so a consumer chooses "Handler
/// failure" vs. "any other stage failure" at the registration/type level, not inside a handler body.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The pipe's response type.</typeparam>
/// <typeparam name="TException">The exception type this handler matches (and its subtypes).</typeparam>
public interface IStageExceptionHandler<TRequest, TResponse, TException> : IOrderedExceptionParticipant
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
    where TException : Exception
{
    /// <summary>
    /// Handles the exception. Call <c>state.SetHandled(response)</c> to short-circuit the pipe with a
    /// response — this stops every remaining stage, including the <c>Handler</c> if the throwing stage
    /// ran before it.
    /// </summary>
    Task HandleAsync(TRequest request, TException exception, RequestExceptionHandlerState<TResponse> state, CancellationToken cancellationToken);
}
```

`src/conduit/Exceptions/Handling/IStageExceptionAction.cs`:
```csharp
namespace conduit.Exceptions.Handling;

/// <summary>
/// Observes an exception thrown by any <c>Stage</c> in a <c>Pipe</c> OTHER than the <c>Handler</c>.
/// Always runs in full, for every match, before any
/// <see cref="IStageExceptionHandler{TRequest,TResponse,TException}"/> is attempted, and cannot suppress
/// the eventual throw itself.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TException">The exception type this action matches (and its subtypes).</typeparam>
public interface IStageExceptionAction<TRequest, TException> : IOrderedExceptionParticipant
    where TRequest : class
    where TException : Exception
{
    /// <summary>
    /// Observes/reacts to the exception. Cannot suppress it.
    /// </summary>
    Task ExecuteAsync(TRequest request, TException exception, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Add `ShortCircuited` to `DebugResult<TResponse>` (scaffolding, no behavior yet)**

This must land before the test below, since the test references `result.ShortCircuited` — otherwise
"run the test and confirm it fails" would fail on an unrelated compile error instead of the real
behavioral reason, which CLAUDE.md's TDD rule specifically rules out. Adding a property with a default
value changes no existing behavior (nothing sets it `true` yet), so this is scaffolding, same as Step 1.

Modify `src/conduit/Pipes/DebugResult.cs` to:
```csharp
namespace conduit.Pipes;

public record DebugResult<TResponse>(TResponse? Response, long OverallDurationMs, StageMetric[] Metrics, bool ShortCircuited = false)
{
    /// <summary>
    /// The overall duration of the pipeline in milliseconds.
    /// </summary>
    public long OverallDurationMs { get; } = OverallDurationMs;

    /// <summary>
    /// The result of the pipeline.
    /// </summary>
    public TResponse? Response { get; } = Response;

    /// <summary>
    /// Metrics about each stage in the process.
    /// </summary>
    public StageMetric[] Metrics { get; } = Metrics;

    /// <summary>
    /// Gets whether the pipe stopped early because a registered exception handler called
    /// <c>RequestExceptionHandlerState&lt;TResponse&gt;.SetHandled</c>. When <c>true</c>, <see cref="Metrics"/>
    /// only covers the stages that actually ran.
    /// </summary>
    public bool ShortCircuited { get; } = ShortCircuited;
}
```

- [ ] **Step 3: Run `dotnet build` to confirm the scaffolding compiles**

Run: `dotnet build src/conduit/conduit.csproj`
Expected: succeeds (the four interfaces, the state class, and `DebugResult`'s new property have no
callers/producers yet, so nothing else can fail).

- [ ] **Step 4: Write the failing test**

`test/conduit.tests/ExceptionHandling/RequestScopedHandlerTests.cs`:
```csharp
using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class RequestScopedHandlerTests
{
    public class HandlerExceptionRequest : IRequest<HandlerExceptionResponse>;

    public class HandlerExceptionResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ThrowingHandler(ILog logger) : RequestHandler<HandlerExceptionRequest, HandlerExceptionResponse>(logger)
    {
        public override Task<HandlerExceptionResponse> HandleAsync(HandlerExceptionRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("handler boom");
    }

    public class RecoveringHandler : IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            HandlerExceptionRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<HandlerExceptionResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new HandlerExceptionResponse { Value = "recovered" });
            return Task.CompletedTask;
        }
    }

    public class NonRecoveringHandler : IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            HandlerExceptionRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<HandlerExceptionResponse> state,
            CancellationToken cancellationToken)
            => Task.CompletedTask; // never calls SetHandled
    }

    public class LoggingPostStage(ILog logger) : PipeStage<HandlerExceptionRequest, HandlerExceptionResponse>(logger)
    {
        public static int InvocationCount;

        protected override Task<StageResult<HandlerExceptionRequest, HandlerExceptionResponse>> ExecuteInternalAsync(
            Guid instanceId, HandlerExceptionRequest request, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(StageResult.WithIndeterminateResult<HandlerExceptionRequest, HandlerExceptionResponse>(GetType()));
        }
    }

    public class NonThrowingHandler(ILog logger) : RequestHandler<HandlerExceptionRequest, HandlerExceptionResponse>(logger)
    {
        public override Task<HandlerExceptionResponse> HandleAsync(HandlerExceptionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HandlerExceptionResponse { Value = "normal" });
    }

    private sealed class DelegateHandler(
        Func<HandlerExceptionRequest, InvalidOperationException, RequestExceptionHandlerState<HandlerExceptionResponse>, CancellationToken, Task> handle)
        : IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            HandlerExceptionRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<HandlerExceptionResponse> state,
            CancellationToken cancellationToken)
            => handle(request, exception, state, cancellationToken);
    }

    private static IPipe<HandlerExceptionRequest, HandlerExceptionResponse> BuildPipe(Action<IServiceCollection> configureHandlers)
    {
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<HandlerExceptionRequest, HandlerExceptionResponse>(p =>
        {
            p.AddHandler<ThrowingHandler>();
            p.AddStage<LoggingPostStage>();
        }), new Mock<ILog>().Object);
        configureHandlers(services);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IPipe<HandlerExceptionRequest, HandlerExceptionResponse>>();
    }

    [Fact]
    public async Task A_Handler_That_Calls_SetHandled_Should_ShortCircuit_And_Return_Its_Response()
    {
        // Arrange
        LoggingPostStage.InvocationCount = 0;
        var pipe = BuildPipe(s => s.AddTransient<
            IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>,
            RecoveringHandler>());

        // Act
        var result = await pipe.PushWithDebugAsync(new HandlerExceptionRequest());

        // Assert: handled, short-circuited, post-stage never ran, metrics truncated to just the Handler.
        Assert.Equal("recovered", result.Response!.Value);
        Assert.True(result.ShortCircuited);
        Assert.Single(result.Metrics);
        Assert.Equal(0, LoggingPostStage.InvocationCount);
    }

    [Fact]
    public async Task A_Handler_That_Never_Calls_SetHandled_Should_Fall_Through_To_StageFailedException()
    {
        // Arrange
        var pipe = BuildPipe(s => s.AddTransient<
            IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>,
            NonRecoveringHandler>());

        // Act
        Func<Task> act = () => pipe.PushAsync(new HandlerExceptionRequest());

        // Assert
        var exception = await Assert.ThrowsAsync<conduit.Exceptions.StageFailedException>(act);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task The_CancellationToken_Passed_To_PushAsync_Should_Reach_The_Registered_Handler()
    {
        // Arrange
        CancellationToken? observedToken = null;
        var pipe = BuildPipe(s => s.AddTransient<
            IRequestExceptionHandler<HandlerExceptionRequest, HandlerExceptionResponse, InvalidOperationException>>(_ =>
            new DelegateHandler((req, ex, state, token) =>
            {
                observedToken = token;
                state.SetHandled(new HandlerExceptionResponse { Value = "recovered" });
                return Task.CompletedTask;
            })));
        using var cts = new CancellationTokenSource();

        // Act
        await pipe.PushAsync(new HandlerExceptionRequest(), cts.Token);

        // Assert
        Assert.Equal(cts.Token, observedToken);
    }

    [Fact]
    public async Task A_Normal_Pipe_Run_With_No_Exception_Should_Report_ShortCircuited_False()
    {
        // Arrange: regression guard — introducing ShortCircuited must not make it true by default.
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<HandlerExceptionRequest, HandlerExceptionResponse>(p =>
            p.AddHandler<NonThrowingHandler>()), new Mock<ILog>().Object);
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<HandlerExceptionRequest, HandlerExceptionResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new HandlerExceptionRequest());

        // Assert
        Assert.Equal("normal", result.Response!.Value);
        Assert.False(result.ShortCircuited);
    }
}
```

- [ ] **Step 5: Run the test to verify it fails for the right reason**

Run: `dotnet test --filter FullyQualifiedName~RequestScopedHandlerTests`
Expected: FAIL. `A_Handler_That_Calls_SetHandled_...` and `The_CancellationToken_...` throw an unhandled
`conduit.Exceptions.StageFailedException` wrapping `InvalidOperationException` during Act (today's
existing behavior — nothing yet dispatches to a registered handler) instead of returning normally — a
real behavioral failure, not a compile error. `A_Handler_That_Never_Calls_SetHandled_...` and
`A_Normal_Pipe_Run_With_No_Exception_Should_Report_ShortCircuited_False` should already incidentally
pass (today's behavior already wraps-and-throws in the first case, and `ShortCircuited` defaults to
`false` and nothing sets it otherwise yet in the second) — confirm both do; that's fine, it's
regression coverage, not new behavior.

- [ ] **Step 6: Implement `ExceptionHandlerDispatcher`**

`src/conduit/Pipes/ExceptionHandlerDispatcher.cs`:
```csharp
using conduit.Exceptions.Handling;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Pipes;

/// <summary>
/// Resolves, orders, and invokes <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/> /
/// <see cref="IRequestExceptionAction{TRequest,TException}"/> (when <c>isHandlerStage</c> is <c>true</c>) or
/// <see cref="IStageExceptionHandler{TRequest,TResponse,TException}"/> /
/// <see cref="IStageExceptionAction{TRequest,TException}"/> (otherwise) for a caught exception, walking the
/// exception's real type hierarchy up to and including <see cref="Exception"/>.
/// </summary>
internal static class ExceptionHandlerDispatcher
{
    internal static async Task<(bool Handled, TResponse? Response)> DispatchAsync<TRequest, TResponse>(
        IServiceProvider provider,
        TRequest request,
        Exception exception,
        bool isHandlerStage,
        CancellationToken cancellationToken)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
    {
        var actionInterface = isHandlerStage ? typeof(IRequestExceptionAction<,>) : typeof(IStageExceptionAction<,>);
        var handlerInterface = isHandlerStage ? typeof(IRequestExceptionHandler<,,>) : typeof(IStageExceptionHandler<,,>);

        var actions = new List<(int Order, object Instance, System.Reflection.MethodInfo Method)>();
        var handlers = new List<(int Order, object Instance, System.Reflection.MethodInfo Method)>();

        foreach (var ancestor in ExceptionHierarchy(exception.GetType()))
        {
            var closedActionType = actionInterface.MakeGenericType(typeof(TRequest), ancestor);
            var actionMethod = closedActionType.GetMethod("ExecuteAsync")!;
            foreach (var instance in provider.GetServices(closedActionType))
            {
                if (instance is null) continue;
                actions.Add((((IOrderedExceptionParticipant)instance).Order, instance, actionMethod));
            }

            var closedHandlerType = handlerInterface.MakeGenericType(typeof(TRequest), typeof(TResponse), ancestor);
            var handlerMethod = closedHandlerType.GetMethod("HandleAsync")!;
            foreach (var instance in provider.GetServices(closedHandlerType))
            {
                if (instance is null) continue;
                handlers.Add((((IOrderedExceptionParticipant)instance).Order, instance, handlerMethod));
            }
        }

        foreach (var (_, instance, method) in actions.OrderBy(a => a.Order))
        {
            await (Task)method.Invoke(instance, [request, exception, cancellationToken])!;
        }

        var state = new RequestExceptionHandlerState<TResponse>();
        foreach (var (_, instance, method) in handlers.OrderBy(h => h.Order))
        {
            await (Task)method.Invoke(instance, [request, exception, state, cancellationToken])!;
            if (state.Handled) break;
        }

        return (state.Handled, state.Response);
    }

    private static IEnumerable<Type> ExceptionHierarchy(Type exceptionType)
    {
        var current = exceptionType;
        while (current is not null)
        {
            yield return current;
            if (current == typeof(Exception)) yield break;
            current = current.BaseType;
        }
    }
}
```

Add `using System.Linq;` at the top alongside the existing usings (needed for `.OrderBy`).

- [ ] **Step 7: Wire dispatch into `Pipe<TRequest,TResponse>.ExecuteStage`**

Modify `src/conduit/Pipes/Pipe.cs`. Change the method signature and catch block:
```csharp
protected async Task<(TResponse? Response, StageMetric? Metric, bool ShortCircuited)> ExecuteStage(
    int index,
    Guid instanceId,
    Type stageType,
    Stopwatch? stageTimer,
    TRequest request,
    CancellationToken cancellationToken,
    bool withMetrics)
{
    stageTimer?.Restart();
    StageMetric? metric = null;
    TResponse? response = null;

    var stage = (IPipeStage<TRequest, TResponse>?)provider.GetService(stageType);
    var stageName = stageType.GetGenericName();
    if (stage == null)
        throw new StageNotFoundException($"Could not resolve stage of type {stageName}");

    logger.Debug($"[{instanceId}] {stageType.GetGenericName()} :: Executing stage {stageName}");

    stageTimer?.Stop();
    var prefetchDuration = stageTimer?.ElapsedMilliseconds;
    stageTimer?.Restart();

    try
    {
        var stageResponse = await stage.ExecuteAsync(instanceId, request, cancellationToken);

        if (!stageResponse.IsSuccessful) HandleUnsuccessfulResult(request, stageResponse);
        response ??= stageResponse.Result;

        stageTimer?.Stop();
        if (withMetrics)
            metric = new StageMetric(index, stageName, prefetchDuration ?? -1, stageTimer?.ElapsedMilliseconds ?? -1);
    }
    catch (Exception e)
    {
        stageTimer?.Stop();
        logger.Error($"[{instanceId}] {stageType.GetGenericName()} :: Error while executing stage {stageName}", e);

        if (e is IPassthroughException) throw;

        var (handled, handledResponse) = await ExceptionHandlerDispatcher.DispatchAsync<TRequest, TResponse>(
            provider, request, e, stage is IRequestHandler, cancellationToken);

        if (!handled)
        {
            HandleUnsuccessfulResult(request, StageResult.WithException<TRequest, TResponse>(e, stageType));
        }

        response = handledResponse;
        if (withMetrics)
            metric = new StageMetric(index, stageName, prefetchDuration ?? -1, stageTimer?.ElapsedMilliseconds ?? -1, Success: true, Exception: e);

        return (response, metric, true);
    }

    return (response, metric, false);
}
```
Add `using conduit.Exceptions.Handling;` is not needed here (the dispatcher call only needs `conduit.Pipes`, already the containing namespace) — no new `using` required in `Pipe.cs`.

- [ ] **Step 8: Update `BuildablePipe.PushInternalAsync`'s loop and both `Push*` methods**

Modify `src/conduit/Pipes/BuildablePipe.cs` to:
```csharp
public class BuildablePipe<TRequest, TResponse>(ILog logger, IServiceProvider serviceProvider, Type[] stages)
    : Pipe<TRequest, TResponse>(logger, serviceProvider)
    where TResponse : class
    where TRequest : class, IRequest<TResponse>
{
    /// <inheritdoc/>
    public override async Task<TResponse?> PushAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var result = await PushInternalAsync(request, withMetrics: false, cancellationToken);
        return result.Response;
    }

    /// <inheritdoc/>
    public override async Task<DebugResult<TResponse?>> PushWithDebugAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var result = await PushInternalAsync(request, withMetrics: true, cancellationToken);
        return new DebugResult<TResponse?>(result.Response, result.OverallDurationMs!.Value, result.Metrics!, result.ShortCircuited);
    }

    private async Task<(TResponse? Response, long? OverallDurationMs, StageMetric[]? Metrics, bool ShortCircuited)> PushInternalAsync(
        TRequest request,
        bool withMetrics,
        CancellationToken cancellationToken = default)
    {
        TResponse? response = null;
        StageMetric[]? metrics = null;
        Stopwatch? overallTimer = null;
        Stopwatch? stageTimer = null;
        var shortCircuited = false;

        if (withMetrics)
        {
            metrics = new StageMetric[stages.Length];
            overallTimer = Stopwatch.StartNew();
            stageTimer = new Stopwatch();
        }

        var instanceId = Guid.NewGuid();
        var stagesRun = stages.Length;
        for (var i = 0; i < stages.Length; i++)
        {
            var result = await ExecuteStage(i, instanceId, stages[i], stageTimer, request, cancellationToken, withMetrics);
            metrics?[i] = result.Metric!;

            if (response is not null && result.Response is not null && !Equals(response, result.Response))
                Logger.Verbose($"[{instanceId}] {stages[i].GetGenericName()} :: Stage response overrode previous stage's response.");

            response = result.Response ?? response;

            if (result.ShortCircuited)
            {
                shortCircuited = true;
                stagesRun = i + 1;
                break;
            }
        }

        overallTimer?.Stop();
        if (metrics is not null && stagesRun < metrics.Length)
            Array.Resize(ref metrics, stagesRun);

        return (response, overallTimer?.ElapsedMilliseconds, metrics, shortCircuited);
    }
}
```
Note the `Array.Resize` call rather than `metrics[..stagesRun]` — see Global Constraints (netstandard2.0 array-slicing risk).

- [ ] **Step 9: Run the test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~RequestScopedHandlerTests`
Expected: PASS (all four tests).

- [ ] **Step 10: Run the full suite**

Run: `dotnet test`
Expected: PASS — confirms no regression in `PipeExceptionWrappingTests`, `ResponseOverrideTests`, `MultipleCustomStagesTests`, etc.

- [ ] **Step 11: Confirm netstandard2.0 still builds clean**

Run: `dotnet build -f netstandard2.0 src/conduit/conduit.csproj`
Expected: succeeds with zero warnings (`TreatWarningsAsErrors`) — this is the earliest point the plan touches reflection/array-resize code that a `net10.0`-only `conduit.tests` run can never itself exercise.

- [ ] **Step 12: Commit**

```bash
git add src/conduit/Exceptions/Handling/ src/conduit/Pipes/ExceptionHandlerDispatcher.cs \
        src/conduit/Pipes/DebugResult.cs src/conduit/Pipes/Pipe.cs src/conduit/Pipes/BuildablePipe.cs \
        test/conduit.tests/ExceptionHandling/RequestScopedHandlerTests.cs
git commit -m "feat(exceptions): add per-request exception handlers with short-circuit

Closes the highest-priority roadmap gap: a Handler-stage exception can now be
intercepted per-(TRequest,TException) instead of always being wrapped as
StageFailedException. A registered IRequestExceptionHandler may call
SetHandled to supply a response, which short-circuits the rest of the pipe."
```

---

### Task 2: Request-scoped Action flavor

**Files:**
- Modify: `src/conduit/Pipes/ExceptionHandlerDispatcher.cs` (no change — actions were already dispatched in Task 1's implementation; this task only adds test coverage that was missing)
- Test: `test/conduit.tests/ExceptionHandling/RequestScopedActionTests.cs`

**Interfaces:**
- Consumes: `IRequestExceptionAction<TRequest,TException>` (Task 1).
- Produces: nothing new.

- [ ] **Step 1: Write the failing test**

`test/conduit.tests/ExceptionHandling/RequestScopedActionTests.cs`:
```csharp
using conduit.Exceptions.Handling;
using conduit.logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class RequestScopedActionTests
{
    public class ActionRequest : IRequest<ActionResponse>;

    public class ActionResponse;

    public class ThrowingHandler(ILog logger) : RequestHandler<ActionRequest, ActionResponse>(logger)
    {
        public override Task<ActionResponse> HandleAsync(ActionRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("handler boom");
    }

    public class ObservingAction : IRequestExceptionAction<ActionRequest, InvalidOperationException>
    {
        public static int InvocationCount;
        public int Order => 0;

        public Task ExecuteAsync(ActionRequest request, InvalidOperationException exception, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task An_Action_Should_Run_But_The_Exception_Should_Still_Propagate_Wrapped()
    {
        // Arrange
        ObservingAction.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<ActionRequest, ActionResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionAction<ActionRequest, InvalidOperationException>, ObservingAction>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<ActionRequest, ActionResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new ActionRequest());

        // Assert: the action ran (observed the exception), but since no handler ever calls SetHandled, the
        // pipe still falls through to today's wrap-and-throw behavior.
        var exception = await Assert.ThrowsAsync<conduit.Exceptions.StageFailedException>(act);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal(1, ObservingAction.InvocationCount);
    }
}
```

- [ ] **Step 2: Run the test**

Run: `dotnet test --filter FullyQualifiedName~RequestScopedActionTests`
Expected: PASS immediately — Task 1's `ExceptionHandlerDispatcher` already runs actions unconditionally before attempting handlers. This step exists to prove that behavior with a dedicated, previously-missing test, not to add new production code.

- [ ] **Step 3: Run the full suite**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add test/conduit.tests/ExceptionHandling/RequestScopedActionTests.cs
git commit -m "test(exceptions): pin that a request-scoped action always runs and cannot suppress the throw"
```

---

### Task 3: Ordering — multiple handlers/actions, `Order` respected

**Files:**
- Test: `test/conduit.tests/ExceptionHandling/ExceptionHandlerOrderingTests.cs`

**Interfaces:**
- Consumes: `IRequestExceptionHandler<,,>`, `IRequestExceptionAction<,>` (Task 1/2).
- Produces: nothing new.

- [ ] **Step 1: Write the failing test**

`test/conduit.tests/ExceptionHandling/ExceptionHandlerOrderingTests.cs`:
```csharp
using conduit.Exceptions.Handling;
using conduit.logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class ExceptionHandlerOrderingTests
{
    public class OrderingRequest : IRequest<OrderingResponse>;

    public class OrderingResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ThrowingHandler(ILog logger) : RequestHandler<OrderingRequest, OrderingResponse>(logger)
    {
        public override Task<OrderingResponse> HandleAsync(OrderingRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }

    public class FirstHandler : IRequestExceptionHandler<OrderingRequest, OrderingResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            OrderingRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<OrderingResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new OrderingResponse { Value = "first" });
            return Task.CompletedTask;
        }
    }

    public class SecondHandler : IRequestExceptionHandler<OrderingRequest, OrderingResponse, InvalidOperationException>
    {
        public static int InvocationCount;
        public int Order => 10;

        public Task HandleAsync(
            OrderingRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<OrderingResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new OrderingResponse { Value = "second" });
            return Task.CompletedTask;
        }
    }

    public class FirstAction : IRequestExceptionAction<OrderingRequest, InvalidOperationException>
    {
        public static readonly List<string> InvocationOrder = [];
        public int Order => 10;

        public Task ExecuteAsync(OrderingRequest request, InvalidOperationException exception, CancellationToken cancellationToken)
        {
            InvocationOrder.Add(nameof(FirstAction));
            return Task.CompletedTask;
        }
    }

    public class SecondAction : IRequestExceptionAction<OrderingRequest, InvalidOperationException>
    {
        public int Order => 0;

        public Task ExecuteAsync(OrderingRequest request, InvalidOperationException exception, CancellationToken cancellationToken)
        {
            FirstAction.InvocationOrder.Add(nameof(SecondAction));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Lower_Order_Handler_Runs_First_And_Wins_Later_Handlers_Do_Not_Run()
    {
        // Arrange
        SecondHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<OrderingRequest, OrderingResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionHandler<OrderingRequest, OrderingResponse, InvalidOperationException>, FirstHandler>();
        services.AddTransient<IRequestExceptionHandler<OrderingRequest, OrderingResponse, InvalidOperationException>, SecondHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<OrderingRequest, OrderingResponse>>();

        // Act
        var response = await pipe.PushAsync(new OrderingRequest());

        // Assert
        Assert.Equal("first", response!.Value);
        Assert.Equal(0, SecondHandler.InvocationCount);
    }

    [Fact]
    public async Task All_Actions_Run_In_Order_Regardless_Of_Registration_Order()
    {
        // Arrange: SecondAction (Order 0) is registered AFTER FirstAction (Order 10) — sorted execution
        // must still run SecondAction first, by Order, not by registration order.
        FirstAction.InvocationOrder.Clear();
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<OrderingRequest, OrderingResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionAction<OrderingRequest, InvalidOperationException>, FirstAction>();
        services.AddTransient<IRequestExceptionAction<OrderingRequest, InvalidOperationException>, SecondAction>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<OrderingRequest, OrderingResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new OrderingRequest());
        await Assert.ThrowsAsync<conduit.Exceptions.StageFailedException>(act);

        // Assert
        Assert.Equal([nameof(SecondAction), nameof(FirstAction)], FirstAction.InvocationOrder);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail for the right reason**

Run: `dotnet test --filter FullyQualifiedName~ExceptionHandlerOrderingTests`
Expected: PASS already, in fact — Task 1's dispatcher already does `.OrderBy(a => a.Order)` (LINQ's `OrderBy` is a documented-stable sort) for both lists. If either test fails here, it indicates the Task 1 implementation diverged from this plan's Step 5 code — treat that as the real red signal and fix `ExceptionHandlerDispatcher` to match, not this test.

- [ ] **Step 3: Run the full suite**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add test/conduit.tests/ExceptionHandling/ExceptionHandlerOrderingTests.cs
git commit -m "test(exceptions): pin Order-based sequencing for multiple handlers and actions"
```

---

### Task 4: Exception hierarchy walk, `Order` beats specificity

**Files:**
- Test: `test/conduit.tests/ExceptionHandling/ExceptionHierarchyMatchingTests.cs`

**Interfaces:**
- Consumes: `IRequestExceptionHandler<,,>` (Task 1).
- Produces: nothing new.

- [ ] **Step 1: Write the failing test**

`test/conduit.tests/ExceptionHandling/ExceptionHierarchyMatchingTests.cs`:
```csharp
using conduit.Exceptions.Handling;
using conduit.logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class ExceptionHierarchyMatchingTests
{
    public class HierarchyRequest : IRequest<HierarchyResponse>;

    public class HierarchyResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class PaymentException(string message) : Exception(message);

    public class PaymentDeclinedException(string message) : PaymentException(message);

    public class ThrowingHandler(ILog logger) : RequestHandler<HierarchyRequest, HierarchyResponse>(logger)
    {
        public override Task<HierarchyResponse> HandleAsync(HierarchyRequest request, CancellationToken cancellationToken = default)
            => throw new PaymentDeclinedException("card declined");
    }

    public class BaseTypeHandler : IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentException>
    {
        public int Order => 0; // lower Order, even though registered against the LESS specific base type

        public Task HandleAsync(
            HierarchyRequest request,
            PaymentException exception,
            RequestExceptionHandlerState<HierarchyResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new HierarchyResponse { Value = "handled-by-base" });
            return Task.CompletedTask;
        }
    }

    public class DerivedTypeHandler : IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentDeclinedException>
    {
        public static int InvocationCount;
        public int Order => 10; // higher Order, even though registered against the MORE specific derived type

        public Task HandleAsync(
            HierarchyRequest request,
            PaymentDeclinedException exception,
            RequestExceptionHandlerState<HierarchyResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new HierarchyResponse { Value = "handled-by-derived" });
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_Handler_Registered_For_A_Base_Exception_Type_Should_Catch_A_Derived_Exception()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<HierarchyRequest, HierarchyResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentException>, BaseTypeHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<HierarchyRequest, HierarchyResponse>>();

        // Act
        var response = await pipe.PushAsync(new HierarchyRequest());

        // Assert
        Assert.Equal("handled-by-base", response!.Value);
    }

    [Fact]
    public async Task Order_Determines_Which_Handler_Wins_Not_Exception_Type_Specificity()
    {
        // Arrange: BaseTypeHandler (Order 0, registered for PaymentException) and DerivedTypeHandler
        // (Order 10, registered for the exact thrown type PaymentDeclinedException) both match. The lower
        // Order must win regardless of which one is the more specific registration.
        DerivedTypeHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<HierarchyRequest, HierarchyResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentException>, BaseTypeHandler>();
        services.AddTransient<IRequestExceptionHandler<HierarchyRequest, HierarchyResponse, PaymentDeclinedException>, DerivedTypeHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<HierarchyRequest, HierarchyResponse>>();

        // Act
        var response = await pipe.PushAsync(new HierarchyRequest());

        // Assert: base-type handler (Order 0) ran and won; derived-type handler (Order 10) never got the chance.
        Assert.Equal("handled-by-base", response!.Value);
        Assert.Equal(0, DerivedTypeHandler.InvocationCount);
    }
}
```

- [ ] **Step 2: Run the tests**

Run: `dotnet test --filter FullyQualifiedName~ExceptionHierarchyMatchingTests`
Expected: PASS — `ExceptionHandlerDispatcher`'s `ExceptionHierarchy` walk (Task 1) already collects matches at every ancestor level and sorts the combined list purely by `Order`, so this confirms rather than changes behavior. If it fails, the hierarchy walk or the sort key in `ExceptionHandlerDispatcher` needs fixing to match this plan's Step 5 code from Task 1.

- [ ] **Step 3: Run the full suite**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add test/conduit.tests/ExceptionHandling/ExceptionHierarchyMatchingTests.cs
git commit -m "test(exceptions): pin hierarchy-walk matching and that Order beats exception-type specificity"
```

---

### Task 5: Stage-scoped mechanism, pre-execution — short-circuit before the Handler runs

**Files:**
- Test: `test/conduit.tests/ExceptionHandling/StageScopedPreExecutionTests.cs`

**Interfaces:**
- Consumes: `IStageExceptionHandler<,,>`, `IStageExceptionAction<,>` (Task 1). `ExceptionHandlerDispatcher.DispatchAsync`'s `isHandlerStage: false` branch (already implemented in Task 1 via `stage is IRequestHandler`).
- Produces: nothing new.

- [ ] **Step 1: Write the failing test**

`test/conduit.tests/ExceptionHandling/StageScopedPreExecutionTests.cs`:
```csharp
using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class StageScopedPreExecutionTests
{
    public class PreStageRequest : IRequest<PreStageResponse>;

    public class PreStageResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ThrowingPreStage(ILog logger) : PipeStage<PreStageRequest, PreStageResponse>(logger)
    {
        protected override Task<StageResult<PreStageRequest, PreStageResponse>> ExecuteInternalAsync(
            Guid instanceId, PreStageRequest request, CancellationToken cancellationToken)
            => throw new UnauthorizedAccessException("not authorized");
    }

    public class BusinessHandler(ILog logger) : RequestHandler<PreStageRequest, PreStageResponse>(logger)
    {
        public static int InvocationCount;

        public override Task<PreStageResponse> HandleAsync(PreStageRequest request, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(new PreStageResponse { Value = "business-logic-ran" });
        }
    }

    public class DenyingStageHandler : IStageExceptionHandler<PreStageRequest, PreStageResponse, UnauthorizedAccessException>
    {
        public int Order => 0;

        public Task HandleAsync(
            PreStageRequest request,
            UnauthorizedAccessException exception,
            RequestExceptionHandlerState<PreStageResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new PreStageResponse { Value = "not-authorized" });
            return Task.CompletedTask;
        }
    }

    public class AuditAction : IStageExceptionAction<PreStageRequest, UnauthorizedAccessException>
    {
        public static int InvocationCount;
        public int Order => 0;

        public Task ExecuteAsync(PreStageRequest request, UnauthorizedAccessException exception, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_Handled_PreExecution_Stage_Exception_Should_ShortCircuit_Before_The_Handler_Runs()
    {
        // Arrange: the scenario from the design discussion — a failed auth check must not let the real
        // business-logic Handler run afterward just because the flat loop would otherwise continue to it.
        BusinessHandler.InvocationCount = 0;
        AuditAction.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<PreStageRequest, PreStageResponse>(p =>
        {
            p.AddStage<ThrowingPreStage>();
            p.AddHandler<BusinessHandler>();
        }), new Mock<ILog>().Object);
        services.AddTransient<IStageExceptionHandler<PreStageRequest, PreStageResponse, UnauthorizedAccessException>, DenyingStageHandler>();
        services.AddTransient<IStageExceptionAction<PreStageRequest, UnauthorizedAccessException>, AuditAction>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<PreStageRequest, PreStageResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new PreStageRequest());

        // Assert
        Assert.Equal("not-authorized", result.Response!.Value);
        Assert.True(result.ShortCircuited);
        Assert.Equal(0, BusinessHandler.InvocationCount);
        Assert.Equal(1, AuditAction.InvocationCount);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails for the right reason**

Run: `dotnet test --filter FullyQualifiedName~StageScopedPreExecutionTests`
Expected: FAIL before Task 1's `stage is IRequestHandler` discrimination lands correctly for this case — actually, Task 1 already implemented this branch, so this should PASS. If it fails, it means `ExecuteStage`'s `stage is IRequestHandler` check (Task 1, Step 7) is not correctly selecting the Stage-scoped interfaces for a non-Handler stage — fix `Pipe.cs` to match Task 1's code, don't add new logic here.

- [ ] **Step 3: Run the full suite**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add test/conduit.tests/ExceptionHandling/StageScopedPreExecutionTests.cs
git commit -m "test(exceptions): pin that a handled pre-execution stage exception short-circuits before the Handler runs"
```

---

### Task 6: Stage-scoped mechanism, post-execution — skip every remaining stage

**Files:**
- Test: `test/conduit.tests/ExceptionHandling/StageScopedPostExecutionTests.cs`

**Interfaces:**
- Consumes: `IStageExceptionHandler<,,>` (Task 1).
- Produces: nothing new.

- [ ] **Step 1: Write the failing test**

`test/conduit.tests/ExceptionHandling/StageScopedPostExecutionTests.cs`:
```csharp
using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class StageScopedPostExecutionTests
{
    public class PostStageRequest : IRequest<PostStageResponse>;

    public class PostStageResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class SucceedingHandler(ILog logger) : RequestHandler<PostStageRequest, PostStageResponse>(logger)
    {
        public override Task<PostStageResponse> HandleAsync(PostStageRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new PostStageResponse { Value = "handled-ok" });
    }

    public class ThrowingPostStage(ILog logger) : PipeStage<PostStageRequest, PostStageResponse>(logger)
    {
        protected override Task<StageResult<PostStageRequest, PostStageResponse>> ExecuteInternalAsync(
            Guid instanceId, PostStageRequest request, CancellationToken cancellationToken)
            => throw new TimeoutException("post-stage timed out");
    }

    public class LaterPostStage(ILog logger) : PipeStage<PostStageRequest, PostStageResponse>(logger)
    {
        public static int InvocationCount;

        protected override Task<StageResult<PostStageRequest, PostStageResponse>> ExecuteInternalAsync(
            Guid instanceId, PostStageRequest request, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(StageResult.WithIndeterminateResult<PostStageRequest, PostStageResponse>(GetType()));
        }
    }

    public class TimeoutRecoveryHandler : IStageExceptionHandler<PostStageRequest, PostStageResponse, TimeoutException>
    {
        public int Order => 0;

        public Task HandleAsync(
            PostStageRequest request,
            TimeoutException exception,
            RequestExceptionHandlerState<PostStageResponse> state,
            CancellationToken cancellationToken)
        {
            state.SetHandled(new PostStageResponse { Value = "recovered-from-timeout" });
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_Handled_PostExecution_Stage_Exception_Should_Skip_Every_Remaining_Stage()
    {
        // Arrange: the Handler already succeeded; a post-stage after it then throws and is handled. The
        // short-circuit rule is uniform (ADR-0016) — a LATER post-stage must not run either.
        LaterPostStage.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<PostStageRequest, PostStageResponse>(p =>
        {
            p.AddHandler<SucceedingHandler>();
            p.AddStage<ThrowingPostStage>();
            p.AddStage<LaterPostStage>();
        }), new Mock<ILog>().Object);
        services.AddTransient<IStageExceptionHandler<PostStageRequest, PostStageResponse, TimeoutException>, TimeoutRecoveryHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<PostStageRequest, PostStageResponse>>();

        // Act
        var result = await pipe.PushWithDebugAsync(new PostStageRequest());

        // Assert
        Assert.Equal("recovered-from-timeout", result.Response!.Value);
        Assert.True(result.ShortCircuited);
        Assert.Equal(0, LaterPostStage.InvocationCount);
        Assert.Equal(2, result.Metrics.Length); // SucceedingHandler + ThrowingPostStage only, LaterPostStage excluded
    }
}
```

- [ ] **Step 2: Run the test to verify it fails for the right reason**

Run: `dotnet test --filter FullyQualifiedName~StageScopedPostExecutionTests`
Expected: PASS if Task 1's `BuildablePipe.PushInternalAsync` `break` (Step 8) is correctly placed inside the `for` loop after `ExecuteStage` returns `ShortCircuited: true`. If `LaterPostStage.InvocationCount` is `1` instead of `0`, the loop isn't breaking — fix `BuildablePipe.cs` to match Task 1's Step 8 code exactly (the `break` must come after `response = result.Response ?? response;`, inside the same iteration that detected the short-circuit).

- [ ] **Step 3: Run the full suite**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add test/conduit.tests/ExceptionHandling/StageScopedPostExecutionTests.cs
git commit -m "test(exceptions): pin that a handled post-execution stage exception skips every remaining stage"
```

---

### Task 7: `IPassthroughException` exemption

**Files:**
- Test: `test/conduit.tests/ExceptionHandling/PassthroughExceptionExemptionTests.cs`

**Interfaces:**
- Consumes: `IStageExceptionHandler<,,>` (Task 1); `conduit.validation.ValidatorNotFoundException` (pre-existing, already an `IPassthroughException` per ADR-0004).
- Produces: nothing new.

- [ ] **Step 1: Write the failing test**

`test/conduit.tests/ExceptionHandling/PassthroughExceptionExemptionTests.cs`:
```csharp
using conduit.Exceptions.Handling;
using conduit.logging;
using conduit.Pipes.Stages;
using conduit.validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class PassthroughExceptionExemptionTests
{
    public class PassthroughRequest : IRequest<PassthroughResponse>;

    public class PassthroughResponse;

    public class ValidatorNotFoundThrowingStage(ILog logger) : PipeStage<PassthroughRequest, PassthroughResponse>(logger)
    {
        protected override Task<StageResult<PassthroughRequest, PassthroughResponse>> ExecuteInternalAsync(
            Guid instanceId, PassthroughRequest request, CancellationToken cancellationToken)
            => throw new ValidatorNotFoundException("no validator registered");
    }

    public class NeverCalledHandler : IStageExceptionHandler<PassthroughRequest, PassthroughResponse, ValidatorNotFoundException>
    {
        public static int InvocationCount;
        public int Order => 0;

        public Task HandleAsync(
            PassthroughRequest request,
            ValidatorNotFoundException exception,
            RequestExceptionHandlerState<PassthroughResponse> state,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            state.SetHandled(new PassthroughResponse());
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_ValidatorNotFoundException_Should_Bypass_Dispatch_Even_When_A_Matching_Handler_Is_Registered()
    {
        // Arrange
        NeverCalledHandler.InvocationCount = 0;
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<PassthroughRequest, PassthroughResponse>(p => p.AddStage<ValidatorNotFoundThrowingStage>()), new Mock<ILog>().Object);
        services.AddTransient<IStageExceptionHandler<PassthroughRequest, PassthroughResponse, ValidatorNotFoundException>, NeverCalledHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<PassthroughRequest, PassthroughResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new PassthroughRequest());

        // Assert: exception passes through unwrapped, and the registered handler was never even invoked.
        await Assert.ThrowsAsync<ValidatorNotFoundException>(act);
        Assert.Equal(0, NeverCalledHandler.InvocationCount);
    }
}
```

- [ ] **Step 2: Run the test**

Run: `dotnet test --filter FullyQualifiedName~PassthroughExceptionExemptionTests`
Expected: PASS — `Pipe.cs`'s `if (e is IPassthroughException) throw;` (unchanged since before Task 1) runs before dispatch is ever attempted, so `NeverCalledHandler` is provably never resolved or invoked.

- [ ] **Step 3: Run the full suite**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add test/conduit.tests/ExceptionHandling/PassthroughExceptionExemptionTests.cs
git commit -m "test(exceptions): pin that IPassthroughException types bypass dispatch even with a matching handler registered"
```

---

### Task 8: Handler/action that itself throws propagates directly

**Files:**
- Test: `test/conduit.tests/ExceptionHandling/ExceptionHandlerRethrowTests.cs`

**Interfaces:**
- Consumes: `IRequestExceptionHandler<,,>` (Task 1).
- Produces: nothing new.

- [ ] **Step 1: Write the failing test**

`test/conduit.tests/ExceptionHandling/ExceptionHandlerRethrowTests.cs`:
```csharp
using conduit.Exceptions.Handling;
using conduit.logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace conduit.tests.ExceptionHandling;

public class ExceptionHandlerRethrowTests
{
    public class RethrowRequest : IRequest<RethrowResponse>;

    public class RethrowResponse;

    public class ThrowingHandler(ILog logger) : RequestHandler<RethrowRequest, RethrowResponse>(logger)
    {
        public override Task<RethrowResponse> HandleAsync(RethrowRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("original failure");
    }

    public class BrokenExceptionHandler : IRequestExceptionHandler<RethrowRequest, RethrowResponse, InvalidOperationException>
    {
        public int Order => 0;

        public Task HandleAsync(
            RethrowRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<RethrowResponse> state,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("handler itself is broken");
    }

    [Fact]
    public async Task A_Handler_That_Throws_Should_Propagate_Directly_Not_Be_Wrapped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConduit(c => c.RegisterPipe<RethrowRequest, RethrowResponse>(p => p.AddHandler<ThrowingHandler>()), new Mock<ILog>().Object);
        services.AddTransient<IRequestExceptionHandler<RethrowRequest, RethrowResponse, InvalidOperationException>, BrokenExceptionHandler>();
        var provider = services.BuildServiceProvider();
        var pipe = provider.GetRequiredService<IPipe<RethrowRequest, RethrowResponse>>();

        // Act
        Func<Task> act = () => pipe.PushAsync(new RethrowRequest());

        // Assert: the handler's own exception surfaces raw — not StageFailedException, not swallowed.
        await Assert.ThrowsAsync<NotSupportedException>(act);
    }
}
```

- [ ] **Step 2: Run the test**

Run: `dotnet test --filter FullyQualifiedName~ExceptionHandlerRethrowTests`
Expected: PASS — `ExceptionHandlerDispatcher.DispatchAsync` (Task 1) has no `try`/`catch` around the reflection `Invoke` calls, so an exception from `method.Invoke(...)` (surfaced as the handler's own exception, since `MethodInfo.Invoke` unwraps a single async-method exception the same way `await` does — matches the codebase's existing "await unwraps single exceptions" ADR-0014 expectation) propagates straight out of `ExecuteStage`'s `catch` block, past `BuildablePipe`, to the caller.

- [ ] **Step 3: Run the full suite**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 4: Confirm netstandard2.0 still builds clean**

Run: `dotnet build -f netstandard2.0 src/conduit/conduit.csproj`
Expected: succeeds with zero warnings.

- [ ] **Step 5: Commit**

```bash
git add test/conduit.tests/ExceptionHandling/ExceptionHandlerRethrowTests.cs
git commit -m "test(exceptions): pin that a handler/action that itself throws propagates directly, not wrapped"
```

---

### Task 9: Documentation — `CONTEXT.md` glossary, `ROADMAP.md` closure

**Files:**
- Modify: `CONTEXT.md`
- Modify: `ROADMAP.md`

**Interfaces:**
- Consumes: nothing (docs only).
- Produces: nothing (docs only).

- [ ] **Step 1: Add glossary entries to `CONTEXT.md`**

Insert the following as new entries immediately before the existing **Notification** entry (the last entry in the file):

```markdown
**RequestExceptionHandler / RequestExceptionAction**:
Per-`(TRequest, TException)` interception points for exceptions thrown specifically by a `Pipe`'s `Handler` stage (`IRequestExceptionHandler<TRequest,TResponse,TException>`/`IRequestExceptionAction<TRequest,TException>`), named to mirror MediatR's equivalents directly. An `Action` observes/reacts and always lets the exception continue; a `Handler` may call `RequestExceptionHandlerState<TResponse>.SetHandled(response)` to supply a response instead, which short-circuits the rest of the `Pipe`. Multiple registrations run in ascending `Order`; matching walks the exception's real inheritance chain.
_Avoid_: Middleware, pipeline behavior — these intercept exceptions per-type, they do not wrap-and-call-next.

**StageExceptionHandler / StageExceptionAction**:
The same mechanism as `RequestExceptionHandler`/`RequestExceptionAction`, scoped instead to exceptions thrown by any `Stage` other than the `Handler` (pre-execution, post-execution, including framework stages like `ValidationStage<,>`). Kept as a separate interface pair rather than one shared interface so a consumer chooses "Handler failure" vs. "any other stage failure" at the type level, not inside a handler body.

**Short-circuit**:
The pipe-level effect of a `RequestExceptionHandler`/`StageExceptionHandler` calling `SetHandled`: the `Pipe` stops immediately and returns that response, running no further `Stage` — uniformly, regardless of which stage threw. This is the explicit, `Pipe`-level mechanism [ADR-0002](docs/adr/0002-flat-sequential-stage-execution.md) anticipated; a `Stage` still cannot decide for itself whether the next one runs.
_Avoid_: "abort"/"cancel" — unrelated to `CancellationToken`.

```

- [ ] **Step 2: Remove item #1 from `ROADMAP.md` and renumber**

Delete the entire "### 1. General exception handling (real gap)" section (its heading through its "**Status**" paragraph, up to but not including "### 2. Void-response requests..."). Renumber every remaining section header: `### 2.` → `### 1.`, `### 3.` → `### 2.`, `### 4.` → `### 3.`, `### 5.` → `### 4.`, `### 6.` → `### 5.`. No other text in the file references a section number, so no further changes are needed.

- [ ] **Step 3: Run the full suite one final time**

Run: `dotnet build && dotnet test`
Expected: PASS — docs-only changes, but this confirms the branch is clean before the final commit.

- [ ] **Step 4: Confirm netstandard2.0 still builds clean (final check for this plan)**

Run: `dotnet build -f netstandard2.0 src/conduit/conduit.csproj`
Expected: succeeds with zero warnings.

- [ ] **Step 5: Commit**

```bash
git add CONTEXT.md ROADMAP.md
git commit -m "docs: close general exception handling roadmap item

CONTEXT.md gains glossary entries for RequestExceptionHandler/Action,
StageExceptionHandler/Action, and Short-circuit. ROADMAP.md item #1 is
removed per its own policy (an item stays listed only until implemented)."
```
