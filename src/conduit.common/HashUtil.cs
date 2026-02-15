using System.Security.Cryptography;
using System.Text;

namespace conduit.common;

/// <summary>
/// Defines the contract for a utility that generates SHA-512 hashes of type names.
/// </summary>
public interface IHashUtil
{
    /// <summary>
    /// Generates a hash of the type name for the specified generic type.
    /// </summary>
    /// <typeparam name="T">The type to hash.</typeparam>
    /// <returns>The SHA-512 hash of the type's full name.</returns>
    string TypeNameHash<T>();
    
    /// <summary>
    /// Generates a hash of the type name for the specified type.
    /// </summary>
    /// <param name="type">The type to hash.</param>
    /// <returns>The SHA-512 hash of the type's full name.</returns>
    string TypeNameHash(Type type);
    
    /// <summary>
    /// Generates a hash of combined type names for a request and response type.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <returns>The SHA-512 hash of the combined type names.</returns>
    string TypeNameHash<TRequest, TResponse>();
    
    /// <summary>
    /// Generates a hash of combined type names for a request and response type.
    /// </summary>
    /// <param name="request">The request type.</param>
    /// <param name="response">The response type.</param>
    /// <returns>The SHA-512 hash of the combined type names.</returns>
    string TypeNameHash(Type request, Type response);
}

/// <summary>
/// Provides utility methods for generating SHA-512 hashes of type names.
/// </summary>
public class HashUtil :  IHashUtil
{
    private static IHashUtil? _hashUtil;
    
    /// <summary>
    /// Gets the singleton instance of the HashUtil.
    /// </summary>
    public static IHashUtil Instance => _hashUtil ??= new HashUtil();
    
    private static string Hash(string input)
    {
        var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        var builder = new StringBuilder();
        foreach (var b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }

    /// <inheritdoc/>
    public string TypeNameHash<T>() => TypeNameHash(typeof(T));

    /// <inheritdoc/>
    public string TypeNameHash(Type type)
    {
        var fullTypeName = type.FullName ?? type.Name;
        return Hash(fullTypeName);
    }

    /// <inheritdoc/>
    public string TypeNameHash<TRequest, TResponse>()
        => TypeNameHash(typeof(TRequest), typeof(TResponse));

    /// <inheritdoc/>
    public string TypeNameHash(Type request, Type response)
    {
        var t1Name = request.FullName ?? request.Name;
        var t2Name = response.FullName ?? response.Name;
        var combined = $"{t1Name}:{t2Name}";
        return Hash(combined);
    }
}