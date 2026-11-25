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
