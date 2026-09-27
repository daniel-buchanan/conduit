namespace conduit.Exceptions.Handling;

/// <summary>
/// Shared ordering contract for the four exception-handling interfaces in this namespace, so
/// <see cref="conduit.Pipes.ExceptionHandlerDispatcher"/> can read a resolved instance's order via a
/// single cast instead of reflecting the property per closed generic type.
/// </summary>
public interface IOrderedExceptionParticipant
{
    /// <summary>
    /// Controls sequencing when multiple handlers/actions match the same exception. Lower values run
    /// first. Ties keep hierarchy-then-registration order (see <see cref="conduit.Pipes.ExceptionHandlerDispatcher"/>).
    /// </summary>
    int Order { get; }
}
