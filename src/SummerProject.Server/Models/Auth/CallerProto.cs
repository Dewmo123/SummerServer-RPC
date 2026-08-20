namespace SummerProject.Server.Models.Auth;

/// <summary>
/// HTTP 인증 결과를 업무 Handler가 사용할 수 있는 호출자 정보로 정규화합니다.
/// </summary>
public sealed record CallerProto(
    long UserId,
    string Username,
    LoginProviderProto Provider);