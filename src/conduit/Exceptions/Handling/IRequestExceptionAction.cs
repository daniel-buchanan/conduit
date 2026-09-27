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
