using System.Text.Json;

namespace MOBA.Game.Scenes;

/// <summary>
/// One spawnable entry from <c>SceneDefinition.Actors</c>.
/// <see cref="Type"/> is the discriminator the <c>SceneLoader</c> uses to look
/// up the matching <c>IActorFactory</c>. <see cref="Properties"/> stays as raw
/// JSON so each factory can deserialise its typed payload itself — no central
/// god-schema for every actor's properties.
/// </summary>
public sealed record ActorEntryDefinition(
    string Type,
    string? Id = null,
    TransformDefinition? Transform = null,
    JsonElement Properties = default);
