namespace conduit.Pipes;

/// <summary>
/// Marker interface implemented by pipe stages that perform model validation.
/// Used so a pipe can opt out of the globally registered default validation stage
/// via <see cref="Configuration.PipeDescriptor.ExcludeValidation"/> without the core
/// pipeline machinery needing a direct reference to the validation package.
/// </summary>
public interface IValidationPipeStage;
