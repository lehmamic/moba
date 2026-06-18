using MOBA.Engine.Core.Assets;
using MOBA.Engine.Core.World;
using MOBA.Game.Scenes;

namespace MOBA.Game.Factories;

/// <summary>
/// One factory per actor type. Server registers sim-only factories; client
/// registers client-side factories that produce the corresponding render
/// subclass (e.g. <c>ClientBuildingActor</c> instead of <c>BuildingActor</c>).
/// Discriminator is the string <see cref="TypeName"/>, matched against
/// <see cref="ActorEntryDefinition.Type"/> in the scene JSON.
///
/// <para>
/// Each factory holds its dependencies via the constructor (e.g. an
/// <see cref="AssetManager"/> for client factories that need to load meshes
/// when constructing actors). No central build-context bag; if a factory
/// needs something, it's wired explicitly at registration time in the host.
/// </para>
/// </summary>
public interface IActorFactory
{
    string TypeName { get; }

    /// <summary>
    /// Builds the runtime actor from a JSON entry. Implementations deserialise
    /// <see cref="ActorEntryDefinition.Properties"/> into a typed payload and apply
    /// <see cref="ActorEntryDefinition.Transform"/> if present. The
    /// <paramref name="sceneRoot"/> is the scene-actor that will own the child
    /// — useful for factories that need to read other components on the
    /// scene root (e.g. <c>GroundPlaneActorFactory</c> reads the
    /// <c>MapComponent</c> for its dimensions).
    /// </summary>
    Actor Create(ActorEntryDefinition entry, Actor sceneRoot);
}
