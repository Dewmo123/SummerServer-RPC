using SummerProject.Server.Bootstrap;
using SummerProject.Server.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

// 요청을 받기 전에 구조화 로그와 필수 설정 검증을 공통 시작 경로에 등록한다.
builder.Logging.AddServerLogging();
builder.Services.AddServerServices(builder.Configuration);

var app = builder.Build();

await app.InitializeServerAsync();
app.MapServerEndpoints();

app.Run();

public partial class Program;