namespace SummerProject.Server.Exceptions.Auth;

internal sealed class InvalidGoogleTokenException : Exception
{
    public InvalidGoogleTokenException()
    {
    }

    public InvalidGoogleTokenException(Exception innerException)
        : base("Google ID 토큰 검증에 실패했습니다.", innerException)
    {
    }
}