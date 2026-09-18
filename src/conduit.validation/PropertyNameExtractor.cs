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
}
