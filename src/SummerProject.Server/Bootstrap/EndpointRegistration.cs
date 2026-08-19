namespace SummerProject.Server.Bootstrap;

internal static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
        return endpoints;
    }
}