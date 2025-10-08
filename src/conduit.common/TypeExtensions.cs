namespace conduit.common;

public static class TypeExtensions
{
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