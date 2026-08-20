using Microsoft.Data.Sqlite;

using SummerProject.Server.Exceptions.Characters;
using SummerProject.Server.Exceptions.Users;
using SummerProject.Server.Helpers.Characters;
using SummerProject.Server.Models.Characters;
using SummerProject.Server.Models.Datas.Characters;
using SummerProject.Server.Repositories.Characters;

namespace SummerProject.Server.Services.Characters;

internal sealed class CharacterProgressionService(
    CharacterRepository characterRepository,
    CharacterProgressionCalculator progressionCalculator)
{
    public async ValueTask<CharacterProto> AddExperienceAsync(
        long userId,
        long amount,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            throw new CharacterInvalidExperienceException();
        }

        CharacterRepositoryResult result = await characterRepository.MutateAsync(
            userId,
            current => AddExperience(current, amount),
            cancellationToken);
        CharacterModel character = Map(result);

        if (!progressionCalculator.TryGetNextLevelRequirement(
                character.Level,
                out long requirement))
        {
            throw new CharacterInvalidExperienceException();
        }

        return new CharacterProto(character.Level, character.Exp, requirement);
    }

    internal async ValueTask<CharacterProto> AddExperienceInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        long amount,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            throw new CharacterInvalidExperienceException();
        }

        // 스테이지 완료 선점과 보상이 모두 성공하거나 함께 롤백되도록 호출자의 트랜잭션을 유지한다.
        CharacterRepositoryResult result = await CharacterRepository.MutateInTransactionAsync(
            connection,
            transaction,
            userId,
            current => AddExperience(current, amount),
            cancellationToken);
        CharacterModel character = Map(result);
        if (!progressionCalculator.TryGetNextLevelRequirement(
                character.Level,
                out long requirement))
        {
            throw new CharacterInvalidExperienceException();
        }

        return new CharacterProto(character.Level, character.Exp, requirement);
    }

    private CharacterModel AddExperience(CharacterModel current, long amount)
    {
        // 계산 실패 시 갱신 전에 예외를 발생시켜 경험치와 레벨을 함께 롤백한다.
        if (!progressionCalculator.TryAddExperience(current, amount, out CharacterModel? updated))
        {
            throw new CharacterInvalidExperienceException();
        }

        return updated!;
    }

    private static CharacterModel Map(CharacterRepositoryResult result) =>
        result.Status switch
        {
            CharacterRepositoryStatus.Succeeded => result.Character!,
            CharacterRepositoryStatus.UserNotFound => throw new UserNotFoundException(),
            CharacterRepositoryStatus.CharacterNotFound => throw new CharacterNotFoundException(),
            _ => throw new InvalidOperationException("알 수 없는 캐릭터 갱신 결과입니다.")
        };
}