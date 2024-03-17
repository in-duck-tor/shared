using InDuckTor.Shared.Security.Context;
using Microsoft.AspNetCore.Http;

namespace InDuckTor.Shared.Security.Http;

public class SecurityContextMiddleware : IMiddleware
{
    private readonly ISecurityContext _securityContext;

    public SecurityContextMiddleware(ISecurityContext securityContext)
    {
        _securityContext = securityContext;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!UserContext.TryCreateFromClaims(context.User.Claims, out var userContext))
        {
            await next.Invoke(context);
            return;
        }

        using (_securityContext.Impersonate(userContext))
        {
            await next.Invoke(context);
        }
    }
}