using MOBA.Engine.Core;
using Silk.NET.Maths;

namespace MOBA.Game;

/// <summary>
/// Movement intent on an actor: where it wants to go, how fast, and what
/// destination marker the server has linked to this target. The per-tick
/// movement physics lives here in <see cref="OnUpdate"/> — the actor's
/// <c>UpdateComponents</c> ticks this component and moves the owner's
/// <see cref="Actor.Transform"/> toward <see cref="Target"/>. Networking
/// (broadcasting position updates, despawning the marker on arrival) is the
/// concern of <c>MovementSystem</c>, which inspects this component's state in
/// its <c>IPostUpdateSystem.OnPostUpdate</c> after the sim has run.
/// </summary>
public sealed class MoveTargetComponent : Component
{
    public const float ArrivalThreshold = 0.1f;

    public MoveTargetComponent(Actor owner, float speed = 10f) : base(owner) =>
        Speed = speed;

    public Vector3D<float>? Target { get; set; }

    public uint? MarkerId { get; set; }

    public float Speed { get; }

    public override void OnUpdate(GameTime time)
    {
        if (Target is not { } target)
        {
            return;
        }

        var position = Owner.Transform.Position;
        var delta = target - position;
        var distance = delta.Length;

        if (distance <= ArrivalThreshold)
        {
            Owner.Transform.Position = target;
            Target = null;
            return;
        }

        var step = Vector3D.Normalize(delta) * (Speed * time.DeltaSeconds);
        Owner.Transform.Position = step.Length >= distance ? target : position + step;
    }
}
