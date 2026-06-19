namespace MOBA.Game.Models;

/// <summary>
/// Static <see cref="AggroProfile"/> lookups per actor role. Minions hit
/// anything (priority 5 still fires); towers and nexus skip other structures
/// (priority 5 doesn't apply). Champions don't carry an aggro profile —
/// the human picks their target.
/// </summary>
public static class AggroProfiles
{
    private static readonly AggroProfile Minion =
        new(AcquisitionRange: 4f, DropRange: 5f, TargetMask: TargetKind.All);

    private static readonly AggroProfile Tower =
        new(AcquisitionRange: 6f, DropRange: 7f, TargetMask: TargetKind.Minion | TargetKind.Champion);

    private static readonly AggroProfile Nexus =
        new(AcquisitionRange: 5f, DropRange: 6f, TargetMask: TargetKind.Minion | TargetKind.Champion);

    public static AggroProfile ForMinion(MinionType _) => Minion;

    public static AggroProfile ForBuilding(string type) => type switch
    {
        "Nexus" => Nexus,
        _ => Tower,
    };
}
