using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InDuckTor.Shared.Security.Jwt;

public interface ITokenFactory
{
    ValueTask<string> CreateToken(IEnumerable<Claim> claims, TimeSpan expiration, CancellationToken ct = default);
}

internal sealed class TokenFactory : ITokenFactory, IDisposable
{
    private readonly IOptionsMonitor<JwtSettings> _optionsMonitor;
    private SigningCredentials? _signingCredentials;
    private readonly IDisposable? _onChangeTracker;

    public TokenFactory(IOptionsMonitor<JwtSettings> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        SetupSigningCredentials(optionsMonitor.CurrentValue);
        _onChangeTracker = optionsMonitor.OnChange(SetupSigningCredentials);

        return;

        void SetupSigningCredentials(JwtSettings settings)
        {
            if (settings.SecretKey is null) return;
            _signingCredentials = CreateSigningCredentials(settings.SecretKey);
        }
    }

    private static SigningCredentials CreateSigningCredentials(string secretKey)
        => new(
            key: new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            algorithm: SecurityAlgorithms.HmacSha512);

    public ValueTask<string> CreateToken(IEnumerable<Claim> claims, TimeSpan expiration, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        var jwtSecurityToken = jwtSecurityTokenHandler.CreateJwtSecurityToken(
            issuer: _optionsMonitor.CurrentValue.Issuer,
            audience: _optionsMonitor.CurrentValue.Audience,
            issuedAt: now,
            expires: now.Add(expiration),
            subject: new ClaimsIdentity(claims),
            signingCredentials: _signingCredentials);

        return ValueTask.FromResult(
            jwtSecurityTokenHandler.WriteToken(jwtSecurityToken));
    }

    public void Dispose()
    {
        _onChangeTracker?.Dispose();
    }
}