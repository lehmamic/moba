namespace MOBA.Game.Messages;

/// <summary>
/// Compact team discriminator for the wire. Game-side team identity stays the
/// string <c>Name</c> on <c>TeamActor</c> / <c>Building</c>; this is the byte
/// that travels in <see cref="ActorSpawnMessage"/> so the client can colour and
/// assemble a replicated actor without a string on the wire.
/// </summary>
public enum TeamId : byte
{
    None = 0,
    Order = 1,
    Chaos = 2,
}

/// <summary>Conversions between the wire <see cref="TeamId"/> and the string team name.</summary>
public static class TeamIds
{
    public static TeamId FromName(string? name) => name switch
    {
        "Order" => TeamId.Order,
        "Chaos" => TeamId.Chaos,
        _ => TeamId.None,
    };

    public static string? ToName(this TeamId team) => team switch
    {
        TeamId.Order => "Order",
        TeamId.Chaos => "Chaos",
        _ => null,
    };
}
