using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using conduit.Exceptions.Handling;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Pipes;

/// <summary>
/// Resolves, orders, and invokes <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/> /
/// <see cref="IRequestExceptionAction{TRequest,TException}"/> (when <c>isHandlerStage</c> is <c>true</c>) or
/// <see cref="IStageExceptionHandler{TRequest,TResponse,TException}"/> /
/// <see cref="IStageExceptionAction{TRequest,TException}"/> (otherwise) for a caught exception, walking the
/// exception's real type hierarchy up to and including <see cref="Exception"/>.
/// </summary>
internal static class ExceptionHandlerDispatcher
{
    internal static async Task<(bool Handled, TResponse? Response)> DispatchAsync<TRequest, TResponse>(
        IServiceProvider provider,
        TRequest request,
        Exception exception,
        bool isHandlerStage,
        CancellationToken cancellationToken)
        where TRequest : class, IRequest<TResponse>
        where TResponse : class
    {
        var actionInterface = isHandlerStage ? typeof(IRequestExceptionAction<,>) : typeof(IStageExceptionAction<,>);
        var handlerInterface = isHandlerStage ? typeof(IRequestExceptionHandler<,,>) : typeof(IStageExceptionHandler<,,>);

        var actions = new List<(int Order, object Instance, MethodInfo Method)>();
        var handlers = new List<(int Order, object Instance, MethodInfo Method)>();

        foreach (var ancestor in ExceptionHierarchy(exception.GetType()))
        {
            var closedActionType = actionInterface.MakeGenericType(typeof(TRequest), ancestor);
            var actionMethod = closedActionType.GetMethod("ExecuteAsync")!;
            foreach (var instance in provider.GetServices(closedActionType))
            {
                if (instance is null) continue;
                actions.Add((((IOrderedExceptionParticipant)instance).Order, instance, actionMethod));
            }

            var closedHandlerType = handlerInterface.MakeGenericType(typeof(TRequest), typeof(TResponse), ancestor);
            var handlerMethod = closedHandlerType.GetMethod("HandleAsync")!;
            foreach (var instance in provider.GetServices(closedHandlerType))
            {
                if (instance is null) continue;
                handlers.Add((((IOrderedExceptionParticipant)instance).Order, instance, handlerMethod));
            }
        }

        foreach (var (_, instance, method) in actions.OrderBy(a => a.Order))
        {
            await InvokeAsTask(method, instance, [request, exception, cancellationToken]);
        }

        var state = new RequestExceptionHandlerState<TResponse>();
        foreach (var (_, instance, method) in handlers.OrderBy(h => h.Order))
        {
            await InvokeAsTask(method, instance, [request, exception, state, cancellationToken]);
            if (state.Handled) break;
        }

        return (state.Handled, state.Response);
    }

    private static Task InvokeAsTask(MethodInfo method, object instance, object[] args)
    {
        try
        {
            return (Task)method.Invoke(instance, args)!;
        }
        catch (TargetInvocationException tie) when (tie.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            throw; // unreachable — ExceptionDispatchInfo.Throw() always throws, this satisfies the compiler's control-flow analysis
        }
    }

    private static IEnumerable<Type> ExceptionHierarchy(Type exceptionType)
    {
        var current = exceptionType;
        while (current is not null)
        {
            yield return current;
            if (current == typeof(Exception)) yield break;
            current = current.BaseType;
        }
    }
}
