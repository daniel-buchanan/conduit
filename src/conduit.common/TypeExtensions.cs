namespace conduit.common;

/// <summary>
/// Provides extension methods for working with type information in the Conduit system.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Gets a human-readable name for a type, including its generic type arguments if present.
    /// </summary>
    /// <param name="type">The type to get the generic name for.</param>
    /// <returns>A fully qualified generic name in the format "Namespace.TypeName&lt;GenericArgs&gt;".</returns>
    public static string GetGenericName(this Type type)
    {
        if (!type.IsGenericType) return type.Name;
        var genericParameters = type.GetGenericArguments();
        var genericNames = string.Join(", ", genericParameters.Select(GetGenericName));
        var typeName = type.Name;
        const char backtick = '`';
        if(typeName.Contains(backtick)) typeName = typeName.Substring(0, typeName.IndexOf(backtick));
        var typeNamespace = type.Namespace;
        return $"{typeNamespace}.{typeName}<{genericNames}>";
    }
}