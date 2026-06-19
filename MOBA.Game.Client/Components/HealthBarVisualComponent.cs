using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Graphics.Abstractions;
using MOBA.Game.Actors;
using MOBA.Game.Components;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Components;

/// <summary>
/// Client-only HP bar drawn as a camera-facing billboard above an actor. Fill
/// ratio derives from the actor's <see cref="HealthComponent"/>; fill colour
/// is read on demand from the actor's <see cref="TeamComponent"/> compared
/// against the local player's team (scanned from the scene). Own team = green,
/// enemy = red, neutral / no local player yet = team-coloured fallback.
/// </summary>
public sealed class HealthBarVisualComponent : Component, IBillboardRenderable
{
    private static readonly Vector3D<float> AllyColor = new(0.25f, 0.85f, 0.30f);
    private static readonly Vector3D<float> EnemyColor = new(0.90f, 0.20f, 0.20f);
    private static readonly Vector3D<float> NeutralColor = new(1.0f, 0.85f, 0.20f);
    private static readonly Vector3D<float> BlueTeamColor = new(0.30f, 0.55f, 0.95f);
    private static readonly Vector3D<float> RedTeamColor = new(0.90f, 0.30f, 0.30f);

    private readonly HealthComponent _health;
    private readonly Scene _scene;

    public HealthBarVisualComponent(
        Actor owner,
        HealthComponent health,
        Scene scene,
        IMesh quad,
        IShader shader,
        Vector3D<float> worldOffset,
        Vector2D<float> sizeWorldUnits)
        : base(owner)
    {
        _health = health;
        _scene = scene;
        Mesh = quad;
        Shader = shader;
        WorldOffset = worldOffset;
        SizeWorldUnits = sizeWorldUnits;
    }

    public IMesh Mesh { get; }

    public IShader Shader { get; }

    public Vector3D<float> WorldOffset { get; }

    public Vector2D<float> SizeWorldUnits { get; }

    public float FillRatio => _health.Ratio;

    public Vector3D<float> FillColor => PickColor(
        Owner.GetComponent<TeamComponent>()?.Team,
        FindLocalTeam());

    public Vector3D<float> BackgroundColor { get; set; } = new(0.12f, 0.12f, 0.14f);

    public Vector3D<float> OutlineColor { get; set; } = new(0f, 0f, 0f);

    public float OutlineWidthFraction { get; set; } = 0.08f;

    public bool IsVisible { get; set; } = true;

    private string? FindLocalTeam()
    {
        foreach (var actor in _scene.Actors)
        {
            if (actor is PlayerActor player
                && player.GetComponent<LocalPlayerInputComponent>() is not null)
            {
                return player.GetComponent<TeamComponent>()?.Team;
            }
        }
        return null;
    }

    private static Vector3D<float> PickColor(string? ownerTeam, string? localTeam)
    {
        if (ownerTeam is null)
        {
            return NeutralColor;
        }
        if (localTeam is null)
        {
            return ownerTeam switch
            {
                "Blue" => BlueTeamColor,
                "Red" => RedTeamColor,
                _ => NeutralColor,
            };
        }
        return string.Equals(ownerTeam, localTeam, StringComparison.Ordinal) ? AllyColor : EnemyColor;
    }
}
