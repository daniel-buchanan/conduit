# Assembly-scan methods are flat `Register*FromAssemblyContaining<T>()` calls; a lambda selector is the future form

The scan methods were named `RegisterHandlersAsImplementedFrom<T>()` and `RegisterPipesAsImplementedFrom<T>()`. Adding exception-handler scanning would have produced `RegisterExceptionHandlersAsImplementedFrom<T>()`. "AsImplementedFrom" doesn't say what is scanned: `T` only locates an assembly. All three are now `Register{Handlers,Pipes,ExceptionHandlers}FromAssemblyContaining<T>()`, the wording .NET users already know from MediatR's `RegisterServicesFromAssemblyContaining<T>()` and FluentValidation's `AddValidatorsFromAssemblyContaining<T>()`. The rename landed before 1.0.0 was published, so it broke no consumer.

A chained fluent form, `RegisterExceptionHandlers().FromAssemblyContaining<T>()`, was rejected. `config.RegisterExceptionHandlers();` on its own would compile, register nothing, and give no warning, so a handler would silently never run. It would also add a public selector type for each kind of registration, all for a single source option.

If scanning ever needs more than one source or a filter (several assemblies, a namespace predicate), add a lambda selector alongside the flat methods rather than a chained one:

```csharp
config.RegisterExceptionHandlers(scan => scan.FromAssemblyContaining<T>().Where(t => t.Namespace == "..."));
```

The lambda can't be left unfinished, so the silent no-op case can't happen, and adding it next to the flat methods is non-breaking.
