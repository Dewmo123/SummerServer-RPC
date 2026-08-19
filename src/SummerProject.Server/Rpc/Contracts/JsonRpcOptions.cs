namespace SummerProject.Server.Rpc.Contracts;

// 외부 RPC 계약과 입력 자원 제한을 구성으로 관리한다.
internal sealed class JsonRpcOptions
{
    public const string SectionName = "JsonRpc";

    public string Path { get; set; } = "/rpc";

    public int MaxRequestBodyBytes { get; set; } = 65_536;

    public int MaxBatchSize { get; set; } = 50;

    public int MaxJsonDepth { get; set; } = 32;
}