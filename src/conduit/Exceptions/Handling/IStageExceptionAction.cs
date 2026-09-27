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
