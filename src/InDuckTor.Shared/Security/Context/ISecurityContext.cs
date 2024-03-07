namespace InDuckTor.Shared.Security.Context;

public interface ISecurityContext
{
    /// <exception cref="InvalidOperationException">Когда <see cref="IsImpersonated"/> == <c>false</c></exception>
    UserContext Currant { get; }

    bool IsImpersonated { get; }
    
    IDisposable Impersonate(UserContext userContext);
}