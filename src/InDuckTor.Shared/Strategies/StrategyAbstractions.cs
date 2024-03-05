namespace InDuckTor.Shared.Strategies;

public interface IStrategy<in TInput, TOutput>
{
    public delegate Task<TOutput> Delegate(TInput input, CancellationToken ct);

    Task<TOutput> Execute(TInput input, CancellationToken ct);
}

public interface ICommand<in TInput, TOutput> : IStrategy<TInput, TOutput>;

public interface IQuery<in TInput, TOutput> : IStrategy<TInput, TOutput>;

// todo : check how contravariance TInput of IStrategy<in TInput, TOutput> behave in interceptors 
/// <remarks><typeparamref name="TInput"/> инвариантен в отличии от аналогичного параметра в <see cref="IStrategy{TInput,TOutput}"/></remarks> 
public interface IStrategyInterceptor<TInput, TOutput>
{
    Task<TOutput> Intercept(TInput input, IStrategy<TInput, TOutput>.Delegate next, CancellationToken ct);
}

public interface IExecutor<TStrategy, in TInput, TOutput> where TStrategy : IStrategy<TInput, TOutput>
{
    Task<TOutput> Execute(TInput input, CancellationToken ct);
} 