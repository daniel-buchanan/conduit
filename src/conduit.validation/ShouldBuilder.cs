using conduit.Pipes.Stages;

namespace conduit.validation;

public interface IShouldBuilder<TRequest, in TProperty> where TRequest : class
{
    IShouldBeBuilder<TRequest, TProperty> Should();
    IShouldNotBeBuilder<TRequest, TProperty> NotBe();
}

public interface IShouldNotBeBuilder<TRequest, in TProperty> where TRequest : class
{
    IRuleBuilder<TRequest> Null(string? message = null);
    IRuleBuilder<TRequest> EqualTo(TProperty value, string? message = null);
    IRuleBuilder<TRequest> In(IEnumerable<TProperty> values);
    IRuleBuilder<TRequest> In(string message, IEnumerable<TProperty> values);
    IRuleBuilder<TRequest> OneOf(IEnumerable<TProperty> values);
    IRuleBuilder<TRequest> OneOf(string message, IEnumerable<TProperty> values);
}

public interface IShouldBeBuilder<TRequest, in TProperty> where TRequest : class
{
    IRuleBuilder<TRequest> Null(string? message = null);
    IRuleBuilder<TRequest> EqualTo(TProperty value, string? message = null);
    IRuleBuilder<TRequest> In(IEnumerable<TProperty> values);
    IRuleBuilder<TRequest> In(string message, IEnumerable<TProperty> values);
    IRuleBuilder<TRequest> OneOf(IEnumerable<TProperty> values);
    IRuleBuilder<TRequest> OneOf(string message, IEnumerable<TProperty> values);
}


public class ShouldBuilder<TRequest, TProperty>(
    IRuleBuilder<TRequest> builder,
    Func<TRequest, TProperty> property) : 
    IShouldBuilder<TRequest, TProperty> where TRequest : class
{
    public IShouldBeBuilder<TRequest, TProperty> Should()
        => new ShouldBeBuilder<TRequest, TProperty>(builder, property);

    public IShouldNotBeBuilder<TRequest, TProperty> NotBe()
        => new ShouldNotBeBuilder<TRequest, TProperty>(builder, property);
}

public abstract class AbstractShouldBuilder<TRequest>(IRuleBuilder<TRequest> builder)
    where TRequest : class
{
    protected void AddRule(Func<TRequest, bool> validator, string? message = null)
    {
        var rule = new Rule<TRequest>(r => validator(r) 
            ? ValidationResult.WithSuccess(r) 
            : ValidationResult.WithFailure(r, [new ValidationError(r.ToString() ?? string.Empty, message)]));
        builder.AddRule(rule);
    }
}

public class ShouldNotBeBuilder<TRequest, TProperty>(
    IRuleBuilder<TRequest> builder,
    Func<TRequest, TProperty> property) : 
    AbstractShouldBuilder<TRequest>(builder),
    IShouldNotBeBuilder<TRequest, TProperty> where TRequest : class
{
    private readonly IRuleBuilder<TRequest> _builder = builder;

    public IRuleBuilder<TRequest> Null(string? message = null)
    {
        AddRule(r => property(r) != null, message);
        return _builder;
    }

    public IRuleBuilder<TRequest> EqualTo(TProperty value, string? message = null)
    {
        AddRule(r => !Equals(property(r), value), message);
        return _builder;
    }

    public IRuleBuilder<TRequest> In(IEnumerable<TProperty> values)
    {
        AddRule(r => !values.Contains(property(r)));
        return _builder;
    }

    public IRuleBuilder<TRequest> In(string message, IEnumerable<TProperty> values)
    {
        AddRule(r => !values.Contains(property(r)), message);
        return _builder;
    }

    public IRuleBuilder<TRequest> OneOf(IEnumerable<TProperty> values)
        => In(values);

    public IRuleBuilder<TRequest> OneOf(string message, IEnumerable<TProperty> values)
        => In(message, values);
}

public class ShouldBeBuilder<TRequest, TProperty>(
    IRuleBuilder<TRequest> builder,
    Func<TRequest, TProperty> property) : 
    AbstractShouldBuilder<TRequest>(builder),
    IShouldBeBuilder<TRequest, TProperty> where TRequest : class
{
    private readonly IRuleBuilder<TRequest> _builder = builder;

    public IRuleBuilder<TRequest> Null(string? message = null)
    {
        AddRule(r => property(r) == null, message);
        return _builder;
    }

    public IRuleBuilder<TRequest> EqualTo(TProperty value, string? message = null)
    {
        AddRule(r => Equals(property(r), value), message);
        return _builder;
    }

    public IRuleBuilder<TRequest> In(IEnumerable<TProperty> values)
    {
        AddRule(r => values.Contains(property(r)));
        return _builder;
    }

    public IRuleBuilder<TRequest> In(string message, IEnumerable<TProperty> values)
    {
        AddRule(r => values.Contains(property(r)), message);
        return _builder;
    }

    public IRuleBuilder<TRequest> OneOf(IEnumerable<TProperty> values)
        => In(values);

    public IRuleBuilder<TRequest> OneOf(string message, IEnumerable<TProperty> values)
        => In(message, values);
}