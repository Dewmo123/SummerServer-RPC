using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Models.Currencies;
using SummerProject.Server.Models.Datas.Currencies;

namespace SummerProject.Server.Repositories.Currencies;

internal enum CurrencyRepositoryStatus
{
    Succeeded,
    UserNotFound,
    CurrencyNotFound,
    Insufficient,
    Overflow
}

internal sealed record CurrencyRepositoryResult(
    CurrencyRepositoryStatus Status,
    CurrencyModel? Currency = null);

internal sealed record CurrencyListRepositoryResult(
    CurrencyRepositoryStatus Status,
    IReadOnlyList<CurrencyModel>? Currencies = null);

internal sealed class CurrencyRepository(SqliteConnectionFactory connectionFactory)
{
    private const string SelectCurrencySql = """
        SELECT user_id AS UserId,
               type AS Type,
               amount AS Amount
        FROM currencies
        WHERE user_id = @UserId AND type = @Type;
        """;

    public async ValueTask<CurrencyRepositoryResult> GetOrCreateAsync(
        long userId,
        CurrencyTypeProto type,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        CurrencyRepositoryResult result = await GetOrCreateInTransactionAsync(
            connection,
            transaction,
            userId,
            type,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async ValueTask<CurrencyListRepositoryResult> ListOrCreateAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        CurrencyListRepositoryResult result = await ListOrCreateInTransactionAsync(
            connection,
            transaction,
            userId,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    internal static async ValueTask<CurrencyListRepositoryResult> ListOrCreateInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CancellationToken cancellationToken)
    {

        // 유일 복합 키와 충돌 무시 삽입으로 동시 최초 조회에도 종류별 행을 하나만 유지한다.
        foreach (CurrencyTypeProto type in Enum.GetValues<CurrencyTypeProto>())
        {
            await InsertMissingAsync(connection, transaction, userId, type, cancellationToken);
        }

        List<CurrencyModel> currencies = (await connection.QueryAsync<CurrencyModel>(
            new CommandDefinition(
                """
                SELECT user_id AS UserId,
                       type AS Type,
                       amount AS Amount
                FROM currencies
                WHERE user_id = @UserId
                ORDER BY type;
                """,
                new { UserId = userId },
                transaction,
                cancellationToken: cancellationToken))).AsList();

        if (currencies.Count == 0)
        {
            bool userExists = await UserExistsAsync(
                connection,
                transaction,
                userId,
                cancellationToken);
            return new CurrencyListRepositoryResult(
                userExists
                    ? CurrencyRepositoryStatus.CurrencyNotFound
                    : CurrencyRepositoryStatus.UserNotFound);
        }

        return new CurrencyListRepositoryResult(CurrencyRepositoryStatus.Succeeded, currencies);
    }

    public async ValueTask<CurrencyRepositoryResult> IncreaseAsync(
        long userId,
        CurrencyTypeProto type,
        long amount,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        CurrencyRepositoryResult result = await IncreaseInTransactionAsync(
            connection,
            transaction,
            userId,
            type,
            amount,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async ValueTask<CurrencyRepositoryResult> DecreaseAsync(
        long userId,
        CurrencyTypeProto type,
        long amount,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        CurrencyRepositoryResult result = await DecreaseInTransactionAsync(
            connection,
            transaction,
            userId,
            type,
            amount,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    internal static async ValueTask<CurrencyRepositoryResult> GetOrCreateInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CurrencyTypeProto type,
        CancellationToken cancellationToken)
    {
        await InsertMissingAsync(connection, transaction, userId, type, cancellationToken);
        CurrencyModel? currency = await FindAsync(
            connection,
            transaction,
            userId,
            type,
            cancellationToken);
        if (currency is not null)
        {
            return new CurrencyRepositoryResult(CurrencyRepositoryStatus.Succeeded, currency);
        }

        bool userExists = await UserExistsAsync(
            connection,
            transaction,
            userId,
            cancellationToken);
        return new CurrencyRepositoryResult(
            userExists
                ? CurrencyRepositoryStatus.CurrencyNotFound
                : CurrencyRepositoryStatus.UserNotFound);
    }

    internal static async ValueTask<CurrencyRepositoryResult> IncreaseInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CurrencyTypeProto type,
        long amount,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        CurrencyRepositoryResult initial = await GetOrCreateInTransactionAsync(
            connection,
            transaction,
            userId,
            type,
            cancellationToken);
        if (initial.Status != CurrencyRepositoryStatus.Succeeded)
        {
            return initial;
        }

        // 범위 조건을 UPDATE에 포함해 조회 이후 경쟁으로 Int64를 넘는 증가가 발생하지 않게 한다.
        int changed = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE currencies
            SET amount = amount + @Amount
            WHERE user_id = @UserId
              AND type = @Type
              AND amount <= @MaximumStartingAmount;
            """,
            new
            {
                UserId = userId,
                Type = type,
                Amount = amount,
                MaximumStartingAmount = long.MaxValue - amount
            },
            transaction,
            cancellationToken: cancellationToken));
        if (changed != 1)
        {
            return new CurrencyRepositoryResult(CurrencyRepositoryStatus.Overflow);
        }

        return new CurrencyRepositoryResult(
            CurrencyRepositoryStatus.Succeeded,
            await FindAsync(connection, transaction, userId, type, cancellationToken));
    }

    internal static async ValueTask<CurrencyRepositoryResult> DecreaseInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CurrencyTypeProto type,
        long amount,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        CurrencyRepositoryResult initial = await GetOrCreateInTransactionAsync(
            connection,
            transaction,
            userId,
            type,
            cancellationToken);
        if (initial.Status != CurrencyRepositoryStatus.Succeeded)
        {
            return initial;
        }

        // 잔액 조건과 차감을 한 문장으로 실행해 동시 요청에도 음수 잔액을 만들지 않는다.
        int changed = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE currencies
            SET amount = amount - @Amount
            WHERE user_id = @UserId
              AND type = @Type
              AND amount >= @Amount;
            """,
            new
            {
                UserId = userId,
                Type = type,
                Amount = amount
            },
            transaction,
            cancellationToken: cancellationToken));
        if (changed != 1)
        {
            return new CurrencyRepositoryResult(CurrencyRepositoryStatus.Insufficient);
        }

        return new CurrencyRepositoryResult(
            CurrencyRepositoryStatus.Succeeded,
            await FindAsync(connection, transaction, userId, type, cancellationToken));
    }

    private static async ValueTask InsertMissingAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CurrencyTypeProto type,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT OR IGNORE INTO currencies (user_id, type, amount)
            SELECT @UserId, @Type, 0
            WHERE EXISTS (SELECT 1 FROM users WHERE id = @UserId);
            """,
            new { UserId = userId, Type = type },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async ValueTask<CurrencyModel?> FindAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CurrencyTypeProto type,
        CancellationToken cancellationToken) =>
        await connection.QuerySingleOrDefaultAsync<CurrencyModel>(new CommandDefinition(
            SelectCurrencySql,
            new { UserId = userId, Type = type },
            transaction,
            cancellationToken: cancellationToken));

    private static async ValueTask<bool> UserExistsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CancellationToken cancellationToken) =>
        await connection.QuerySingleAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM users WHERE id = @UserId);",
            new { UserId = userId },
            transaction,
            cancellationToken: cancellationToken));
}