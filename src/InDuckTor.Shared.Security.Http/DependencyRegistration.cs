using InDuckTor.Shared.Security.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace InDuckTor.Shared.Security.Http;

// todo add documentation
public static class DependencyRegistration
{
    public static IServiceCollection AddInDuckTorSecurity(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddScoped<ISecurityContext, SecurityContext>()
            .AddScoped<SecurityContextMiddleware>();
    }

    public static IApplicationBuilder UseInDuckTorSecurity(this IApplicationBuilder builder)
        => builder.UseMiddleware<SecurityContextMiddleware>();
}