using System.Reflection;
using conduit.Configuration;
using conduit.Pipes;
using conduit.Pipes.Stages;
using Microsoft.Extensions.DependencyInjection;

namespace conduit.Helpers;

public static class ReflectionHelper
{
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
    /// Builds a service descriptor for every closed generic interface deriving from <typeparamref name="TBase"/>
    /// that each concrete implementation in <typeparamref name="TLocator"/>'s assembly implements — e.g.
    /// <c>IPipe&lt;TRequest,TResponse&gt;</c> for <c>TBase = IPipe</c>. Reading the closed types from the
    /// interfaces themselves works whether the type derives from a generic base class or implements the
    /// interface directly, and never depends on <see cref="Type.GetInterfaces"/>'s unspecified order.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A discovered type is an open generic definition, or implements no closed generic interface deriving
    /// from <typeparamref name="TBase"/>, so it could never be resolved by its request/response pair. Failing
    /// loud at registration time is preferred over shipping an unreachable registration (edge-case review Q7/Q13).
    /// </exception>
    public static ServiceDescriptor[] GetRegistrationsFromAssemblyContaining<TLocator, TBase>()
    {
        var descriptors = new List<ServiceDescriptor>();
        foreach (var t in GetTypesFromAssembly<TLocator>(i => i == typeof(TBase)))
        {
            var serviceTypes = ClosedInterfacesOf(t, i => typeof(TBase).IsAssignableFrom(i));
            descriptors.AddRange(serviceTypes.Select(s => new ServiceDescriptor(s, t, ServiceLifetime.Scoped)));
        }

        return descriptors.ToArray();
    }

    /// <summary>
    /// Returns the closed generic interfaces of <paramref name="type"/> matching <paramref name="predicate"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="type"/> is an open generic definition, or no closed generic interface matches.
    /// </exception>
    public static Type[] ClosedInterfacesOf(Type type, Func<Type, bool> predicate)
    {
        var interfaces = type.IsGenericTypeDefinition
            ? []
            : type.GetInterfaces().Where(i => i.IsGenericType && predicate(i)).ToArray();

        if (interfaces.Length == 0)
            throw new InvalidOperationException(
                $"Could not determine the closed generic type arguments for '{type.FullName}'. " +
                "Types discovered by assembly scanning must be non-generic and implement a closed generic " +
                "interface (e.g. IPipe<TRequest, TResponse> or IRequestHandler<TRequest, TResponse>).");

        return interfaces;
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