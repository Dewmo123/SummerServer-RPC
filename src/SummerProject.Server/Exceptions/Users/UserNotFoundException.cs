namespace SummerProject.Server.Exceptions.Users;

/// <summary>
/// 인증 정보의 사용자 ID가 현재 DB에 존재하지 않을 때 게임 데이터 생성을 중단합니다.
/// </summary>
internal sealed class UserNotFoundException : Exception;