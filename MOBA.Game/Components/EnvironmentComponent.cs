using MOBA.Engine.Core;
using MOBA.Game.Scenes;

namespace MOBA.Game.Components;

/// <summary>
/// Carries the scene's environment config (clear color, directional light) as
/// a sim-side data carrier — actual rendering side-effects (setting
/// <c>Renderer.ClearColor</c>, building the <c>DirectionalLight</c> struct)
/// happen on the client when its scene-root reads this component. Storing the
/// raw <see cref="EnvironmentDefinition"/> here means MOBA.Game stays render-free.
/// </summary>
public sealed class EnvironmentComponent : Component
{
    public EnvironmentComponent(Actor owner, EnvironmentDefinition definition) : base(owner) => Definition = definition;

    public EnvironmentDefinition Definition { get; }
}
