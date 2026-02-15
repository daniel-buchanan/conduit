using System.Reflection;

namespace conduit.validation;

public interface IValidationBuilder
{
    IValidationBuilder WithValidatorFor<TRequest, TResponse>(IModelValidator<TRequest, TResponse> validator)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class;
    IValidationBuilder WithValidatorFor<TRequest, TResponse, TModelValidator>()
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
        where TModelValidator : IModelValidator<TRequest, TResponse>;

    IValidationBuilder WithValidatorsFromAssembly<TLocator>();
    IValidationBuilder WithValidatorsFromAssembly(Assembly assembly);
    IValidationBuilder ThrowIfValidatorNotFound();
}