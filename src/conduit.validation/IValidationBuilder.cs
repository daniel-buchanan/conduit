using System.Reflection;

namespace conduit.validation;

public interface IValidationBuilder
{
    IValidationBuilder WithValidatorFor<TRequest>(IModelValidator<TRequest> validator);
    IValidationBuilder WithValidatorFor<TRequest, TModelValidator>()
        where TModelValidator : IModelValidator<TRequest>;

    IValidationBuilder WithValidatorsFromAssembly<TLocator>();
    IValidationBuilder WithValidatorsFromAssembly(Assembly assembly);
    IValidationBuilder ThrowIfValidatorNotFound();
}