using Microsoft.Data.Sqlite;

using SummerProject.Server.Exceptions.Currencies;
using SummerProject.Server.Exceptions.Users;
using SummerProject.Server.Models.Currencies;
using SummerProject.Server.Models.Datas.Currencies;
using SummerProject.Server.Repositories.Currencies;

namespace SummerProject.Server.Services.Currencies;

internal sealed class CurrencyQueryService(CurrencyRepository currencyRepository)
{
    public async ValueTask<CurrencyProto> GetMineAsync(
        long userId,
        CurrencyTypeProto type,
        CancellationToken cancellationToken)
    {
        ValidateType(type);
        CurrencyRepositoryResult result = await currencyRepository.GetOrCreateAsync(
            userId,
            type,
            cancellationToken);
        return result.Status switch
        {
            CurrencyRepositoryStatus.Succeeded => ToProto(result.Currency!),
            CurrencyRepositoryStatus.UserNotFound => throw new UserNotFoundException(),
            CurrencyRepositoryStatus.CurrencyNotFound =>
                throw new InvalidOperationException("재화 지연 생성 결과를 찾을 수 없습니다."),
            _ => throw new InvalidOperationException("알 수 없는 재화 조회 결과입니다.")
        };
    }

    public async ValueTask<IReadOnlyList<CurrencyProto>> ListMineAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        CurrencyListRepositoryResult result = await currencyRepository.ListOrCreateAsync(
            userId,
            cancellationToken);
        return MapList(result);
    }

    internal async ValueTask<IReadOnlyList<CurrencyProto>> ListMineInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        CancellationToken cancellationToken)
    {
        // 완료 응답의 전체 재화가 보상과 같은 커밋 시점을 보도록 호출자의 트랜잭션을 공유한다.
        CurrencyListRepositoryResult result = await CurrencyRepository.ListOrCreateInTransactionAsync(
            connection,
            transaction,
            userId,
            cancellationToken);
        return MapList(result);
    }

    private static CurrencyProto ToProto(CurrencyModel currency) =>
        new(currency.Type, currency.Amount);

    private static IReadOnlyList<CurrencyProto> MapList(CurrencyListRepositoryResult result)
    {
        if (result.Status == CurrencyRepositoryStatus.UserNotFound)
        {
            throw new UserNotFoundException();
        }

        if (result.Status != CurrencyRepositoryStatus.Succeeded)
        {
            throw new InvalidOperationException("재화 목록 지연 생성 결과를 찾을 수 없습니다.");
        }

        // 정렬 책임을 서비스에서도 고정해 저장소 구현과 무관하게 외부 계약을 보존한다.
        return result.Currencies!
            .OrderBy(currency => currency.Type)
            .Select(ToProto)
            .ToArray();
    }

    internal static void ValidateType(CurrencyTypeProto type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new CurrencyInvalidTypeException();
        }
    }
}