namespace conduit.validation;

public interface IShouldBuilder<TRequest, in TProperty> where TRequest : class
{
    IShouldBeBuilder<TRequest, TProperty> Should();
    IShouldBeBuilder<TRequest, TProperty> NotBe();
}

public class ShouldBuilder<TRequest, TProperty>(
    IRuleBuilder<TRequest> builder,
    Func<TRequest, TProperty> property) : 
    IShouldBuilder<TRequest, TProperty> where TRequest : class
{
    public IShouldBeBuilder<TRequest, TProperty> Should()
        => new ShouldBeBuilder<TRequest, TProperty>(builder, property);

    public IShouldBeBuilder<TRequest, TProperty> NotBe()
        => new ShouldNotBeBuilder<TRequest, TProperty>(builder, property);
}