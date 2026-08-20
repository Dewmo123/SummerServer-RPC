using Microsoft.Data.Sqlite;

using SummerProject.Server.Exceptions.Currencies;
using SummerProject.Server.Exceptions.Users;
using SummerProject.Server.Models.Currencies;
using SummerProject.Server.Repositories.Currencies;

namespace SummerProject.Server.Services.Currencies;

internal sealed class CurrencyBalanceService(CurrencyRepository currencyRepository)
{
    public async ValueTask<CurrencyProto> IncreaseAsync(
        long userId,
        CurrencyTypeProto type,
        long amount,
        CancellationToken cancellationToken)
    {
        Validate(type, amount);
        CurrencyRepositoryResult result = await currencyRepository.IncreaseAsync(
            userId,
            type,
            amount,
            cancellationToken);
        return Map(result);
    }

    public async ValueTask<CurrencyProto> DecreaseAsync(
        long userId,
        CurrencyTypeProto type,
        long amount,
        CancellationToken cancellationToken)
    {
        Validate(type, amount);
        CurrencyRepositoryResult result = await currencyRepository.DecreaseAsync(
            userId,
            type,
            amount,
            cancellationToken);
        return Map(result);
    }

    internal async ValueTask<CurrencyProto> IncreaseInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CurrencyTypeProto type,
        long amount,
        CancellationToken cancellationToken)
    {
        Validate(type, amount);

        // 스테이지 실행 상태와 재화 보상이 분리 커밋되지 않도록 호출자의 트랜잭션을 그대로 사용한다.
        CurrencyRepositoryResult result = await CurrencyRepository.IncreaseInTransactionAsync(
            connection,
            transaction,
            userId,
            type,
            amount,
            cancellationToken);
        return Map(result);
    }

    internal async ValueTask<CurrencyProto> DecreaseInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CurrencyTypeProto type,
        long amount,
        CancellationToken cancellationToken)
    {
        Validate(type, amount);
        CurrencyRepositoryResult result = await CurrencyRepository.DecreaseInTransactionAsync(
            connection,
            transaction,
            userId,
            type,
            amount,
            cancellationToken);
        return Map(result);
    }

    private static CurrencyProto Map(CurrencyRepositoryResult result) =>
        result.Status switch
        {
            CurrencyRepositoryStatus.Succeeded =>
                new CurrencyProto(result.Currency!.Type, result.Currency.Amount),
            CurrencyRepositoryStatus.UserNotFound => throw new UserNotFoundException(),
            CurrencyRepositoryStatus.CurrencyNotFound =>
                throw new InvalidOperationException("재화 갱신 대상을 찾을 수 없습니다."),
            CurrencyRepositoryStatus.Insufficient => throw new CurrencyInsufficientException(),
            CurrencyRepositoryStatus.Overflow => throw new CurrencyOverflowException(),
            _ => throw new InvalidOperationException("알 수 없는 재화 갱신 결과입니다.")
        };

    private static void Validate(CurrencyTypeProto type, long amount)
    {
        CurrencyQueryService.ValidateType(type);
        if (amount <= 0)
        {
            throw new CurrencyInvalidAmountException();
        }
    }
}