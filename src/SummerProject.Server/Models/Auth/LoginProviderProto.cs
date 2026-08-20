namespace SummerProject.Server.Models.Auth;

/// <summary>
/// 사용자 인증 공급자의 DB 저장 코드를 정의합니다.
/// </summary>
public enum LoginProviderProto
{
    Google = 1,
    Facebook = 2,
    Guest = 999
}