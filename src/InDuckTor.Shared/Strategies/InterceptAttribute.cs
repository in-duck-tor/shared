namespace InDuckTor.Shared.Strategies;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InterceptAttribute : Attribute
{
    public InterceptAttribute(Type interceptorType)
    {
        if (!interceptorType.TryGetGenericInterfaceDefinition(typeof(IStrategyInterceptor<,>), out var interceptorInterface))
            throw new ArgumentException($"Тип декоратора не реализует {typeof(IStrategyInterceptor<,>).Name}", nameof(interceptorType));

        InterceptorType = interceptorType;
        InterceptorInterfaceType = interceptorInterface;
        InterceptorInputType = interceptorInterface.GetGenericArguments()[0];
        InterceptorOutputType = interceptorInterface.GetGenericArguments()[1];
    }

    public Type InterceptorType { get; }
    public Type InterceptorInterfaceType { get; }
    public Type InterceptorInputType { get; }
    public Type InterceptorOutputType { get; }

    // todo
    // public required int Index { get; init; }
}