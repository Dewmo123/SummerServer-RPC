namespace SummerProject.Server.Models.DTOs.Auth;

public sealed class GoogleLoginRequest
{
    public required string IdToken { get; init; }
}

public sealed class DevelopmentLoginRequest;

public sealed class RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}

public sealed class LogoutRequest
{
    public required string RefreshToken { get; init; }
}