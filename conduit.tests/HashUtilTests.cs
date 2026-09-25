using conduit.common;
using Xunit;

namespace conduit.tests;

public class HashUtilTests
{
    private readonly IHashUtil _hashUtil = new HashUtil();

    public class RequestA;
    public class ResponseA;
    public class RequestB;
    public class ResponseB;

    [Fact]
    public void TypeNameHash_Should_Be_Stable_For_The_Same_Type()
    {
        Assert.Equal(_hashUtil.TypeNameHash<RequestA>(), _hashUtil.TypeNameHash<RequestA>());
    }

    [Fact]
    public void TypeNameHash_Should_Differ_For_Different_Types()
    {
        Assert.NotEqual(_hashUtil.TypeNameHash<RequestA>(), _hashUtil.TypeNameHash<RequestB>());
    }

    [Fact]
    public void TypeNameHash_For_Request_Response_Pair_Should_Differ_By_Order()
    {
        // Arrange & Act
        var forward = _hashUtil.TypeNameHash(typeof(RequestA), typeof(ResponseB));
        var reversed = _hashUtil.TypeNameHash(typeof(ResponseB), typeof(RequestA));

        // Assert: the combined hash is order-sensitive, so a (RequestA, ResponseB) pipe and a
        // (ResponseB, RequestA) pipe never collide in the registry.
        Assert.NotEqual(forward, reversed);
    }

    [Fact]
    public void TypeNameHash_For_Request_Response_Pair_Should_Differ_From_Either_Type_Alone()
    {
        var pairHash = _hashUtil.TypeNameHash(typeof(RequestA), typeof(ResponseA));
        var requestHash = _hashUtil.TypeNameHash<RequestA>();
        var responseHash = _hashUtil.TypeNameHash<ResponseA>();

        Assert.NotEqual(pairHash, requestHash);
        Assert.NotEqual(pairHash, responseHash);
    }

    [Fact]
    public void Instance_Should_Return_The_Same_Singleton()
    {
        Assert.Same(HashUtil.Instance, HashUtil.Instance);
    }

    [Fact]
    public void TypeNameHash_Should_Not_Throw_For_A_Type_With_No_FullName()
    {
        // Arrange: an unbound generic type parameter (T below) has a null Type.FullName, exercising the
        // `type.FullName ?? type.Name` fallback.
        var openParameter = typeof(GenericHolder<>).GetGenericArguments()[0];

        // Act
        var hash = _hashUtil.TypeNameHash(openParameter);

        // Assert
        Assert.False(string.IsNullOrEmpty(hash));
    }

    private class GenericHolder<T>;
}
