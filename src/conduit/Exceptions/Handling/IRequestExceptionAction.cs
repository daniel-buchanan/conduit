namespace conduit.Exceptions.Handling;

/// <summary>
/// Observes an exception thrown specifically by a <c>Pipe</c>'s <c>Handler</c> stage. Always runs in
/// full, for every match, before any <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/>
/// is attempted, and cannot suppress the eventual throw itself. Named after MediatR's
/// <c>IRequestExceptionAction</c> for direct cross-reference.
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
