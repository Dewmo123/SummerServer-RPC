using SummerProject.Server.Helpers.Auth;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.Datas.Auth;
using SummerProject.Server.Repositories.Auth;

namespace SummerProject.Server.Services.Auth;

internal sealed class AuthenticationSessionService(
    JwtTokenService jwtTokenService,
    RefreshTokenGenerator refreshTokenGenerator,
    RefreshTokenRepository refreshTokenRepository,
    TimeProvider timeProvider)
{
    public async ValueTask<LoginSessionProto> CreateAsync(
        UserModel user,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        IssuedAccessTokenProto accessToken = jwtTokenService.Issue(user, now);
        IssuedRefreshTokenProto refreshToken = refreshTokenGenerator.CreateNew(now);
        await refreshTokenRepository.InsertAsync(user.Id, refreshToken, cancellationToken);
        return new LoginSessionProto(
            user.Id,
            user.Username,
            new IssuedTokenPairProto(accessToken, refreshToken));
    }
}