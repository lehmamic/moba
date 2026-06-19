using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Components;
using MOBA.Game.Models;
using Silk.NET.Maths;

namespace MOBA.Game.Actors;

/// <summary>
/// A lane minion. Spawned in waves by <see cref="Components.MinionSpawnerComponent"/>
/// on the server (sim only — movement intent via <c>MoveTargetComponent</c>,
/// replication via <c>ReplicatedSpawnComponent</c>), and re-created on the
/// client by the minion factory with a skeletal renderer. Carries its team name
/// and <see cref="Type"/> so both sides agree on identity and visuals.
/// </summary>
public sealed class MinionActor : Actor
{
    public MinionActor(Vector3D<float> position, string team, MinionType type)
    {
        Team = team;
        Type = type;
        Transform.Position = position;
        _ = new TransformComponent(this);
    }

    public string Team { get; }

    public MinionType Type { get; }
}
