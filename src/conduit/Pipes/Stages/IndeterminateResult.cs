namespace conduit.Pipes.Stages;

/// <summary>
/// Represents an indeterminate result from a pipeline stage that doesn't produce a definitive response or error.
/// This is used for stages that pass control to the next stage without producing a result.
/// </summary>
public class IndeterminateResult;