namespace conduit.validation;

public interface IRuleBuilder<TRequest> where TRequest : class
{
    IShouldBuilder<TRequest, TProperty> Should<TProperty>(Func<TRequest, TProperty> property);
    internal Rule<TRequest>[] Build();
    internal void AddRule(Rule<TRequest> rule);
}

public class RuleBuilder<TRequest> : IRuleBuilder<TRequest> where TRequest : class
{
    private readonly List<Rule<TRequest>> _rules = new();
    
    public IShouldBuilder<TRequest, TProperty> Should<TProperty>(Func<TRequest, TProperty> property)
        => new ShouldBuilder<TRequest, TProperty>(this, property);

    public Rule<TRequest>[] Build()
        => _rules.ToArray();

    public void AddRule(Rule<TRequest> rule)
        => _rules.Add(rule);
}