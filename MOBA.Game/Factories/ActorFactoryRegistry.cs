namespace MOBA.Game.Factories;

/// <summary>
/// Side-specific dictionary from <see cref="IActorFactory.TypeName"/> to
/// factory instance. Server builds one with sim factories only, client with
/// client factories that produce render subclasses. Duplicate <c>TypeName</c>s
/// throw at construction time — registration errors surface immediately, not
/// when the first scene tries to load.
/// </summary>
public sealed class ActorFactoryRegistry
{
    private readonly IReadOnlyDictionary<string, IActorFactory> _byType;

    public ActorFactoryRegistry(IReadOnlyList<IActorFactory> factories)
    {
        var byType = new Dictionary<string, IActorFactory>(StringComparer.Ordinal);
        foreach (var f in factories)
        {
            if (!byType.TryAdd(f.TypeName, f))
            {
                throw new InvalidOperationException(
                    $"Duplicate IActorFactory registration for type '{f.TypeName}'.");
            }
        }

        _byType = byType;
    }

    public IActorFactory Get(string typeName)
    {
        if (!_byType.TryGetValue(typeName, out var factory))
        {
            throw new InvalidOperationException(
                $"No IActorFactory registered for actor type '{typeName}'. " +
                $"Available: {string.Join(", ", _byType.Keys)}.");
        }

        return factory;
    }
}
