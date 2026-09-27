namespace conduit.Exceptions.Handling;

/// <summary>
/// Handles an exception thrown specifically by a <c>Pipe</c>'s <c>Handler</c> stage for a given
/// <typeparamref name="TRequest"/>/<typeparamref name="TException"/> pair, and may supply a response to
/// short-circuit the pipe instead of letting the exception propagate. Named after MediatR's
/// <c>IRequestExceptionHandler</c> for direct cross-reference (see docs/research/mediatr-feature-gap.md).
/// </summary>
/// <remarks>
/// Register implementations as <c>Transient</c> or <c>Singleton</c>, not <c>Scoped</c> — Conduit's
/// pipe-resolution machinery currently resolves stages, handlers, and exception handlers from the root
/// <see cref="IServiceProvider"/> regardless of which scope requested the pipe, so a <c>Scoped</c>
/// registration will not behave as a per-scope instance (and can throw if <c>ValidateScopes</c> is
/// enabled). A handler registered against the base <see cref="Exception"/> type will also intercept
/// <see cref="OperationCanceledException"/> from a cancelled <see cref="CancellationToken"/> — scope the
/// registration to a narrower exception type if that's not wanted.
/// </remarks>
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
