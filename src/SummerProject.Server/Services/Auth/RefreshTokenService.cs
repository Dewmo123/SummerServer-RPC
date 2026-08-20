using SummerProject.Server.Exceptions.Auth;
using SummerProject.Server.Helpers.Auth;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.Datas.Auth;
using SummerProject.Server.Repositories.Auth;

namespace SummerProject.Server.Services.Auth;

internal sealed class RefreshTokenService(
    RefreshTokenGenerator tokenGenerator,
    RefreshTokenRepository tokenRepository,
    JwtTokenService jwtTokenService,
    TimeProvider timeProvider)
{
    public async ValueTask<IssuedTokenPairProto> RotateAsync(
        string rawToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new InvalidRefreshTokenException();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        byte[] hash = tokenGenerator.Hash(rawToken);
        RefreshRotationResult result = await tokenRepository.RotateAsync(
            hash,
            (familyId, absoluteExpiration) =>
                tokenGenerator.CreateReplacement(familyId, now, absoluteExpiration),
            now,
            cancellationToken);

        if (result.Status == RefreshRotationStatus.Invalid)
        {
            throw new InvalidRefreshTokenException();
        }

        if (result.Status == RefreshRotationStatus.Reused)
        {
            throw new RefreshTokenReusedException();
        }

        RefreshTokenModel previous = result.PreviousToken
            ?? throw new InvalidOperationException("회전된 토큰의 사용자 정보가 없습니다.");
        IssuedRefreshTokenProto replacement = result.ReplacementToken
            ?? throw new InvalidOperationException("회전된 리프레시 토큰이 없습니다.");
        IssuedAccessTokenProto accessToken = jwtTokenService.Issue(
            previous.UserId,
            previous.Username,
            previous.Provider,
            now);
        return new IssuedTokenPairProto(accessToken, replacement);
    }

    public async ValueTask LogoutAsync(
        string rawToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return;
        }

        byte[] hash = tokenGenerator.Hash(rawToken);
        await tokenRepository.RevokeFamilyByTokenHashAsync(
            hash,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }
}