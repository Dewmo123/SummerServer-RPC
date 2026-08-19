namespace SummerProject.Server.Bootstrap;

internal static class ServiceRegistration
{
    public static IServiceCollection AddServerServices(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }
}