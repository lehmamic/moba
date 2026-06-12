using MOBA.Engine.Core;

namespace MOBA.Game;

/// <summary>
/// Static mob / monster actor (jungle camp, buff mob, minion-spawn template)
/// instantiated from the map's <see cref="Monster"/> list. Same plumbing as
/// <see cref="BuildingActor"/>; kept distinct so future game systems can pivot
/// on the role (e.g. respawn timers apply to monsters, HP / destructible to
/// buildings).
/// </summary>
public sealed class MonsterActor : Actor
{
    public MonsterActor(Monster definition)
    {
        Definition = definition;
        Transform.Position = definition.Position;
        Transform.Rotation = definition.Rotation;
        Transform.Scale = definition.Scale;
        _ = new TransformComponent(this);
    }

    public Monster Definition { get; }
}
