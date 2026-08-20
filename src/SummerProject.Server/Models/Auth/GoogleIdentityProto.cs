namespace SummerProject.Server.Models.Auth;

/// <summary>
/// 검증이 끝난 Google ID 토큰에서 로그인에 필요한 식별자만 전달합니다.
/// </summary>
public sealed record GoogleIdentityProto(string Subject);