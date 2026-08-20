using Microsoft.Extensions.Options;

using SummerProject.Server.GameData.Catalogs;
using SummerProject.Server.GameData.Catalogs.Maps;
using SummerProject.Server.GameData.Catalogs.Stages;
using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Rpc.Contracts;

namespace SummerProject.Server.Bootstrap;

internal static class ServerInitialization
{
    public static async Task InitializeServerAsync(this WebApplication application)
    {
        // 마이그레이션 검증에 실패한 서버가 요청을 받지 않도록 엔드포인트 등록 전에 초기화한다.
        ILogger logger = application.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ServerInitialization));

        try
        {
            ValidateOptions(application.Services);
            MapCatalog mapCatalog = application.Services.GetRequiredService<MapCatalog>();
            StageCatalog stageCatalog = application.Services.GetRequiredService<StageCatalog>();
            logger.LogInformation(
                "정적 카탈로그를 적재했습니다. MapCount: {MapCount}, StageCount: {StageCount}",
                mapCatalog.Count,
                stageCatalog.Count);

            SqliteMigrationRunner migrationRunner =
                application.Services.GetRequiredService<SqliteMigrationRunner>();
            await migrationRunner.RunAsync(CancellationToken.None);
        }
        catch (OptionsValidationException exception)
        {
            // Options 검증 메시지는 비밀값을 포함하지 않으며 운영자가 잘못된 키를 바로 식별해야 한다.
            logger.LogCritical(exception, "서버 설정 검증에 실패했습니다.");
            throw;
        }
        catch (CatalogValidationException exception)
        {
            // 손상된 콘텐츠로 요청을 처리하지 않도록 파일명만 포함한 진단을 남기고 시작을 중단한다.
            logger.LogCritical(exception, "정적 카탈로그 검증에 실패했습니다.");
            throw;
        }
    }

    private static void ValidateOptions(IServiceProvider services)
    {
        // DB 파일을 만들기 전에 모든 설정을 먼저 평가해 잘못된 구성으로 상태를 변경하지 않는다.
        _ = services.GetRequiredService<IOptions<JsonRpcOptions>>().Value;
        _ = services.GetRequiredService<IOptions<CatalogOptions>>().Value;
        _ = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        _ = services.GetRequiredService<IOptions<JwtOptions>>().Value;
        _ = services.GetRequiredService<IOptions<RefreshTokenOptions>>().Value;
        _ = services.GetRequiredService<IOptions<GoogleAuthOptions>>().Value;
    }
}