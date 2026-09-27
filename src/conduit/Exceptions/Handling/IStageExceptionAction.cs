namespace conduit.Exceptions.Handling;

/// <summary>
/// Observes an exception thrown by any <c>Stage</c> in a <c>Pipe</c> OTHER than the <c>Handler</c>.
/// Always runs in full, for every match, before any
/// <see cref="IStageExceptionHandler{TRequest,TResponse,TException}"/> is attempted, and cannot suppress
/// the eventual throw itself.
/// </summary>
/// <remarks>
/// Register implementations as <c>Transient</c> or <c>Singleton</c>, not <c>Scoped</c> — Conduit's
/// pipe-resolution machinery currently resolves stages, handlers, and exception handlers from the root
/// <see cref="IServiceProvider"/> regardless of which scope requested the pipe, so a <c>Scoped</c>
/// registration will not behave as a per-scope instance (and can throw if <c>ValidateScopes</c> is
/// enabled). An action registered against the base <see cref="Exception"/> type will also intercept
/// <see cref="OperationCanceledException"/> from a cancelled <see cref="CancellationToken"/> — scope the
/// registration to a narrower exception type if that's not wanted.
/// </remarks>
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
