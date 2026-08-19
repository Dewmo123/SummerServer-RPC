namespace SummerProject.Server.Rpc.Contracts;

internal sealed class JsonRpcOptions
{
    public const string SectionName = "JsonRpc";

    public int MaxRequestBodyBytes { get; set; } = 65_536;

    public int MaxBatchSize { get; set; } = 50;

    public int MaxJsonDepth { get; set; } = 32;
}