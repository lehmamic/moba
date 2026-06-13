using Silk.NET.Maths;

namespace MOBA.Game.Messages;

/// <summary>
/// Server → Client: the currently-active navmesh path for one networked
/// actor. Sent once per accepted MoveCommand right after the server commits
/// the path on the authoritative <c>MoveTargetComponent</c>. Clients write
/// the snapshot into <c>ReplicatedPathComponent</c> on the matching actor —
/// it powers the F3 debug overlay and is read by future minimap UI. The
/// waypoint count is capped by <c>NavMesh.MaxStraightPath</c> (256), so a
/// ushort length prefix is enough.
/// </summary>
public readonly record struct MovePathMessage(uint NetworkId, Vector3D<float>[] Waypoints)
{
    public byte[] Serialize()
    {
        using var stream = new MemoryStream(1 + 4 + 2 + Waypoints.Length * 12);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)MessageType.MovePath);
        writer.Write(NetworkId);
        writer.Write((ushort)Waypoints.Length);
        foreach (var w in Waypoints)
        {
            writer.Write(w.X);
            writer.Write(w.Y);
            writer.Write(w.Z);
        }
        return stream.ToArray();
    }

    public static MovePathMessage ReadPayload(BinaryReader reader)
    {
        var id = reader.ReadUInt32();
        var count = reader.ReadUInt16();
        var waypoints = new Vector3D<float>[count];
        for (var i = 0; i < count; i++)
        {
            waypoints[i] = new Vector3D<float>(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());
        }
        return new MovePathMessage(id, waypoints);
    }
}
