namespace MOBA.Game.Models;

/// <summary>
/// Which classes of actor an <c>AggroComponent</c> is allowed to target.
/// Minion aggro carries the full set so priority 5 ("closest enemy structure")
/// can fire; tower / nexus aggro masks <see cref="Building"/> out because
/// turrets don't shoot each other.
/// </summary>
[Flags]
public enum TargetKind : byte
{
    None = 0,
    Minion = 1 << 0,
    Champion = 1 << 1,
    Building = 1 << 2,
    All = Minion | Champion | Building,
}
