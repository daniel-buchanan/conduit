using conduit.Pipes;

namespace conduit.Helpers;

/// <summary>
/// Shared logic for filtering and closing the default pre-/post-execution stage types, used by both
/// registration paths: <c>DefaultPipeConfiguration&lt;TRequest,TResponse,THandler&gt;</c> (RegisterHandler)
/// and <c>PipeFactory</c> (RegisterPipe).
/// </summary>
public static class StageMaterializer
{
    /// <summary>
    /// Whether a default stage type should be included, given whether the current pipe excludes validation.
    /// Skips any stage that implements <see cref="IValidationPipeStage"/> when <paramref name="excludeValidation"/> is true.
    /// </summary>
    public static bool IsIncluded(Type stageType, bool excludeValidation)
        => !excludeValidation || !typeof(IValidationPipeStage).IsAssignableFrom(stageType);

    /// <summary>
    /// Closes an open generic stage type over the given request/response (and, for a 3-parameter stage,
    /// handler) types. Returns the type unchanged if it isn't an open generic type definition.
    /// </summary>
    public static Type Materialize(Type stageType, Type request, Type response, Type? handler = null)
    {
        if (!stageType.IsGenericTypeDefinition) return stageType;
        var parameterCount = stageType.GetGenericArguments().Length;
        return parameterCount < 3
            ? stageType.MakeGenericType(request, response)
            : stageType.MakeGenericType(request, response, handler!);
    }
}
