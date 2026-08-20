using SummerProject.Server.GameData.Catalogs;
using SummerProject.Server.GameData.Catalogs.Maps;
using SummerProject.Server.GameData.Catalogs.Stages;
using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Infrastructure.Logging;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Rpc.Serialization;
using SummerProject.Server.Rpc.Validation;

namespace SummerProject.Server.Bootstrap;

internal static class ServiceRegistration
{
    public static IServiceCollection AddServerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("sqlite");
        services.AddServerOptions(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<EmbeddedSqlMigrationSource>();
        services.AddSingleton<SqliteMigrationRunner>();
        services.AddSingleton<JsonCatalogLoader>();
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<JsonCatalogLoader>().LoadMapCatalog());
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<JsonCatalogLoader>().LoadStageCatalog());

        // 상태가 없는 프로토콜 구성 요소는 재사용하고 요청 조정 객체만 요청 범위로 분리한다.
        services.AddSingleton<JsonRpcSerializerOptions>();
        services.AddSingleton<JsonRpcRequestParser>();
        services.AddSingleton<JsonRpcParameterBinder>();
        services.AddSingleton<JsonRpcMethodRegistry>();
        services.AddSingleton<JsonRpcResponseWriter>();
        services.AddSingleton<JsonRpcExceptionMapper>();
        services.AddSingleton<SensitiveLogFilter>();
        services.AddSingleton<JsonRpcLogWriter>();
        services.AddScoped<JsonRpcDispatcher>();
        services.AddScoped<JsonRpcRequestProcessor>();

        return services;
    }
}