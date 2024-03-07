namespace InDuckTor.Shared.Security.Jwt;

public class JwtSettings
{
    /// <summary>
    /// Пропустить проверку подписи 
    /// </summary>
    public bool OmitSignature { get; set; }
    public string? SecretKey { get; set; }

    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;
}