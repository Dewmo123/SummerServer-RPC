namespace SummerProject.Server.Rpc.Validation;

internal sealed class JsonRpcInvalidParamsException : Exception
{
    public JsonRpcInvalidParamsException(Exception? innerException = null)
        : base("JSON-RPC params를 요청 타입에 바인딩할 수 없습니다.", innerException)
    {
    }
}