using ZLogger;
using ZLogger.Formatters;

namespace SummerProject.Server.Infrastructure.Logging;

// 실행 환경에 관계없이 로그 필드와 출력 형식을 동일한 UTC JSON으로 유지한다.
internal static class LoggingRegistration
{
    public static ILoggingBuilder AddServerLogging(this ILoggingBuilder logging)
    {
        // 기본 공급자의 비구조화 출력이 JSON 로그와 섞이지 않도록 교체한다.
        logging.ClearProviders();
        logging.AddZLoggerConsole(options =>
        {
            options.UseJsonFormatter(formatter =>
            {
                formatter.UseUtcTimestamp = true;
                formatter.IncludeProperties =
                    IncludeProperties.Timestamp
                    | IncludeProperties.LogLevel
                    | IncludeProperties.CategoryName
                    | IncludeProperties.Message
                    | IncludeProperties.Exception
                    | IncludeProperties.ParameterKeyValues;
            });
        });

        return logging;
    }
}