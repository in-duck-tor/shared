using System.Linq.Expressions;

namespace InDuckTor.Shared.Utils;

// todo разобрать + переписать

#region Ворованный код

internal class ParameterReplacer : ExpressionVisitor
{
    private readonly IDictionary<ParameterExpression, ParameterExpression> _map;

    public ParameterReplacer(IDictionary<ParameterExpression, ParameterExpression> map)
    {
        _map = map;
    }

    public static Expression ReplaceParameters(IDictionary<ParameterExpression, ParameterExpression> map,
        Expression expression)
    {
        return new ParameterReplacer(map).Visit(expression);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (_map.TryGetValue(node, out var replacement))
        {
            node = replacement;
        }

        return base.VisitParameter(node);
    }
}

/// <summary>
/// Содержит методы расширения для композиции выражений-предикатов 
/// </summary>
public static class PredicateBuilder
{
    /// <summary>
    /// предикат true 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Expression<Func<T, bool>> True<T>() => f => true;

    /// <summary>
    /// предикат false
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Expression<Func<T, bool>> False<T>() => f => false;

    /// <summary>
    /// позволяет построить композизицию двух предикатов 
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <param name="merge"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second,
        Func<Expression, Expression, Expression> merge)
    {
        var map = first.Parameters
            .Select((f, i) => new { f, s = second.Parameters[i] })
            .ToDictionary(p => p.s, p => p.f);
        //replace params in the second lambda expression with params of the first
        var secondBody = ParameterReplacer.ReplaceParameters(map, second.Body);
        //apply composition of expression bodies with first expression params
        return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
    }

    /// <summary>
    /// Логическое И двух выражений-предикатов
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second) =>
        first.Compose(second, Expression.And);

    /// <summary>
    /// Логическое ИЛИ двух выражений-предикатов
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second) =>
        first.Compose(second, Expression.Or);
}

#endregion