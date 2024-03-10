using System.IdentityModel.Tokens.Jwt;
using InDuckTor.Shared.Security.Context;
using InDuckTor.Shared.Security.Http;
using InDuckTor.Shared.Security.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace InDuckTor.Shared.Security;

// todo add documentation
public static class DependencyRegistration
{
    public static IServiceCollection AddInDuckTorSecurity(this IServiceCollection serviceCollection)
    {
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        return serviceCollection
            .AddScoped<ISecurityContext, SecurityContext>()
            .AddScoped<SecurityContextMiddleware>();
    }

    public static IApplicationBuilder UseInDuckTorSecurity(this IApplicationBuilder builder)
        => builder.UseMiddleware<SecurityContextMiddleware>();

    public static IServiceCollection AddInDuckTorJwt(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection
            .Configure<JwtSettings>(configuration)
            .AddOptions<JwtSettings>()
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience)
                                  && !string.IsNullOrWhiteSpace(settings.Issuer)
                                  && (!string.IsNullOrEmpty(settings.SecretKey) || settings.OmitSignature));

        serviceCollection.TryAddSingleton<ITokenFactory, TokenFactory>();
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
        return serviceCollection;
    }

    /// <param name="serviceCollection"></param>
    /// <param name="configuration">Секция конфигурации для <see cref="JwtSettings"/></param>
    public static IServiceCollection AddInDuckTorAuthentication(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var jwtSettings = configuration.Get<JwtSettings>()
                          ?? throw new ArgumentException("Невозможно извлечь настройки JWT из конфигурации", nameof(configuration));
        serviceCollection.AddInDuckTorJwt(configuration)
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = TokenValidator.CreateTokenValidationParameters(jwtSettings));

        return serviceCollection;
    }
}