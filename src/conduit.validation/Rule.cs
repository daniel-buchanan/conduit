using System;
using conduit.Pipes.Stages;

namespace conduit.validation;

public interface IRule<TRequest> where TRequest : class
{
    ValidationResult<TRequest> Validate(TRequest request);
    Task<ValidationResult<TRequest>> ValidateAsync(TRequest request);
}


public class Rule<TRequest> : IRule<TRequest>
    where TRequest : class
{
    private readonly Func<TRequest, Task<ValidationResult<TRequest>>> _validator;

    public Rule(Func<TRequest, ValidationResult<TRequest>> validator) 
        => _validator = r => Task.FromResult(validator(r));

    public Rule(Func<TRequest, Task<ValidationResult<TRequest>>> validator) => 
        _validator = validator;

    public ValidationResult<TRequest> Validate(TRequest request)
    {
        var t = ValidateAsync(request);
        t.Wait();
        return t.Result;
    }
    
    public async Task<ValidationResult<TRequest>> ValidateAsync(TRequest request)
        => await _validator(request);
}