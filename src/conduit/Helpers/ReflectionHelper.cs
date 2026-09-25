using System.Reflection;
using conduit.Configuration;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Helpers;

public static class ReflectionHelper
{
    private static Type[] GetGenericParameters(Type t)
    {
        var genericParameters = t.GetGenericArguments();
        if (genericParameters.Length == 0 && t.BaseType != null)
            genericParameters = t.BaseType.GetGenericArguments();

        return genericParameters;
    }

    public static Type[] GetTypesFromAssembly<T>(Func<Type, bool> predicate)
        => GetTypesFromAssembly(typeof(T), predicate);

    public static Type[] GetTypesFromAssembly(Type locator, Func<Type, bool> predicate)
        => GetTypesFromAssembly(locator.Assembly, predicate);

    public static Type[] GetTypesFromAssembly(Assembly assembly, Func<Type, bool> predicate)
    {
        var types = assembly.GetTypes();
        return types.Where(t => t.GetInterfaces().Any(predicate) && !t.IsInterface && !t.IsAbstract).ToArray();
    }

    /// <summary>
    /// Builds service descriptors for every closed implementation of <typeparamref name="TBase"/> found in
    /// <typeparamref name="TLocator"/>'s assembly.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A discovered type's closed TRequest/TResponse (or other generic) arguments could not be determined
    /// from its generic base class. Previously this silently registered the type under itself instead,
    /// producing a pipe that could never be resolved via <c>IPipe&lt;TRequest,TResponse&gt;</c> — see ADR-0007's
    /// sibling decisions and the edge-case review (Q7/Q13): failing loud at registration time is preferred
    /// over shipping an unreachable registration.
    /// </exception>
    public static ServiceDescriptor[] GetRegistrationsAsImplementedFrom<TLocator, TBase>()
    {
        var descriptors = new List<ServiceDescriptor>();
        var types = GetTypesFromAssembly<TLocator>(i => i == typeof(TBase));
        foreach (var t in types)
        {
            var genericParameters = GetGenericParameters(t);

            if (genericParameters.Length == 0)
                throw new InvalidOperationException(
                    $"Could not determine the closed generic type arguments for '{t.FullName}'. " +
                    $"Types discovered via {nameof(GetRegistrationsAsImplementedFrom)} must derive from a generic base class " +
                    $"closed over the request/response types (e.g. Pipe<TRequest, TResponse>).");

            var interfaceType = Enumerable.First(t.GetInterfaces());
            if (interfaceType.GetGenericArguments().Any() &&
                interfaceType.GetGenericArguments().Length == genericParameters.Length)
            {
                var interfaceGenericType = interfaceType.IsGenericTypeDefinition ? interfaceType.MakeGenericType(genericParameters) : interfaceType;
                var implementedGenericType = t.IsGenericTypeDefinition ? t.MakeGenericType(genericParameters) : t;
                descriptors.Add(new ServiceDescriptor(interfaceGenericType, implementedGenericType, ServiceLifetime.Scoped));
                continue;
            }

            descriptors.Add(new ServiceDescriptor(interfaceType, t, ServiceLifetime.Scoped));
        }

        return descriptors.ToArray();
    }

    public static ServiceDescriptor[] GetServiceDescriptorsForHandler<TRequest, TResponse, THandler>(bool excludeValidation = false)
        => GetServiceDescriptorsForHandler(typeof(TRequest), typeof(TResponse), typeof(THandler), excludeValidation);

    public static ServiceDescriptor[] GetServiceDescriptorsForHandler(Type request, Type response, Type handler, bool excludeValidation = false)
    {
        var genericPipeInterface = typeof(IPipe<,>);
        var genericPipeConfiguration = typeof(DefaultPipeConfiguration<,,>);
        var genericPipeImpl = typeof(DefaultPipe<,>);
        var genericPreStage = typeof(DebugPreExecutionStage<,>);
        var genericPostStage = typeof(DebugPostExecutionStage<,>);
        var genericHandlerInterface = typeof(IRequestHandler<,>);

        var pipeInterface = genericPipeInterface.MakeGenericType(request, response);
        var pipeImpl = genericPipeImpl.MakeGenericType(request, response);
        var preStage = genericPreStage.MakeGenericType(request, response);
        var postStage = genericPostStage.MakeGenericType(request, response);
        var handlerInterface = genericHandlerInterface.MakeGenericType(request, response);
        var defaultConfiguration = genericPipeConfiguration.MakeGenericType(request, response, handlerInterface);

        var pipeDescriptor = new ServiceDescriptor(pipeInterface, pipeImpl, ServiceLifetime.Scoped);
        var preStageDescriptor = new  ServiceDescriptor(preStage, preStage, ServiceLifetime.Scoped);
        var postStageDescriptor = new ServiceDescriptor(postStage, postStage, ServiceLifetime.Scoped);
        var handlerDescriptor =  new ServiceDescriptor(handlerInterface, handler, ServiceLifetime.Scoped);

        // A factory rather than a type-to-type map: excludeValidation is per-registration data that has to be
        // baked in at configuration time, not something the container can supply by resolving the closed type
        // on its own (see DefaultPipeConfiguration<TRequest,TResponse,THandler>'s excludeValidation parameter).
        var defaultConfigurationDescriptor = new ServiceDescriptor(
            defaultConfiguration,
            provider => Activator.CreateInstance(
                defaultConfiguration,
                provider.GetRequiredService<DefaultPipeConfiguration>(),
                excludeValidation)!,
            ServiceLifetime.Scoped);

        return [pipeDescriptor, preStageDescriptor, postStageDescriptor, handlerDescriptor, defaultConfigurationDescriptor];
    }
}