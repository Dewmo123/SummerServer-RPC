using System.Collections.Immutable;

using SummerProject.Server.Exceptions.Rooms;
using SummerProject.Server.Models.DTOs.GameData;
using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.Helpers.Rooms;

/// <summary>
/// 외부 함정 Packet을 검증된 위치·회전 Proto로 변환합니다.
/// </summary>
internal sealed class RoomLayoutValidator
{
    internal const int MaximumTrapCount = 100;

    private const double MinimumRotationMagnitudeSquared = 0.98;
    private const double MaximumRotationMagnitudeSquared = 1.02;

    public ImmutableArray<TrapProto> Validate(
        MapProto map,
        IReadOnlyList<TrapPacket>? packets)
    {
        if (packets is null || packets.Count > MaximumTrapCount)
        {
            throw new RoomInvalidParamsException();
        }

        ImmutableArray<TrapProto>.Builder traps =
            ImmutableArray.CreateBuilder<TrapProto>(packets.Count);
        HashSet<GridPositionProto> positions = [];

        foreach (TrapPacket? packet in packets)
        {
            if (packet is null || packet.Position is null || packet.Rotation is null)
            {
                throw new RoomInvalidParamsException();
            }

            if (!Enum.IsDefined(packet.Type))
            {
                throw new TrapTypeUnsupportedException();
            }

            PositionPacket positionPacket = packet.Position;
            if (positionPacket.X < 0
                || positionPacket.X >= map.Width
                || positionPacket.Y < 0
                || positionPacket.Y >= map.Height
                || positionPacket.Z != 0)
            {
                throw new TrapOutOfBoundsException();
            }

            GridPositionProto position = new(
                positionPacket.X,
                positionPacket.Y,
                positionPacket.Z);
            if (!positions.Add(position))
            {
                throw new TrapPositionDuplicatedException();
            }

            RotationPacket rotation = packet.Rotation;
            double magnitudeSquared = (rotation.X * rotation.X)
                + (rotation.Y * rotation.Y)
                + (rotation.Z * rotation.Z)
                + (rotation.W * rotation.W);
            if (!double.IsFinite(magnitudeSquared)
                || magnitudeSquared < MinimumRotationMagnitudeSquared
                || magnitudeSquared > MaximumRotationMagnitudeSquared)
            {
                throw new TrapRotationInvalidException();
            }

            traps.Add(new TrapProto(
                packet.Type,
                position,
                new NormalizedRotationProto(
                    rotation.X,
                    rotation.Y,
                    rotation.Z,
                    rotation.W)));
        }

        return traps.MoveToImmutable();
    }
}