namespace SummerProject.Server.Bootstrap;

internal static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
        endpoints.MapPost("/rpc", JsonRpcEndpoint.HandleAsync);
        return endpoints;
    }
}