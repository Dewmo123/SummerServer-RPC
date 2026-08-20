using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Models.Datas.Characters;

namespace SummerProject.Server.Repositories.Characters;

internal enum CharacterRepositoryStatus
{
    Succeeded,
    UserNotFound,
    CharacterNotFound
}

internal sealed record CharacterRepositoryResult(
    CharacterRepositoryStatus Status,
    CharacterModel? Character = null);

internal sealed class CharacterRepository(SqliteConnectionFactory connectionFactory)
{
    private const string SelectCharacterSql = """
        SELECT user_id AS UserId,
               level AS Level,
               exp AS Exp
        FROM characters
        WHERE user_id = @UserId;
        """;

    public async ValueTask<CharacterRepositoryResult> GetOrCreateAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        CharacterRepositoryResult result = await GetOrCreateInTransactionAsync(
            connection,
            transaction,
            userId,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async ValueTask<CharacterRepositoryResult> MutateAsync(
        long userId,
        Func<CharacterModel, CharacterModel> mutation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        CharacterRepositoryResult result = await MutateInTransactionAsync(
            connection,
            transaction,
            userId,
            mutation,
            cancellationToken);
        if (result.Status == CharacterRepositoryStatus.Succeeded)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await transaction.RollbackAsync(cancellationToken);
        }

        return result;
    }

    internal static async ValueTask<CharacterRepositoryResult> GetOrCreateInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT OR IGNORE INTO characters (user_id, level, exp)
            SELECT @UserId, 1, 0
            WHERE EXISTS (SELECT 1 FROM users WHERE id = @UserId);
            """,
            new { UserId = userId },
            transaction,
            cancellationToken: cancellationToken));

        CharacterModel? character = await connection.QuerySingleOrDefaultAsync<CharacterModel>(
            new CommandDefinition(
                SelectCharacterSql,
                new { UserId = userId },
                transaction,
                cancellationToken: cancellationToken));
        if (character is not null)
        {
            return new CharacterRepositoryResult(CharacterRepositoryStatus.Succeeded, character);
        }

        bool userExists = await connection.QuerySingleAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM users WHERE id = @UserId);",
            new { UserId = userId },
            transaction,
            cancellationToken: cancellationToken));
        return new CharacterRepositoryResult(
            userExists
                ? CharacterRepositoryStatus.CharacterNotFound
                : CharacterRepositoryStatus.UserNotFound);
    }

    internal static async ValueTask<int> UpdateInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CharacterModel character,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE characters
            SET level = @Level,
                exp = @Exp
            WHERE user_id = @UserId;
            """,
            character,
            transaction,
            cancellationToken: cancellationToken));

    internal static async ValueTask<CharacterRepositoryResult> MutateInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        Func<CharacterModel, CharacterModel> mutation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        // 지연 생성, 현재 상태 판정과 갱신을 같은 쓰기 트랜잭션에 묶어 경험치 지급 경쟁을 직렬화한다.
        CharacterRepositoryResult currentResult = await GetOrCreateInTransactionAsync(
            connection,
            transaction,
            userId,
            cancellationToken);
        if (currentResult.Status != CharacterRepositoryStatus.Succeeded)
        {
            return currentResult;
        }

        CharacterModel updated = mutation(currentResult.Character!);
        int changed = await UpdateInTransactionAsync(
            connection,
            transaction,
            updated,
            cancellationToken);
        return changed == 1
            ? new CharacterRepositoryResult(CharacterRepositoryStatus.Succeeded, updated)
            : new CharacterRepositoryResult(CharacterRepositoryStatus.CharacterNotFound);
    }
}