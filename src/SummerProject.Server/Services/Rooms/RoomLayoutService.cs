using System.Collections.Immutable;
using System.Text.Json;

using SummerProject.Server.Exceptions.Rooms;
using SummerProject.Server.Exceptions.Users;
using SummerProject.Server.GameData.Catalogs.Maps;
using SummerProject.Server.Helpers.Rooms;
using SummerProject.Server.Models.Datas.Rooms;
using SummerProject.Server.Models.DTOs.GameData;
using SummerProject.Server.Models.GameData;
using SummerProject.Server.Models.Rooms;
using SummerProject.Server.Repositories.Rooms;

namespace SummerProject.Server.Services.Rooms;

/// <summary>
/// 사용자 방 전체 스냅샷의 검증, 저장과 카탈로그 결합 조회를 조정합니다.
/// </summary>
internal sealed class RoomLayoutService(
    MapCatalog mapCatalog,
    RoomLayoutValidator validator,
    RoomTrapSnapshotSerializer snapshotSerializer,
    UserRoomRepository repository,
    TimeProvider timeProvider,
    ILogger<RoomLayoutService> logger)
{
    public async ValueTask<RoomLayoutProto> UpsertAsync(
        long userId,
        long mapId,
        IReadOnlyList<TrapPacket>? trapPackets,
        CancellationToken cancellationToken)
    {
        if (!mapCatalog.TryGet(mapId, out MapProto? map))
        {
            throw new MapNotFoundException();
        }

        // 모든 입력 검증과 직렬화를 DB 쓰기 전에 끝내 실패 요청이 기존 방을 변경하지 않게 한다.
        ImmutableArray<TrapProto> traps = validator.Validate(map, trapPackets);
        string trapsJson = snapshotSerializer.Serialize(traps);
        UserRoomUpsertStatus status = await repository.UpsertAsync(
            userId,
            mapId,
            trapsJson,
            timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
            cancellationToken);
        if (status == UserRoomUpsertStatus.UserNotFound)
        {
            throw new UserNotFoundException();
        }

        return new RoomLayoutProto(userId, map, traps);
    }

    public async ValueTask<RoomLayoutProto> GetAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        UserRoomModel room = await repository.FindByUserIdAsync(userId, cancellationToken)
            ?? throw new RoomNotFoundException();
        if (!mapCatalog.TryGet(room.MapId, out MapProto? map))
        {
            // 배포된 카탈로그가 저장 데이터와 어긋난 사실을 운영자가 추적할 수 있도록 식별자만 남긴다.
            logger.LogError(
                "저장된 사용자 방의 맵 참조가 현재 카탈로그에 없습니다. UserId: {UserId}, MapId: {MapId}",
                room.UserId,
                room.MapId);
            throw new RoomMapInvalidException();
        }

        try
        {
            IReadOnlyList<TrapPacket> packets = snapshotSerializer.Deserialize(room.TrapsJson);
            ImmutableArray<TrapProto> traps = validator.Validate(map, packets);
            return new RoomLayoutProto(room.UserId, map, traps);
        }
        catch (Exception exception) when (exception is JsonException
            or InvalidOperationException
            or RoomInvalidParamsException
            or TrapTypeUnsupportedException
            or TrapOutOfBoundsException
            or TrapPositionDuplicatedException
            or TrapRotationInvalidException)
        {
            // DB 스냅샷 손상은 클라이언트 입력 오류가 아니므로 업무 오류로 오인되지 않게 내부 오류로 승격한다.
            throw new InvalidOperationException("저장된 사용자 방 함정 스냅샷이 유효하지 않습니다.", exception);
        }
    }
}