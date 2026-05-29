using MOBA.Engine.Core;
using MOBA.Engine.Graphics;
using Silk.NET.Maths;

namespace MOBA.Game.Client;

/// <summary>
/// Skeletal-mesh render component (client-only). Mirrors Madhav <i>Game
/// Programming in C++</i> ch.12 <c>SkeletalMeshComponent</c> + the
/// <c>FollowActor.mMoving</c> idle/walk state machine, with one MOBA-specific
/// twist: the "is moving" decision is derived from observed position changes,
/// not from input, because input is owned by the server in our setup.
///
/// <para>
/// Each tick: advance <see cref="_animTime"/> by <c>dt × playRate</c>, wrap at
/// the clip's duration, and recompute the matrix palette via
/// <see cref="Animation.GetGlobalPoseAtTime"/>. When the position delta crosses
/// the movement threshold, snap to the walk clip; when motion stops, snap back
/// to idle. No crossfade for the first cut.
/// </para>
/// </summary>
public sealed class SkeletalMeshRendererComponent : Component, ISkinnedRenderable
{
    private const float MovementThreshold = 0.001f;

    // The state machine sticks around for a window after the last observed
    // movement so the client's 60 Hz render frames don't flicker between
    // walking and idle in the gaps between 30 Hz server snapshots. 150 ms
    // covers ~4–5 snapshot intervals — enough to survive jitter, short
    // enough to feel responsive when the character actually stops.
    private const float MovementWindowSeconds = 0.15f;

    private readonly Model _model;
    private readonly Animation _idleClip;
    private readonly Animation _moveClip;
    private float _animTime;
    private bool _isMoving;
    private float _timeSinceLastMovement = float.PositiveInfinity;
    private Vector3D<float> _lastPosition;

    public SkeletalMeshRendererComponent(Actor owner, Model model) : base(owner)
    {
        if (model.Skeleton is null)
        {
            throw new ArgumentException("Model has no skeleton — use MeshRendererComponent for static meshes.", nameof(model));
        }
        if (model.Animations.Count < 2)
        {
            throw new ArgumentException(
                "Skeletal model needs at least two animation clips (idle + walk).",
                nameof(model));
        }
        if (model.Mesh is not ISkinnedMesh)
        {
            throw new ArgumentException("Model's mesh is not skinned.", nameof(model));
        }

        _model = model;
        Mesh = model.Mesh;
        Material = model.Material;
        Palette = new MatrixPalette();

        // Pick idle = longest, locomotion = shortest non-idle. Tripo / Mixamo exports
        // name clips uninformatively (NlaTrack / NlaTrack.001 …); the heuristic
        // also tries explicit name hints first (Mixamo-style "idle" / "run" / "walk").
        (_idleClip, _moveClip) = PickIdleAndMoveClips(model.Animations);
        CurrentClip = _idleClip;
        _lastPosition = owner.Transform.Position;
        PlayClip(_idleClip);
    }

    public IMesh Mesh { get; }

    public Material Material { get; }

    public MatrixPalette Palette { get; }

    public Animation CurrentClip { get; private set; }

    public float PlayRate { get; set; } = 1f;

    public override void OnUpdate(GameTime time)
    {
        // 1. Movement-state machine. Don't trust the per-frame position-changed
        // check directly — server snapshots arrive at 30 Hz while we render at
        // 60 Hz, so in the gap frames the position is "unchanged" and the
        // state would flicker. Use a decaying "time since last observed
        // movement" instead; treat as walking if that's recent enough.
        var currentPos = Owner.Transform.Position;
        var posChanged = (currentPos - _lastPosition).Length > MovementThreshold;
        _lastPosition = currentPos;

        if (posChanged)
        {
            _timeSinceLastMovement = 0f;
        }
        else
        {
            _timeSinceLastMovement += time.DeltaSeconds;
        }

        var nowMoving = _timeSinceLastMovement < MovementWindowSeconds;
        if (nowMoving != _isMoving)
        {
            _isMoving = nowMoving;
            PlayClip(nowMoving ? _moveClip : _idleClip);
        }

        // 2. Advance animation time, wrap at duration, recompute palette.
        _animTime += time.DeltaSeconds * PlayRate;
        while (_animTime > CurrentClip.Duration)
        {
            _animTime -= CurrentClip.Duration;
        }
        ComputeMatrixPalette();
    }

    private void PlayClip(Animation clip)
    {
        CurrentClip = clip;
        _animTime = 0f;
        ComputeMatrixPalette();
    }

    private void ComputeMatrixPalette()
    {
        var skeleton = _model.Skeleton!;
        Span<Matrix4X4<float>> globalPoses = stackalloc Matrix4X4<float>[MatrixPalette.MaxBones];
        CurrentClip.GetGlobalPoseAtTime(skeleton, _animTime, globalPoses);

        // palette[i] = globalInvBindPose[i] * globalPose[i] (row-vector form,
        // matches Madhav's `mPalette.mEntry[i] = globalInvBindPoses[i] * currentPoses[i]`).
        for (var i = 0; i < skeleton.NumBones; i++)
        {
            Palette.Entries[i] = skeleton.GlobalInverseBindPoses[i] * globalPoses[i];
        }
    }

    private static (Animation idle, Animation move) PickIdleAndMoveClips(
        IReadOnlyDictionary<string, Animation> animations)
    {
        // Idle: prefer a clip whose name contains "idle" (Mixamo convention);
        // otherwise the longest clip — relaxed waiting loops are consistently
        // the longest thing in a Mixamo / Tripo character pack.
        // Move (the clip we play when the character is moving): prefer "run",
        // then "walk", then the shortest non-idle clip. Choosing run-over-walk
        // matches our sim's single high speed; once the sim gets multiple
        // speeds we can keep both clips and pick based on velocity.
        var idleByName = FindByNameContains(animations, "idle");
        var runByName = FindByNameContains(animations, "run");
        var walkByName = FindByNameContains(animations, "walk");

        var sortedByDuration = animations.Values.OrderBy(a => a.Duration).ToList();
        var idle = idleByName ?? sortedByDuration[^1];
        var move = runByName
            ?? walkByName
            ?? sortedByDuration.First(a => a != idle);

        return (idle, move);
    }

    private static Animation? FindByNameContains(
        IReadOnlyDictionary<string, Animation> animations,
        string keyword)
    {
        foreach (var (name, clip) in animations)
        {
            if (name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return clip;
            }
        }
        return null;
    }
}