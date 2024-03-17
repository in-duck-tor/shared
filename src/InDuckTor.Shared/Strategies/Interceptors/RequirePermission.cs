using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentResults;
using InDuckTor.Shared.Models;
using InDuckTor.Shared.Security.Context;

namespace InDuckTor.Shared.Strategies.Interceptors;

public interface IRequirePermissionData
{
    public string ErrorMessage { get; }
    public string PermissionId { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IRequirePermissionData
{
    public const string DefaultErrorMessage = "Недостаточно прав";
    public string ErrorMessage { get; init; } = DefaultErrorMessage;
    public bool Throws { get; init; } = false;
    public string PermissionId { get; }
    public RequirePermissionAttribute(string permissionId) => PermissionId = permissionId;
}

public interface IOverridePrivilegeData
{
    public string PermissionId { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class OverridePrivilegeAttribute : Attribute, IOverridePrivilegeData
{
    public OverridePrivilegeAttribute(string permissionId) => PermissionId = permissionId;
    public string PermissionId { get; }
}

internal static class RequirePermissionInterceptorRegistry
{
    internal readonly record struct InterceptorContext(IReadOnlyList<IRequirePermissionData> RequiredPermissions, IReadOnlyList<IOverridePrivilegeData> OverridePrivileges);

    private static readonly Dictionary<Type, InterceptorContext> PermissionsRegistry = new();

    internal static InterceptorContext GetContextFor<TInterceptor>()
        => PermissionsRegistry.TryGetValue(typeof(TInterceptor), out var permissions)
            ? permissions
            : throw new InvalidOperationException("Переданный тип интерсептора не был зарегистрирован");

    /// <param name="strategyType">Тип реализации</param>
    /// <param name="input">Тип входного значения стратегии</param>
    /// <param name="output">Тип выходного значения стратегии</param>
    /// <returns>Множество интерсепторов для данной стратегии</returns>
    internal static IEnumerable<Type> TrySetupForStrategy(Type strategyType, Type input, Type output)
    {
        if (TrySetupThrowingInterceptor(strategyType, input, output, out var throwingInterceptor)) yield return throwingInterceptor;
        if (TrySetupResultInterceptor(strategyType, input, output, out var resultInterceptor)) yield return resultInterceptor;
    }

    private static bool TrySetupThrowingInterceptor(Type strategyType, Type input, Type output, [NotNullWhen(true)] out Type? interceptor)
    {
        var permissionAttributes = strategyType.GetCustomAttributes<RequirePermissionAttribute>()
            .Where(attribute => attribute.Throws)
            .ToArray();

        if (permissionAttributes.Length == 0) return (interceptor = null) != null;
        interceptor = typeof(RequirePermissionsThrowingInterceptor<,,>).MakeGenericType(strategyType, input, output);

        var overridePrivilegeAttributes = strategyType.GetCustomAttributes<OverridePrivilegeAttribute>().ToArray();
        PermissionsRegistry.Add(interceptor, new InterceptorContext(permissionAttributes, overridePrivilegeAttributes));

        return true;
    }

    private static bool TrySetupResultInterceptor(Type strategyType, Type input, Type output, [NotNullWhen(true)] out Type? interceptor)
    {
        var permissionAttributes = strategyType.GetCustomAttributes<RequirePermissionAttribute>()
            .Where(attribute => attribute.Throws == false)
            .ToArray();

        if (permissionAttributes.Length == 0) return (interceptor = null) != null;
        interceptor = typeof(RequirePermissionsInterceptor<,,>).MakeGenericType(strategyType, input, output);

        var overridePrivilegeAttributes = strategyType.GetCustomAttributes<OverridePrivilegeAttribute>().ToArray();
        PermissionsRegistry.Add(interceptor, new InterceptorContext(permissionAttributes, overridePrivilegeAttributes));
        return true;
    }
}

public class RequirePermissionsInterceptor<TTargetStrategy, TInput, TSuccess>(ISecurityContext securityContext)
    : IStrategyInterceptor<TInput, Result<TSuccess>>
    where TTargetStrategy : IStrategy<TInput, Result<TSuccess>>
{
    public Task<Result<TSuccess>> Intercept(TInput input, IStrategy<TInput, Result<TSuccess>>.Delegate next, CancellationToken ct)
    {
        var context = RequirePermissionInterceptorRegistry.GetContextFor<RequirePermissionsInterceptor<TTargetStrategy, TInput, TSuccess>>();
        return context.CheckPermissions(securityContext.Currant, out var errorMessage)
            ? next(input, ct)
            : Task.FromResult<Result<TSuccess>>(new Errors.Forbidden(errorMessage));
    }
}

public class RequirePermissionsThrowingInterceptor<TTargetStrategy, TInput, TOutput>(ISecurityContext securityContext)
    : IStrategyInterceptor<TInput, TOutput>
    where TTargetStrategy : IStrategy<TInput, TOutput>
{
    public Task<TOutput> Intercept(TInput input, IStrategy<TInput, TOutput>.Delegate next, CancellationToken ct)
    {
        var context = RequirePermissionInterceptorRegistry.GetContextFor<RequirePermissionsThrowingInterceptor<TTargetStrategy, TInput, TOutput>>();
        return context.CheckPermissions(securityContext.Currant, out var errorMessage)
            ? next(input, ct)
            : throw new ForbiddenException(errorMessage);
    }
}

internal static class RequirePermissionInterceptorContextExtensions
{
    internal static bool CheckPermissions(this RequirePermissionInterceptorRegistry.InterceptorContext context, UserContext userContext, [NotNullWhen(false)] out string? errorMessage)
    {
        var permissions = userContext.Permissions;
        errorMessage = context.RequiredPermissions
            .FirstOrDefault(data => !permissions.Contains(data.PermissionId))
            ?.ErrorMessage;

        var hasOverridePrivilege = context.OverridePrivileges.Any(data => permissions.Contains(data.PermissionId));

        return errorMessage is not null || hasOverridePrivilege;
    }
}