namespace conduit.Exceptions.Handling;

/// <summary>
/// Observes an exception thrown specifically by a <c>Pipe</c>'s <c>Handler</c> stage. Always runs in
/// full, for every match, before any <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/>
/// is attempted, and cannot suppress the eventual throw itself. Named after MediatR's
/// <c>IRequestExceptionAction</c> for direct cross-reference.
/// </summary>
/// <remarks>
/// Resolved from the scope <c>IConduit</c> was resolved in, so any service lifetime works (ADR-0017). Caller
/// cancellation never reaches it: an <see cref="OperationCanceledException"/> raised while the pipe's
/// <see cref="CancellationToken"/> is cancelled passes through unwrapped (ADR-0018).
/// </remarks>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TException">The exception type this action matches (and its subtypes).</typeparam>
public interface IRequestExceptionAction<TRequest, TException> : IOrderedExceptionParticipant
    where TRequest : class
    where TException : Exception
{
    /// <summary>
    /// Observes/reacts to the exception. Cannot suppress it — the pipe still falls through to whatever
    /// the Handler-flavor run decides afterward.
    /// </summary>
    Task ExecuteAsync(TRequest request, TException exception, CancellationToken cancellationToken);
}
