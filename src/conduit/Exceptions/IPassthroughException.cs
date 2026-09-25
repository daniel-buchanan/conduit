namespace conduit.Exceptions;

/// <summary>
/// Marker interface for exceptions that a <c>Pipe</c> must let propagate unwrapped from
/// <c>ExecuteStage</c>, rather than wrapping in a <see cref="StageFailedException"/>.
/// Implemented directly by <see cref="ValidationFailedException"/> and <see cref="StageFailedException"/>
/// themselves, and by exceptions defined in other packages (e.g. conduit.validation's
/// ValidatorNotFoundException) that core must recognize without referencing that package's assembly —
/// the same problem <see cref="conduit.Pipes.IValidationPipeStage"/> solves for stages. See ADR-0004 and
/// ADR-0007.
/// </summary>
public interface IPassthroughException;
