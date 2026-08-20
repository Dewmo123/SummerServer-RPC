using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.DTOs.Auth;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Auth;

namespace SummerProject.Server.Controllers.Auth;

internal sealed class DevelopmentLoginHandler(DevelopmentLoginService loginService)
    : IRpcMethodHandler<DevelopmentLoginRequest, DevelopmentLoginResponse>
{
    public async ValueTask<DevelopmentLoginResponse> HandleAsync(
        DevelopmentLoginRequest request,
        CancellationToken cancellationToken)
    {
        LoginSessionProto session = await loginService.LoginAsync(cancellationToken);
        return new DevelopmentLoginResponse(
            session.UserId,
            session.Username,
            TokenPairPacket.From(session.Tokens));
    }
}