using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using SummerProject.Server.Tests.Rpc;

namespace SummerProject.Server.Tests.Infrastructure.Configuration;

public sealed class OptionsValidationTests
{
    // 잘못된 운영 설정이 첫 요청 전에 한국어 진단과 함께 실패하는지 검증한다.
    [Theory]
    [InlineData("JsonRpc:Path", "/other", "JSON-RPC 경로는 외부 계약에 따라 /rpc여야 합니다.")]
    [InlineData("JsonRpc:MaxBatchSize", "0", "JSON-RPC 배치 제한은 1 이상이어야 합니다.")]
    [InlineData("Catalog:RootPath", "", "정적 카탈로그 루트 경로는 비어 있을 수 없습니다.")]
    [InlineData("Database:Path", "", "SQLite DB 경로는 비어 있을 수 없습니다.")]
    [InlineData("Jwt:SigningKey", "", "JWT 서명 키가 누락되었거나 32바이트보다 짧습니다.")]
    [InlineData("Jwt:SigningKey", "short", "JWT 서명 키가 누락되었거나 32바이트보다 짧습니다.")]
    [InlineData("RefreshToken:LifetimeDays", "0", "리프레시 토큰 수명은 1일 이상 365일 이하여야 합니다.")]
    [InlineData("Google:ClientIds:0", " ", "Google Client ID 목록에는 빈 값을 넣을 수 없습니다.")]
    public async Task InvalidOptionsFailStartupWithKoreanOperationalLog(
        string key,
        string value,
        string expectedMessage)
    {
        await using InvalidOptionsApplicationFactory application = new(key, value);

        Exception exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using HttpClient client = application.CreateClient();
            await client.GetAsync("/health");
        });

        string diagnosticText = string.Join(
            Environment.NewLine,
            new[] { exception.ToString() }.Concat(
                application.Logs.Select(log => $"{log.Message} {log.ExceptionMessage}")));
        Assert.Contains(expectedMessage, diagnosticText, StringComparison.Ordinal);
        Assert.Contains(
            application.Logs,
            log => log.ExceptionMessage?.Contains(expectedMessage, StringComparison.Ordinal) is true);
        Assert.False(File.Exists(application.DatabasePath));
    }

    private sealed class InvalidOptionsApplicationFactory(string key, string value)
        : ConfiguredServerApplicationFactory
    {
        internal System.Collections.Concurrent.ConcurrentQueue<JsonRpcTestLogEntry> Logs { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?> { [key] = value });
            });
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new JsonRpcTestLoggerProvider(Logs));
            });
        }
    }
}