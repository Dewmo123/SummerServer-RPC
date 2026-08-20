namespace SummerProject.Server.Models.GameData;

/// <summary>
/// 허용 오차 안에서 정규화된 quaternion 회전값입니다.
/// </summary>
internal readonly record struct NormalizedRotationProto(double X, double Y, double Z, double W);