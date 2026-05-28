namespace MOBA.Engine.Core;

/// <summary>
/// Generic lazy-load cache that participates in the <see cref="GameHost"/> lifecycle.
/// Looks up an asset by <typeparamref name="TKey"/>; if missing, calls the loader
/// function supplied to the constructor and caches the result. Any cached asset
/// that implements <see cref="IDisposable"/> is disposed in <see cref="OnShutdown"/>
/// (so the cache is safe to use for GPU handles, native blobs, parsed JSON DTOs,
/// or plain <see cref="string"/> contents alike).
/// </summary>
/// <typeparam name="TKey">Cache key (typically a file path or path tuple).</typeparam>
/// <typeparam name="TAsset">Loaded asset value.</typeparam>
public class AssetCache<TKey, TAsset> : IEngineSystem
    where TKey : notnull
{
    private readonly Func<TKey, TAsset> _loader;
    private readonly Dictionary<TKey, TAsset> _cache = [];

    public AssetCache(Func<TKey, TAsset> loader) => _loader = loader;

    public IReadOnlyDictionary<TKey, TAsset> Cached => _cache;

    public TAsset GetOrLoad(TKey key)
    {
        if (!_cache.TryGetValue(key, out var asset))
        {
            asset = _loader(key);
            _cache[key] = asset;
        }
        return asset;
    }

    public void OnInitialize() { }

    public void OnUpdate(GameTime time) { }

    public void OnShutdown()
    {
        foreach (var asset in _cache.Values)
        {
            if (asset is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _cache.Clear();
    }

    public void Dispose()
    {
        OnShutdown();
        GC.SuppressFinalize(this);
    }
}
