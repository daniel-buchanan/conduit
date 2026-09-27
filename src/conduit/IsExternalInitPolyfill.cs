#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    // netstandard2.0 predates this marker type, which the compiler requires to emit
    // init-only setters and records. Safe to declare per-assembly: it only needs to
    // be visible to the compiler, never to consumers.
    internal static class IsExternalInit;
}
#endif
