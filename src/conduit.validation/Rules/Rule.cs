using conduit.common;
using conduit.Pipes.Stages;

namespace conduit.validation.Rules;

/// <summary>
/// Defines the contract for a validation rule that can be executed synchronously or asynchronously.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public interface IRule<TRequest> where TRequest : class
{
    /// <summary>
    /// Synchronously validates the specified request using this rule.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult<TRequest> Validate(TRequest request);
    
    /// <summary>
    /// Asynchronously validates the specified request using this rule.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A task that represents the asynchronous operation, returning the validation result.</returns>
    Task<ValidationResult<TRequest>> ValidateAsync(TRequest request);
}


/// <summary>
/// Provides an implementation of <see cref="IRule{TRequest}"/> that wraps a validation function.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public class Rule<TRequest> : IRule<TRequest>
    where TRequest : class
{
    private readonly Func<TRequest, Task<ValidationResult<TRequest>>> _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rule{TRequest}"/> class with a synchronous validation function.
    /// </summary>
    /// <param name="validator">The synchronous validation function.</param>
    public Rule(Func<TRequest, ValidationResult<TRequest>> validator) 
        => _validator = r => Task.FromResult(validator(r));

    /// <summary>
    /// Initializes a new instance of the <see cref="Rule{TRequest}"/> class with an asynchronous validation function.
    /// </summary>
    /// <param name="validator">The asynchronous validation function.</param>
    public Rule(Func<TRequest, Task<ValidationResult<TRequest>>> validator) 
        => _validator = validator;

    /// <inheritdoc/>
    public ValidationResult<TRequest> Validate(TRequest request) 
        => ValidateAsync(request).Await();

    /// <inheritdoc/>
    public async Task<ValidationResult<TRequest>> ValidateAsync(TRequest request)
        => await _validator(request);
}