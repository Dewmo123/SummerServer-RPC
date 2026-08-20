using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.DTOs.Characters;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Characters;

namespace SummerProject.Server.Controllers.Characters;

internal sealed class GetMyCharacterHandler(
    CallerContext callerContext,
    CharacterQueryService characterQueryService)
    : IRpcMethodHandler<GetMyCharacterRequest, GetMyCharacterResponse>
{
    public async ValueTask<GetMyCharacterResponse> HandleAsync(
        GetMyCharacterRequest request,
        CancellationToken cancellationToken)
    {
        // 보호 메서드 등록 단계에서 인증을 선검증하므로 검증된 사용자 ID만 서비스에 전달한다.
        CallerProto caller = callerContext.Caller
            ?? throw new InvalidOperationException("인증된 호출자가 없습니다.");
        CharacterPacket character = CharacterPacket.From(
            await characterQueryService.GetMineAsync(caller.UserId, cancellationToken));
        return new GetMyCharacterResponse(character);
    }
}