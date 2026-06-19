namespace MOBA.Game.Models;

/// <summary>
/// The three lane-minion roles. Travels on the wire as the
/// <c>ActorSpawnMessage.Variant</c> byte, so the values are stable.
/// </summary>
public enum MinionType : byte
{
    Melee = 0,
    Caster = 1,
    Siege = 2,
}
