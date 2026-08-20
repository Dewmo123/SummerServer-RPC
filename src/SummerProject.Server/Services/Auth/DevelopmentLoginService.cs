using Microsoft.Extensions.Options;

using SummerProject.Server.Exceptions.Auth;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.Datas.Auth;
using SummerProject.Server.Repositories.Auth;

namespace SummerProject.Server.Services.Auth;

internal sealed class DevelopmentLoginService(
    IOptions<DevelopmentLoginOptions> options,
    UserRepository userRepository,
    AuthenticationSessionService sessionService)
{
    public async ValueTask<LoginSessionProto> LoginAsync(CancellationToken cancellationToken)
    {
        UserModel? user = await userRepository.FindByUsernameAsync(
            options.Value.Username,
            cancellationToken);
        if (user is null)
        {
            throw new DevelopmentUserNotFoundException();
        }

        return await sessionService.CreateAsync(user, cancellationToken);
    }
}