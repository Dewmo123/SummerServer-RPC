using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using SummerProject.Server.Models.Auth;

namespace SummerProject.Server.Infrastructure.Security;

internal sealed class CallerContext
{
    public CallerProto? Caller { get; private set; }

    public void Initialize(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            Caller = null;
            return;
        }

        string? subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        string? username = principal.FindFirstValue("username");
        string? providerValue = principal.FindFirstValue("provider");
        if (!long.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out long userId)
            || userId <= 0
            || string.IsNullOrWhiteSpace(username)
            || !int.TryParse(providerValue, NumberStyles.None, CultureInfo.InvariantCulture, out int providerCode)
            || !Enum.IsDefined(typeof(LoginProviderProto), providerCode))
        {
            Caller = null;
            return;
        }

        Caller = new CallerProto(userId, username, (LoginProviderProto)providerCode);
    }
}