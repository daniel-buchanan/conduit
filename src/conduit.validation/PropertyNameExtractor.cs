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
    {
        var body = expression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            body = unary.Operand;

        var names = new List<string>();
        while (body is MemberExpression member)
        {
            names.Insert(0, member.Member.Name);
            body = member.Expression;
        }

        if (names.Count == 0 || body is not ParameterExpression)
            throw new ArgumentException(
                $"Expression '{expression}' must be a simple member access chain (e.g. x => x.Foo.Bar).",
                nameof(expression));

        return string.Join(".", names);
    }

    /// <summary>
    /// Extracts the property name (see <see cref="Extract{TRequest,TProperty}"/>) and compiles the
    /// expression, in one call — the shared shape every <c>Should</c>/<c>ShouldString</c> builder constructor needs.
    /// The compiled delegate is null-safe along the chain: if a member access partway down the path is null
    /// (e.g. <c>x =&gt; x.Address.City</c> when <c>Address</c> is null), it evaluates to <c>default(TProperty)</c>
    /// rather than throwing — the same outcome as the leaf property itself being null. See ADR-0013.
    /// </summary>
    public static (Func<TRequest, TProperty> Compiled, string PropertyName) ExtractAndCompile<TRequest, TProperty>(
        Expression<Func<TRequest, TProperty>> expression)
    {
        var propertyName = Extract(expression);
        var compiled = expression.Compile();

        TProperty NullSafeCompiled(TRequest request)
        {
            try
            {
                return compiled(request);
            }
            catch (NullReferenceException)
            {
                // Extract above already guarantees this expression is a pure member-access chain rooted at
                // the lambda parameter — the only possible source of an NRE from invoking it is a null
                // intermediate somewhere in that chain, never arbitrary user code.
                return default!;
            }
        }

        return (NullSafeCompiled, propertyName);
    }
}
