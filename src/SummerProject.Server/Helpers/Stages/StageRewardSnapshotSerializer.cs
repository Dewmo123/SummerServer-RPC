using System.Text.Json;

using SummerProject.Server.Models.Currencies;
using SummerProject.Server.Models.DTOs.Currencies;

namespace SummerProject.Server.Helpers.Stages;

internal sealed class StageRewardSnapshotSerializer
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public string Serialize(IReadOnlyList<CurrencyProto> currencies)
    {
        // 감사용 스냅샷도 외부 CurrencyPacket 배열과 같은 숫자 코드·camelCase 계약으로 고정한다.
        CurrencyPacket[] packets = currencies.Select(CurrencyPacket.From).ToArray();
        return JsonSerializer.Serialize(packets, _options);
    }
}