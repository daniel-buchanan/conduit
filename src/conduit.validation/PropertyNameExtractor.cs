using System.Linq.Expressions;

namespace conduit.validation;

/// <summary>
/// Extracts a dotted property-path name (e.g. "Address.City") from a member-access expression tree,
/// so <see cref="ValidationError.PropertyName"/> can carry the real property being validated.
/// </summary>
internal static class PropertyNameExtractor
{
    /// <summary>
    /// Extracts the full dotted member-access path from the given expression.
    /// </summary>
    /// <param name="expression">A pure member-access chain, e.g. <c>x => x.Address.City</c>.</param>
    /// <exception cref="ArgumentException">
    /// The expression body is not a pure member-access chain rooted at the lambda parameter
    /// (e.g. it contains a method call, an indexer, or any other non-member node).
    /// </exception>
    public static string Extract<TRequest, TProperty>(Expression<Func<TRequest, TProperty>> expression)
        => string.Join(".", MemberChain(expression).Select(m => m.Member.Name));

    /// <summary>
    /// Extracts the property name (see <see cref="Extract{TRequest,TProperty}"/>) and compiles the
    /// expression, in one call — the shared shape every <c>Should</c>/<c>ShouldString</c> builder constructor needs.
    /// The compiled delegate is null-safe along the chain: if a member access partway down the path is null
    /// (e.g. <c>x =&gt; x.Address.City</c> when <c>Address</c> is null), it evaluates to <c>default(TProperty)</c>
    /// rather than throwing — the same outcome as the leaf property itself being null. Exceptions thrown
    /// inside a property getter still propagate. See ADR-0013 and ADR-0019.
    /// </summary>
    public static (Func<TRequest, TProperty> Compiled, string PropertyName) ExtractAndCompile<TRequest, TProperty>(
        Expression<Func<TRequest, TProperty>> expression)
    {
        var chain = MemberChain(expression);
        var propertyName = string.Join(".", chain.Select(m => m.Member.Name));

        // Rebuild the chain as: tmp0 = x.A; if (tmp0 == null) return default; tmp1 = tmp0.B; ... return tmpN.
        var parameter = expression.Parameters[0];
        var returnLabel = Expression.Label(typeof(TProperty));
        var returnDefault = Expression.Return(returnLabel, Expression.Default(typeof(TProperty)));
        var variables = new List<ParameterExpression>();
        var body = new List<Expression>();

        Expression current = parameter;
        foreach (var member in chain)
        {
            if (CanBeNull(current.Type))
                body.Add(Expression.IfThen(Expression.Equal(current, Expression.Constant(null, current.Type)), returnDefault));

            var next = Expression.Variable(member.Type);
            variables.Add(next);
            body.Add(Expression.Assign(next, Expression.MakeMemberAccess(current, member.Member)));
            current = next;
        }

        body.Add(Expression.Label(returnLabel, Expression.Convert(current, typeof(TProperty))));
        var compiled = Expression.Lambda<Func<TRequest, TProperty>>(Expression.Block(variables, body), parameter).Compile();

        return (compiled, propertyName);
    }

    private static bool CanBeNull(Type type)
        => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

    /// <summary>
    /// Returns the member accesses of a pure member-access chain, outermost (closest to the parameter) first.
    /// </summary>
    /// <exception cref="ArgumentException">The expression is not a pure member-access chain rooted at the lambda parameter.</exception>
    private static List<MemberExpression> MemberChain<TRequest, TProperty>(Expression<Func<TRequest, TProperty>> expression)
    {
        var body = expression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            body = unary.Operand;

        var chain = new List<MemberExpression>();
        while (body is MemberExpression member)
        {
            chain.Insert(0, member);
            body = member.Expression;
        }

        if (chain.Count == 0 || body is not ParameterExpression)
            throw new ArgumentException(
                $"Expression '{expression}' must be a simple member access chain (e.g. x => x.Foo.Bar).",
                nameof(expression));

        return chain;
    }
}
