using System.Text;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Rpc.Contracts;

namespace SummerProject.Server.Bootstrap;

internal static class OptionsRegistration
{
    public static IServiceCollection AddServerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 계약을 위반한 설정이 요청 처리 중 발견되지 않도록 애플리케이션 시작 단계에서 검증한다.
        services
            .AddOptions<JsonRpcOptions>()
            .Bind(configuration.GetSection(JsonRpcOptions.SectionName))
            .Validate(
                options => string.Equals(options.Path, "/rpc", StringComparison.Ordinal),
                "JSON-RPC 경로는 외부 계약에 따라 /rpc여야 합니다.")
            .Validate(options => options.MaxRequestBodyBytes > 0, "JSON-RPC 요청 본문 제한은 1 이상이어야 합니다.")
            .Validate(options => options.MaxBatchSize > 0, "JSON-RPC 배치 제한은 1 이상이어야 합니다.")
            .Validate(options => options.MaxJsonDepth > 0, "JSON 최대 깊이는 1 이상이어야 합니다.")
            .ValidateOnStart();

        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Path), "SQLite DB 경로는 비어 있을 수 없습니다.")
            .Validate(options => options.BusyTimeoutMilliseconds >= 0, "SQLite busy timeout은 0 이상이어야 합니다.")
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT 발급자는 비어 있을 수 없습니다.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT 대상은 비어 있을 수 없습니다.")
            .Validate(
                options => options.SigningKey is not null
                    && Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                "JWT 서명 키가 누락되었거나 32바이트보다 짧습니다.")
            .Validate(
                options => options.AccessTokenMinutes is >= 1 and <= 1_440,
                "액세스 토큰 수명은 1분 이상 1440분 이하여야 합니다.")
            .Validate(options => options.ClockSkewSeconds >= 0, "JWT 허용 시계 오차는 0초 이상이어야 합니다.")
            .ValidateOnStart();

        services
            .AddOptions<RefreshTokenOptions>()
            .Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
            .Validate(
                options => options.LifetimeDays is >= 1 and <= 365,
                "리프레시 토큰 수명은 1일 이상 365일 이하여야 합니다.")
            .ValidateOnStart();

        services
            .AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            // Google 로그인이 구현되기 전에는 빈 목록을 허용하되, 제공된 값은 모두 유효해야 한다.
            .Validate(
                options => options.ClientIds is not null
                    && options.ClientIds.All(clientId => !string.IsNullOrWhiteSpace(clientId)),
                "Google Client ID 목록에는 빈 값을 넣을 수 없습니다.")
            .Validate(
                options => options.ClientIds is not null
                    && options.ClientIds.Distinct(StringComparer.Ordinal).Count() == options.ClientIds.Length,
                "Google Client ID 목록에는 중복 값을 넣을 수 없습니다.")
            .ValidateOnStart();

        return services;
    }
}