using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Graphics.Abstractions;
using MOBA.Engine.Graphics.Animations;
using MOBA.Engine.Graphics.Rendering;
using MOBA.Game.Components;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Components;

/// <summary>
/// Skeletal-mesh render component (client-only). Mirrors Madhav <i>Game
/// Programming in C++</i> ch.12 <c>SkeletalMeshComponent</c> + the
/// <c>FollowActor.mMoving</c> idle/walk state machine, with two MOBA-specific
/// twists:
/// <list type="bullet">
///   <item>The "is moving" decision is derived from observed position changes,
///         not from input, because input is owned by the server in our setup.</item>
///   <item>When the owner's <see cref="HealthComponent"/> drops to zero, the
///         component locks into a death state and plays the model's "death" /
///         "defeated" clip once, freezing on the final frame instead of
///         looping. The clip-selection state machine lives entirely inside
///         this component (single Madhav-style animation component); no
///         separate <c>DeathAnimationComponent</c> hangs off the actor.</item>
/// </list>
///
/// <para>
/// Each tick: figure out which clip the actor should be playing (death > move
/// > idle), swap and reset the playhead on a clip change, advance
/// <see cref="_animTime"/> by <c>dt × playRate</c>, wrap at duration for
/// looping clips or clamp at duration for one-shots, and recompute the
/// matrix palette via <see cref="Animation.GetGlobalPoseAtTime"/>.
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
    private readonly Animation? _deathClip;
    private float _animTime;
    private AnimState _state = AnimState.Idle;
    private bool _currentClipLoops = true;
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

        // Parts may mix skinned and static primitives (e.g. a weapon part not
        // bound to bones). The renderer's pass split filters per-part: skinned
        // parts go through pass 2 with this component's palette, static ones
        // fall through to pass 1 as plain draws.
        _model = model;
        Parts = model.Parts;
        Palette = new MatrixPalette();

        // Pick idle = longest, locomotion = shortest non-idle. Tripo / Mixamo exports
        // name clips uninformatively (NlaTrack / NlaTrack.001 …); the heuristic
        // also tries explicit name hints first (Mixamo-style "idle" / "run" / "walk").
        // Death clip is optional — models without one fall back to a frozen idle pose.
        (_idleClip, _moveClip) = PickIdleAndMoveClips(model.Animations);
        _deathClip = PickDeathClip(model.Animations);
        CurrentClip = _idleClip;
        _lastPosition = owner.Transform.Position;
        PlayClip(_idleClip, looping: true);
    }

    public IReadOnlyList<ModelPart> Parts { get; }

    /// <summary>
    /// Mutable visibility flag — the renderer skips this component's parts when false.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    public MatrixPalette Palette { get; }

    public Animation CurrentClip { get; private set; }

    public float PlayRate { get; set; } = 1f;

    /// <summary>
    /// True once the playhead reaches the duration of a non-looping clip
    /// (the death / defeated freeze frame). Looping clips never report
    /// finished — they just wrap. External observers (e.g. a future "fade
    /// out on death-finished" effect) can poll this without subscribing
    /// to any events.
    /// </summary>
    public bool IsCurrentClipFinished => !_currentClipLoops && _animTime >= CurrentClip.Duration;

    public override void OnUpdate(GameTime time)
    {
        var desired = DesiredState(time.DeltaSeconds);
        if (desired != _state)
        {
            _state = desired;
            switch (desired)
            {
                case AnimState.Death:
                    PlayClip(_deathClip ?? _idleClip, looping: false);
                    break;
                case AnimState.Move:
                    PlayClip(_moveClip, looping: true);
                    break;
                default:
                    PlayClip(_idleClip, looping: true);
                    break;
            }
        }

        _animTime += time.DeltaSeconds * PlayRate;
        if (_currentClipLoops)
        {
            while (_animTime > CurrentClip.Duration)
            {
                _animTime -= CurrentClip.Duration;
            }
        }
        else
        {
            // Clamp at the final frame so the corpse freezes in pose instead
            // of jumping back to the start.
            if (_animTime > CurrentClip.Duration)
            {
                _animTime = CurrentClip.Duration;
            }
        }

        ComputeMatrixPalette();
    }

    /// <summary>
    /// Picks the clip the actor should be playing this tick. Death is sticky
    /// — once entered we never leave it. Otherwise the movement state machine
    /// produces idle / move based on observed position changes with a small
    /// hold window so the 30 Hz server snapshot cadence doesn't flicker the
    /// state every frame between snapshots.
    /// </summary>
    private AnimState DesiredState(float deltaSeconds)
    {
        if (_state == AnimState.Death)
        {
            return AnimState.Death;
        }

        if (Owner.GetComponent<HealthComponent>() is { Current: <= 0f })
        {
            return AnimState.Death;
        }

        var currentPos = Owner.Transform.Position;
        var posChanged = (currentPos - _lastPosition).Length > MovementThreshold;
        _lastPosition = currentPos;

        if (posChanged)
        {
            _timeSinceLastMovement = 0f;
        }
        else
        {
            _timeSinceLastMovement += deltaSeconds;
        }

        return _timeSinceLastMovement < MovementWindowSeconds ? AnimState.Move : AnimState.Idle;
    }

    private void PlayClip(Animation clip, bool looping)
    {
        CurrentClip = clip;
        _currentClipLoops = looping;
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

    /// <summary>
    /// Looks for a death-state clip by name. Minion GLBs ship a "death" clip,
    /// the knight ships "defeated"; a stray "dying" / "die" is accepted too.
    /// Returns null when nothing matches — the death-state path then freezes
    /// on the idle pose instead.
    /// </summary>
    private static Animation? PickDeathClip(IReadOnlyDictionary<string, Animation> animations) =>
        FindByNameContains(animations, "death")
        ?? FindByNameContains(animations, "defeated")
        ?? FindByNameContains(animations, "dying")
        ?? FindByNameContains(animations, "die");

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

    private enum AnimState
    {
        Idle,
        Move,
        Death,
    }
}
