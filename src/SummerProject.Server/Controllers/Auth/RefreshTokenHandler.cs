using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.DTOs.Auth;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Auth;

namespace SummerProject.Server.Controllers.Auth;

internal sealed class RefreshTokenHandler(RefreshTokenService refreshTokenService)
    : IRpcMethodHandler<RefreshTokenRequest, RefreshTokenResponse>
{
    public async ValueTask<RefreshTokenResponse> HandleAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        IssuedTokenPairProto tokens = await refreshTokenService.RotateAsync(
            request.RefreshToken,
            cancellationToken);
        return new RefreshTokenResponse(TokenPairPacket.From(tokens));
    }
}