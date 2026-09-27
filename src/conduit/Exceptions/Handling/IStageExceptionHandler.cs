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
