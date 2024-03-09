using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text.Json.Serialization;
using InDuckTor.Shared.Utils;

namespace InDuckTor.Shared.Security.Context;

[JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: Unknown)]
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum AccountType
{
    Unknown,
    [EnumMember(Value = "system")] System,
    [EnumMember(Value = "client")] Client,
    [EnumMember(Value = "service")] Service,
}

/// <summary>
/// Контекст пользователя обращающегося к системе
/// </summary>
/// <param name="ClientId">Id системы-клиента которая авторизовала пользователя</param>
/// <param name="AccountType">Тип учётной записи</param>
/// <param name="Permissions">Набор привилегий</param>
/// <param name="Claims">Все полученные данные о контексте пользователя</param>
public record UserContext(
    int Id,
    string Login,
    string? ClientId,
    AccountType AccountType,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<Claim> Claims)
{
    /// <summary>
    /// Создаёт <see cref="UserContext"/> и наполняет <see cref="Claims"/> из переданных аргументов
    /// </summary>
    public static UserContext Create(int id, string login, string? clientId, AccountType accountType, IEnumerable<string> permissions,
        IEnumerable<Claim>? additionalClaims = null)
    {
        var permissionsList = permissions.ToList();
        List<Claim> claims =
        [
            new(InDuckTorClaims.Id, id.ToString()),
            new(InDuckTorClaims.Login, login),
            new(InDuckTorClaims.AccountType, accountType.GetEnumMemberName()),
        ];

        claims.AddRange(permissionsList.Select(permission => new Claim(InDuckTorClaims.Permission, permission)));
        if (clientId != null)
            claims.Add(new Claim(InDuckTorClaims.ClientId, clientId));
        if (additionalClaims != null)
            claims.AddRange(additionalClaims);

        return new UserContext(id, login, clientId, accountType, permissionsList, claims);
    }

    public static bool TryCreateFromClaims(IEnumerable<Claim> claims, [NotNullWhen(true)] out UserContext? userContext)
    {
        var claimsList = claims.ToList();
        var idClaim = claimsList.Find(claim => claim.Type == InDuckTorClaims.Id);
        var login = claimsList.Find(claim => claim.Type == InDuckTorClaims.Login)?.Value;
        if (!int.TryParse(idClaim?.Value, out var id) || login is null)
        {
            userContext = null;
            return false;
        }

        var clientId = claimsList.Find(claim => claim.Type == InDuckTorClaims.ClientId)?.Value;
        var accountType = EnumExtensions.TryParseWithEnumMember<AccountType>(
                              claimsList.Find(claim => claim.Type == InDuckTorClaims.AccountType)?.Value)
                          ?? AccountType.Unknown;
        var permissions = claimsList.Where(claim => claim.Type == InDuckTorClaims.Permission).Select(claim => claim.Value).ToList();

        userContext = new UserContext(id, login, clientId, accountType, permissions, claimsList);
        return true;
    }
}