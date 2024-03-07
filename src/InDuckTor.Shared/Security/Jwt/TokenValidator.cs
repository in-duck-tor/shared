using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace InDuckTor.Shared.Security.Jwt;

internal static class TokenValidator
{
    internal static TokenValidationParameters CreateTokenValidationParameters(JwtSettings settings)
        => new()
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = !settings.OmitSignature,
            RequireExpirationTime = false,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = settings.OmitSignature ? null : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey!))
        };
}