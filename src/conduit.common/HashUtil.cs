using System.Security.Cryptography;
using System.Text;

namespace conduit.common;

public interface IHashUtil
{
    string TypeNameHash<T>();
    string TypeNameHash(Type type);
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
}