using SummerProject.Server.Helpers.Auth;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.Datas.Auth;
using SummerProject.Server.Repositories.Auth;

namespace SummerProject.Server.Services.Auth;

internal sealed class GoogleLoginService(
    IGoogleIdTokenValidator idTokenValidator,
    GoogleUsernameFactory usernameFactory,
    UserRepository userRepository,
    AuthenticationSessionService sessionService,
    TimeProvider timeProvider)
{
    public async ValueTask<LoginSessionProto> LoginAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        GoogleIdentityProto identity = await idTokenValidator.ValidateAsync(idToken, cancellationToken);
        long createdAtUtcMs = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

        foreach (string username in usernameFactory.CreateCandidates(identity.Subject))
        {
            UserModel? user = await userRepository.GetOrCreateGoogleUserAsync(
                identity.Subject,
                username,
                createdAtUtcMs,
                cancellationToken);
            if (user is not null)
            {
                return await sessionService.CreateAsync(user, cancellationToken);
            }
        }

        // 모든 결정적 후보가 충돌한 경우 외부 식별자를 임의의 사용자명으로 노출하지 않는다.
        throw new InvalidOperationException("Google 사용자명을 안전하게 생성할 수 없습니다.");
    }
}