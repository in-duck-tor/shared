using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace InDuckTor.Shared.Security.Jwt;

// todo add documentation
public static class DependencyRegistration
{
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
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        var jwtSettings = configuration.Get<JwtSettings>()
                          ?? throw new ArgumentException("Невозможно извлечь настройки JWT из конфигурации", nameof(configuration));
        serviceCollection.AddInDuckTorJwt(configuration)
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = TokenValidator.CreateTokenValidationParameters(jwtSettings));

        return serviceCollection;
    }
}