namespace MOBA.Game.Models;

/// <summary>
/// Per-actor aggro tuning: how far we look for targets
/// (<see cref="AcquisitionRange"/>), how far the current target must drift
/// before we drop it (<see cref="DropRange"/>, slightly larger so wiggling on
/// the edge doesn't re-aggro every tick), which actor kinds are valid targets
/// (<see cref="TargetMask"/> — minions hit everything, towers skip
/// other buildings), and how long the "defend ally" memory lasts on a
/// detected enemy champion basic-attack (<see cref="DefendAllyWindow"/>).
/// </summary>
public readonly record struct AggroProfile(
    float AcquisitionRange,
    float DropRange,
    TargetKind TargetMask,
    float DefendAllyWindow = 3f);
