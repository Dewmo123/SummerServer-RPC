using Microsoft.Data.Sqlite;

using SummerProject.Server.Exceptions.Characters;
using SummerProject.Server.Exceptions.Users;
using SummerProject.Server.Helpers.Characters;
using SummerProject.Server.Models.Characters;
using SummerProject.Server.Models.Datas.Characters;
using SummerProject.Server.Repositories.Characters;

namespace SummerProject.Server.Services.Characters;

internal sealed class CharacterQueryService(
    CharacterRepository characterRepository,
    CharacterProgressionCalculator progressionCalculator)
{
    public async ValueTask<CharacterProto> GetMineAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        CharacterRepositoryResult result = await characterRepository.GetOrCreateAsync(
            userId,
            cancellationToken);
        return result.Status switch
        {
            CharacterRepositoryStatus.Succeeded => ToProto(result.Character!),
            CharacterRepositoryStatus.UserNotFound => throw new UserNotFoundException(),
            CharacterRepositoryStatus.CharacterNotFound => throw new CharacterNotFoundException(),
            _ => throw new InvalidOperationException("알 수 없는 캐릭터 조회 결과입니다.")
        };
    }

    internal async ValueTask<CharacterProto> GetMineInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CancellationToken cancellationToken)
    {
        // 경험치가 없는 보상도 완료 결과에 캐릭터 상태를 포함하도록 호출자의 트랜잭션에 참여한다.
        CharacterRepositoryResult result = await CharacterRepository.GetOrCreateInTransactionAsync(
            connection,
            transaction,
            userId,
            cancellationToken);
        return result.Status switch
        {
            CharacterRepositoryStatus.Succeeded => ToProto(result.Character!),
            CharacterRepositoryStatus.UserNotFound => throw new UserNotFoundException(),
            CharacterRepositoryStatus.CharacterNotFound => throw new CharacterNotFoundException(),
            _ => throw new InvalidOperationException("알 수 없는 캐릭터 조회 결과입니다.")
        };
    }

    private CharacterProto ToProto(CharacterModel character)
    {
        // 외부 응답에 필요한 다음 레벨 경험치는 단일 성장 규칙에서 계산한다.
        if (!progressionCalculator.TryGetNextLevelRequirement(
                character.Level,
                out long requirement))
        {
            throw new InvalidOperationException("캐릭터 성장 상태가 응답 범위를 벗어났습니다.");
        }

        return new CharacterProto(character.Level, character.Exp, requirement);
    }
}