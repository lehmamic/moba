using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Scenes;

namespace MOBA.Game.Components;

/// <summary>
/// Carries the scene's per-camera initial config. Client-side scene root reads
/// this in its ctor and configures the <c>CameraSwitcher</c>; server-side
/// ignores it. Keeping the raw <see cref="CamerasDefinition"/> on a component lets
/// future systems (e.g. a cinematic-cut animator) read the same defaults
/// without re-parsing the scene JSON.
/// </summary>
public sealed class CamerasComponent : Component
{
    public CamerasComponent(Actor owner, CamerasDefinition definition) : base(owner) => Definition = definition;

    public CamerasDefinition Definition { get; }
}
