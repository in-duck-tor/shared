using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using InDuckTor.Shared.Strategies.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InDuckTor.Shared.Strategies;

public static class DependencyRegistration
{
    public static IServiceCollection AddStrategiesFrom(this IServiceCollection services, params Assembly[] assemblies)
    {
        var concreteTypes = assemblies.SelectMany(assembly => assembly.ExportedTypes).Where(type => type is { IsClass: true, IsAbstract: false });

        foreach (var type in concreteTypes)
        {
            if (!type.TryGetGenericInterfaceDefinition(typeof(IStrategy<,>), out var strategyInterface)) continue;

            services.RegisterStrategy(type, strategyInterface)
                .RegisterStrategyExecutor(type, strategyInterface, GetInterceptorTypesFor(type, strategyInterface));
        }

        return services;
    }

    private static IServiceCollection RegisterStrategy(this IServiceCollection services, Type implementationType, Type strategyInterface)
    {
        var serviceDescriptors = implementationType
            .GetInterfaces()
            .Where(@interface => @interface.IsAssignableTo(strategyInterface))
            .Select(strategyLikeInterface => ServiceDescriptor.Scoped(strategyLikeInterface, implementationType));

        return services.Add(serviceDescriptors).AddScoped(implementationType);
    }

    private static IEnumerable<Type> GetInterceptorTypesFor(Type strategyType, Type strategyInterfaceType)
    {
        var inputType = strategyInterfaceType.GetGenericArguments()[0];
        var outputType = strategyInterfaceType.GetGenericArguments()[1];

        return GetExplicitInterceptorsTypesFor(strategyType, inputType, outputType)
            .Concat(RequirePermissionInterceptorRegistry.TrySetupForStrategy(strategyType, inputType, outputType));
    }

    private static IEnumerable<Type> GetExplicitInterceptorsTypesFor(Type strategyType, Type inputType, Type outputType)
    {
        var interceptAttributes = strategyType.GetCustomAttributes<InterceptAttribute>().ToList();
        var generalInterceptorType = typeof(IStrategyInterceptor<,>).MakeGenericType(inputType, outputType);

        foreach (var interceptAttribute in interceptAttributes)
        {
            if (interceptAttribute.InterceptorType.IsAssignableTo(generalInterceptorType))
            {
                yield return interceptAttribute.InterceptorType;
                continue;
            }

            if (!inputType.IsAssignableTo(interceptAttribute.InterceptorInputType))
                throw new ArgumentException($"Невозможно применить декоратор с входным параметром {interceptAttribute.InterceptorInputType} к стратегии {strategyType} с входным параметром {inputType}");
            if (outputType != interceptAttribute.InterceptorOutputType)
                throw new ArgumentException($"Невозможно применить декоратор с выходным параметром {interceptAttribute.InterceptorOutputType} к стратегии {strategyType} с выходным параметром {outputType}");

            throw new InvalidOperationException($"Невозможно применить декоратор {interceptAttribute.InterceptorType} к стратегии {strategyType}");
        }
    }

    private static IServiceCollection RegisterStrategyExecutor(this IServiceCollection services, Type strategyType, Type strategyInterface, IEnumerable<Type> interceptorTypes)
    {
        var interceptorTypesArray = interceptorTypes.ToArray();
        services.TryAdd(interceptorTypesArray.Select(type => ServiceDescriptor.Scoped(type, type)));

        var inputType = strategyInterface.GetGenericArguments()[0];
        var outputType = strategyInterface.GetGenericArguments()[1];

        var executorDescriptors = strategyType
            .GetInterfaces()
            .Where(@interface => @interface.IsAssignableTo(strategyInterface))
            .Select(strategyLikeInterface =>
            {
                var executorFactory = CreateExecutorFactory(strategyLikeInterface, inputType, outputType, interceptorTypesArray);
                var executorType = typeof(IExecutor<,,>).MakeGenericType(strategyLikeInterface, inputType, outputType);
                return ServiceDescriptor.Scoped(executorType, executorFactory);
            });

        return services.Add(executorDescriptors);
    }

    internal static bool TryGetGenericInterfaceDefinition(this Type type, Type interfaceDefinition, [NotNullWhen(true)] out Type? interfaceType)
    {
        interfaceType = type.GetInterfaces()
            .FirstOrDefault(@interface => @interface.IsGenericType
                                          && @interface.GetGenericTypeDefinition() == interfaceDefinition);
        return interfaceType is not null;
    }

    private class StrategyExecutor<TStrategy, TInput, TOutput> : IExecutor<TStrategy, TInput, TOutput>
        where TStrategy : IStrategy<TInput, TOutput>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Type[] _interceptorTypes;
        private readonly Lazy<IStrategy<TInput, TOutput>.Delegate> _lazyStrategyDelegate;

        public StrategyExecutor(IServiceProvider serviceProvider, Type[] interceptorTypes)
        {
            _serviceProvider = serviceProvider;
            _interceptorTypes = interceptorTypes;
            _lazyStrategyDelegate = new Lazy<IStrategy<TInput, TOutput>.Delegate>(ComposeStrategyDelegate);
        }

        private IStrategy<TInput, TOutput>.Delegate ComposeStrategyDelegate()
        {
            var strategy = _serviceProvider.GetRequiredService<TStrategy>();
            return _interceptorTypes.Select(_serviceProvider.GetRequiredService)
                .Cast<IStrategyInterceptor<TInput, TOutput>>()
                .Reverse()
                .Aggregate(
                    seed: (IStrategy<TInput, TOutput>.Delegate)strategy.Execute,
                    (next, interceptor) => (input, ct) => interceptor.Intercept(input, next, ct));
        }

        public Task<TOutput> Execute(TInput input, CancellationToken ct)
        {
            return _lazyStrategyDelegate.Value.Invoke(input, ct);
        }
    }

    private static Func<IServiceProvider, object> CreateExecutorFactory(Type strategyServiceType, Type inputType, Type outputType, Type[] interceptorTypes)
    {
        var constructorInfo = typeof(StrategyExecutor<,,>)
            .MakeGenericType(strategyServiceType, inputType, outputType)
            .GetConstructors()
            .First();
        return provider => constructorInfo.Invoke([ provider, interceptorTypes ]);
    }
}