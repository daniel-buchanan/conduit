using System.Reflection;
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
    {
        var assembly = locator.Assembly;
        var types = assembly.GetTypes();
        return types.Where(t => t.GetInterfaces().Any(predicate) && !t.IsInterface).ToArray();
    }
    
    public static Type[] GetTypesFromAssembly(Assembly assembly, Func<Type, bool> predicate)
    {
        var types = assembly.GetTypes();
        return types.Where(t => t.GetInterfaces().Any(predicate) && !t.IsInterface).ToArray();
    }

    public static ServiceDescriptor[] GetRegistrationsAsImplementedFrom<TLocator, TBase>(bool includeCtorDeps = false)
    {
        var descriptors = new List<ServiceDescriptor>();
        var types = GetTypesFromAssembly<TLocator>(i => i == typeof(TBase));
        foreach (var t in types)
        {
            if (includeCtorDeps) descriptors.AddRange(GetServiceDescriptorsForCtorDependencies(t, types));
            
            var genericParameters = GetGenericParameters(t);

            if (genericParameters.Length == 0)
            {
                descriptors.Add(new ServiceDescriptor(t, t, ServiceLifetime.Scoped));
                continue;
            }

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

    private static ServiceDescriptor[] GetServiceDescriptorsForCtorDependencies(Type t, Type[] types)
    {
        var descriptors = new List<ServiceDescriptor>();
        var ctors = t.GetConstructors();
        var injectCtor = ctors.First(c => c.GetParameters().Length > 0);
        var ctorParameters = injectCtor.GetParameters().Select(p => p.ParameterType).ToArray();

        foreach (var ctorParam in ctorParameters)
        {
            if (ctorParam.IsInterface)
            {
                var implementations = types.Where(tt => tt.GetInterfaces().Any(i => i == ctorParam));
                var impl =  implementations.First();
                descriptors.Add(new ServiceDescriptor(ctorParam, impl, ServiceLifetime.Scoped));
            }
            else
            {
                descriptors.Add(new ServiceDescriptor(ctorParam, ctorParam, ServiceLifetime.Scoped));
            }
        }
        
        return descriptors.ToArray();
    }

    public static ServiceDescriptor[] GetServiceDescriptorsForHandler<TRequest, TResponse, THandler>()
        => GetServiceDescriptorsForHandler(typeof(TRequest), typeof(TResponse), typeof(THandler));
    
    public static ServiceDescriptor[] GetServiceDescriptorsForHandler(Type request, Type response, Type handler)
    {
        var genericPipeInterface = typeof(IPipe<,>);
        var genericPipeImpl = typeof(DefaultPipe<,>);
        var genericHandlerStage = typeof(HandleRequestStage<,>);
        var genericPreStage = typeof(DebugPreExecutionStage<,>);
        var genericPostStage = typeof(DebugPostExecutionStage<,>);
        var genericHandlerInterface = typeof(IRequestHandler<,>);
        
        var pipeInterface = genericPipeInterface.MakeGenericType(request, response);
        var pipeImpl = genericPipeImpl.MakeGenericType(request, response);
        var handlerStage = genericHandlerStage.MakeGenericType(request, response);
        var preStage = genericPreStage.MakeGenericType(request, response);
        var postStage = genericPostStage.MakeGenericType(request, response);
        var handlerInterface = genericHandlerInterface.MakeGenericType(request, response);
        
        var pipeDescriptor = new ServiceDescriptor(pipeInterface, pipeImpl, ServiceLifetime.Scoped);
        var handlerStageDescriptor = new ServiceDescriptor(handlerStage, handlerStage, ServiceLifetime.Scoped);
        var preStageDescriptor = new  ServiceDescriptor(preStage, preStage, ServiceLifetime.Scoped);
        var postStageDescriptor = new ServiceDescriptor(postStage, postStage, ServiceLifetime.Scoped);
        var handlerDescriptor =  new ServiceDescriptor(handlerInterface, handler, ServiceLifetime.Scoped);
        
        return [pipeDescriptor, handlerStageDescriptor, preStageDescriptor, postStageDescriptor, handlerDescriptor];
    }
}