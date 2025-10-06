using System.Security.Cryptography;
using System.Text;

namespace conduit.common;

public interface IHashUtil
{
    string TypeNameHash<T>();
    string TypeNameHash(Type type);
    string TypeNameHash<TRequest, TResponse>();
    string TypeNameHash(Type request, Type response);
}

public class HashUtil :  IHashUtil
{
    private static IHashUtil? _hashUtil;
    public static IHashUtil Instance => _hashUtil ??= new HashUtil();
    
    private static string Hash(string input)
    {
        using var hash = SHA512.Create();
        var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(input));
        var builder = new StringBuilder();
        foreach (var b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }

    public string TypeNameHash<T>()
    {
        var fullTypeName = typeof(T).FullName;
        return Hash(fullTypeName);
    }

    public string TypeNameHash(Type type)
    {
        var fullTypeName = type.FullName;
        return Hash(fullTypeName);
    }

    public string TypeNameHash<TRequest, TResponse>()
        => TypeNameHash(typeof(TRequest), typeof(TResponse));

    public string TypeNameHash(Type request, Type response)
    {
        var t1Name = request.FullName;
        var t2Name = response.FullName;
        var combined = $"{t1Name}:{t2Name}";
        return Hash(combined);
    }
}