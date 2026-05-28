namespace MOBA.Engine.Core;

/// <summary>
/// One-stop hub for every <see cref="AssetCache{TKey,TAsset}"/> the host needs.
/// Extension methods in higher-layer projects (`MOBA.Engine.Graphics`, `MOBA.Game`,
/// ...) register typed caches (shader + texture, map JSON, champion stats, ...) and
/// expose convenient <c>Load*</c> helpers. The host registers a single
/// <see cref="AssetManager"/> as one <see cref="IEngineSystem"/>; lifecycle calls
/// propagate to every cache (forward for init/update, LIFO for shutdown).
/// </summary>
public sealed class AssetManager : IEngineSystem
{
    private readonly List<IEngineSystem> _caches = [];
    private readonly Dictionary<(Type Key, Type Asset), object> _byType = [];

    public AssetCache<TKey, TAsset> AddCache<TKey, TAsset>(Func<TKey, TAsset> loader)
        where TKey : notnull
    {
        var cache = new AssetCache<TKey, TAsset>(loader);
        _caches.Add(cache);
        _byType[(typeof(TKey), typeof(TAsset))] = cache;
        return cache;
    }

    public AssetCache<TKey, TAsset> Cache<TKey, TAsset>() where TKey : notnull
    {
        if (_byType.TryGetValue((typeof(TKey), typeof(TAsset)), out var cache))
        {
            return (AssetCache<TKey, TAsset>)cache;
        }
        throw new InvalidOperationException(
            $"No AssetCache registered for ({typeof(TKey).Name}, {typeof(TAsset).Name}). " +
            "Call the appropriate Add*Cache extension before requesting it.");
    }

    public void OnInitialize()
    {
        foreach (var cache in _caches)
        {
            cache.OnInitialize();
        }
    }

    public void OnUpdate(GameTime time)
    {
        foreach (var cache in _caches)
        {
            cache.OnUpdate(time);
        }
    }

    public void OnShutdown()
    {
        for (var i = _caches.Count - 1; i >= 0; i--)
        {
            _caches[i].OnShutdown();
        }
    }

    public void Dispose()
    {
        OnShutdown();
        GC.SuppressFinalize(this);
    }
}
