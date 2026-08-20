using SummerProject.Server.Models.DTOs.Auth;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Auth;

namespace SummerProject.Server.Controllers.Auth;

internal sealed class LogoutHandler(RefreshTokenService refreshTokenService)
    : IRpcMethodHandler<LogoutRequest, LogoutResponse>
{
    public async ValueTask<LogoutResponse> HandleAsync(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await refreshTokenService.LogoutAsync(request.RefreshToken, cancellationToken);
        return new LogoutResponse(true);
    }
}