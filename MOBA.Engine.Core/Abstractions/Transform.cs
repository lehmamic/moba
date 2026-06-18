using Silk.NET.Maths;

namespace MOBA.Engine.Core.Abstractions;

/// <summary>
/// Position, rotation, and scale for an <see cref="Actor"/>, plus the composed
/// world matrix. Bundling these on a dedicated class (rather than as flat
/// properties on Actor) reads better at call sites — <c>actor.Transform.Position</c>
/// instead of <c>actor.Position</c> — and gives a natural home for transform
/// helpers (look-at, lerp, parenting) when they arrive.
///
/// <para>
/// <b>Actor bind-pose convention:</b> following Madhav's <i>Game Programming in C++</i>
/// (chapter 12), the actor's bind-pose forward axis is <b>+X</b> (Up = +Y, Right = -Z
/// follows from the right-hand rule). This matches the convention Mixamo / Tripo3D /
/// most game-pipeline character exports use, so imported character glbs land
/// face-aligned without any per-asset rotation correction. Note this is the actor's
/// own body frame — world axes from ADR-001 still apply (+X right, +Y up, -Z forward
/// into scene) for camera, light, level geometry, etc.
/// </para>
/// </summary>
public sealed class Transform
{
    private readonly Actor _owner;

    internal Transform(Actor owner) => _owner = owner;

    public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;

    public Quaternion<float> Rotation { get; set; } = Quaternion<float>.Identity;

    public Vector3D<float> Scale { get; set; } = Vector3D<float>.One;

    /// <summary>Local matrix relative to the parent (or world if there is no parent): <c>S * R * T</c> in row-vector convention.</summary>
    public Matrix4X4<float> Local =>
        Matrix4X4.CreateScale(Scale)
        * Matrix4X4.CreateFromQuaternion(Rotation)
        * Matrix4X4.CreateTranslation(Position);

    /// <summary>
    /// World matrix — folds <see cref="Local"/> through the parent chain
    /// (<c>Local * Parent.World</c>, recursively). Recomputed every read; at our
    /// actor counts (~tens) and shallow hierarchies (~2) this is negligible —
    /// move to a cached-with-dirty-flag scheme only if profiling shows it.
    /// Falls back to <see cref="Local"/> when the actor has no parent.
    /// </summary>
    public Matrix4X4<float> World =>
        _owner.Parent is { } parent ? Local * parent.Transform.World : Local;

    /// <summary>
    /// World-space forward vector — the actor's bind-pose +X axis rotated by
    /// <see cref="Rotation"/>. "Where the actor is looking" in world coordinates.
    /// </summary>
    public Vector3D<float> Forward => Vector3D.Transform(new Vector3D<float>(1f, 0f, 0f), Rotation);

    /// <summary>World-space up vector — bind pose +Y rotated by <see cref="Rotation"/>.</summary>
    public Vector3D<float> Up => Vector3D.Transform(new Vector3D<float>(0f, 1f, 0f), Rotation);

    /// <summary>
    /// World-space right vector — bind pose -Z rotated by <see cref="Rotation"/>.
    /// Right-hand rule with forward = +X, up = +Y gives right = -Z (forward × up).
    /// </summary>
    public Vector3D<float> Right => Vector3D.Transform(new Vector3D<float>(0f, 0f, -1f), Rotation);
}
