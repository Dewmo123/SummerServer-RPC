using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

using SummerProject.Server.Controllers.Auth;
using SummerProject.Server.Controllers.Characters;
using SummerProject.Server.Controllers.Currencies;
using SummerProject.Server.Controllers.Stages;
using SummerProject.Server.GameData.Catalogs;
using SummerProject.Server.GameData.Catalogs.Maps;
using SummerProject.Server.GameData.Catalogs.Stages;
using SummerProject.Server.Helpers.Auth;
using SummerProject.Server.Helpers.Characters;
using SummerProject.Server.Helpers.Stages;
using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Infrastructure.Logging;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.DTOs.Auth;
using SummerProject.Server.Models.DTOs.Characters;
using SummerProject.Server.Models.DTOs.Currencies;
using SummerProject.Server.Models.DTOs.Stages;
using SummerProject.Server.Repositories.Auth;
using SummerProject.Server.Repositories.Characters;
using SummerProject.Server.Repositories.Currencies;
using SummerProject.Server.Repositories.Stages;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Rpc.Serialization;
using SummerProject.Server.Rpc.Validation;
using SummerProject.Server.Services.Auth;
using SummerProject.Server.Services.Characters;
using SummerProject.Server.Services.Currencies;
using SummerProject.Server.Services.Stages;

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
        services.AddScoped<CallerContext>();
        services.AddScoped<JsonRpcDispatcher>();
        services.AddScoped<JsonRpcRequestProcessor>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters =
                    JwtTokenService.CreateValidationParameters(jwtOptions.Value);
            });

        services.AddSingleton<JwtTokenService>();
        services.AddSingleton<RefreshTokenGenerator>();
        services.AddSingleton<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
        services.AddSingleton<GoogleUsernameFactory>();
        services.AddScoped<UserRepository>();
        services.AddScoped<RefreshTokenRepository>();
        services.AddScoped<AuthenticationSessionService>();
        services.AddScoped<GoogleLoginService>();
        services.AddScoped<DevelopmentLoginService>();
        services.AddScoped<RefreshTokenService>();

        services.AddSingleton<CharacterProgressionCalculator>();
        services.AddScoped<CharacterRepository>();
        services.AddScoped<CharacterQueryService>();
        services.AddScoped<CharacterProgressionService>();
        services.AddScoped<CurrencyRepository>();
        services.AddScoped<CurrencyQueryService>();
        services.AddScoped<CurrencyBalanceService>();
        services.AddSingleton<StageRewardSnapshotSerializer>();
        services.AddScoped<StageRunRepository>();
        services.AddScoped<StageCatalogQueryService>();
        services.AddScoped<StageEntryService>();
        services.AddScoped<StageCompletionService>();

        services.AddJsonRpcMethod<GoogleLoginRequest, GoogleLoginResponse, GoogleLoginHandler>(
            "auth.login.google",
            "idToken");
        services.AddJsonRpcMethod<RefreshTokenRequest, RefreshTokenResponse, RefreshTokenHandler>(
            "auth.token.refresh",
            "refreshToken");
        services.AddJsonRpcMethod<LogoutRequest, LogoutResponse, LogoutHandler>(
            "auth.logout",
            "refreshToken");
        services.AddAuthenticatedJsonRpcMethod<
            GetMyCharacterRequest,
            GetMyCharacterResponse,
            GetMyCharacterHandler>("character.getMine");
        services.AddAuthenticatedJsonRpcMethod<
            GetMyCurrencyRequest,
            GetMyCurrencyResponse,
            GetMyCurrencyHandler>("currency.getMine", "type");
        services.AddAuthenticatedJsonRpcMethod<
            ListMyCurrenciesRequest,
            ListMyCurrenciesResponse,
            ListMyCurrenciesHandler>("currency.listMine");
        services.AddJsonRpcMethod<GetStageRequest, GetStageResponse, GetStageHandler>(
            "stage.get",
            "stageId");
        services.AddAuthenticatedJsonRpcMethod<EnterStageRequest, EnterStageResponse, EnterStageHandler>(
            "stage.enter",
            "stageId");
        services.AddAuthenticatedJsonRpcMethod<
            CompleteStageRequest,
            CompleteStageResponse,
            CompleteStageHandler>("stage.complete", "runId");

        // 환경과 명시 옵션을 Registry 생성 시 함께 확인해 비활성 메서드는 조회 목록에서 제외한다.
        services.AddConditionalJsonRpcMethod<
            DevelopmentLoginRequest,
            DevelopmentLoginResponse,
            DevelopmentLoginHandler>(
                "auth.login.development",
                serviceProvider =>
                    serviceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    && serviceProvider.GetRequiredService<IOptions<DevelopmentLoginOptions>>()
                        .Value.Enabled);

        return services;
    }
}