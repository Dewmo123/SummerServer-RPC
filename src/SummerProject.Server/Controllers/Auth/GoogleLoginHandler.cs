using SummerProject.Server.Models.DTOs.Auth;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Auth;

namespace SummerProject.Server.Controllers.Auth;

internal sealed class GoogleLoginHandler(GoogleLoginService loginService)
    : IRpcMethodHandler<GoogleLoginRequest, GoogleLoginResponse>
{
    public async ValueTask<GoogleLoginResponse> HandleAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        Models.Auth.LoginSessionProto session = await loginService.LoginAsync(
            request.IdToken,
            cancellationToken);
        return new GoogleLoginResponse(
            session.UserId,
            session.Username,
            TokenPairPacket.From(session.Tokens));
    }
}