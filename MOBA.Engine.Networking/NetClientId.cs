namespace MOBA.Engine.Networking;

/// <summary>
/// Stable identifier for a connected client on the server transport.
/// Wraps the underlying transport's native id (Riptide uses <see cref="ushort"/>)
/// behind a strongly-typed value so callers don't accidentally mix client ids
/// with actor network ids.
/// </summary>
public readonly record struct NetClientId(ushort Value)
{
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
