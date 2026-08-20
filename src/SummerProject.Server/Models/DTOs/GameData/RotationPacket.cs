namespace SummerProject.Server.Models.DTOs.GameData;

/// <summary>
/// JSON-RPC 응답에서 함정의 quaternion 회전을 표현합니다.
/// </summary>
internal sealed record RotationPacket(double X, double Y, double Z, double W);