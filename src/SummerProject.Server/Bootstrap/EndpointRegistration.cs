namespace SummerProject.Server.Bootstrap;

internal static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 외부 HTTP 진입점은 상태 확인과 단일 JSON-RPC 경로만 공개한다.
        endpoints.MapHealthChecks("/health");
        endpoints.MapPost("/rpc", JsonRpcEndpoint.HandleAsync);
        return endpoints;
    }
}