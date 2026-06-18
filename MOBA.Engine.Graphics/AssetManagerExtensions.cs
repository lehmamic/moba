using MOBA.Engine.Core.Assets;
using MOBA.Engine.Graphics.Abstractions;
using MOBA.Engine.Graphics.Loaders;
using MOBA.Engine.Graphics.Rendering;
using MOBA.Utilities;

namespace MOBA.Engine.Graphics;

/// <summary>
/// Registers and reads GPU resources through a shared <see cref="AssetManager"/>.
/// Each cache is registered with the root folder it owns (<c>shadersRoot</c>,
/// <c>texturesRoot</c>, <c>assetsRoot</c>) so callers reference assets by short
/// name (shaders / textures) or by repo-relative path under <c>assets/</c>
/// (models). Cache keys are plain <see cref="string"/>; public-API root paths
/// are typed as <see cref="AbsolutePath"/>.
/// </summary>
public static class AssetManagerExtensions
{
    /// <summary>
    /// Registers a shader cache keyed by short name. The on-disk pair lives at
    /// <c>{shadersRoot}/{name}.vert</c> + <c>{name}.frag</c>. The cache itself is
    /// the shader registry — unknown names fail at file-open time.
    /// </summary>
    public static AssetCache<string, IShader> AddShaderCache(
        this AssetManager assets,
        IGraphicsBackend backend,
        AbsolutePath shadersRoot) =>
        assets.AddCache<string, IShader>(name =>
            backend.CreateShader(
                File.ReadAllText(shadersRoot / $"{name}.vert"),
                File.ReadAllText(shadersRoot / $"{name}.frag")));

    /// <summary>
    /// Registers a texture cache keyed by file name (including extension). Files
    /// resolve to <c>{texturesRoot}/{filename}</c>. Same convention as the shader
    /// cache: the registered root owns the lookup; callers only ever see filenames.
    /// </summary>
    public static AssetCache<string, ITexture> AddTextureCache(
        this AssetManager assets,
        IGraphicsBackend backend,
        AbsolutePath texturesRoot) =>
        assets.AddCache<string, ITexture>(filename =>
        {
            var data = TextureLoader.LoadRgba(texturesRoot / filename);
            return backend.CreateTexture(data.Pixels, data.Width, data.Height);
        });

    /// <summary>
    /// Registers a single model cache for every disk-loaded <see cref="Model"/> —
    /// character glTFs, terrain mesh, building / monster prefabs all flow through
    /// the same cache. Keys are repo-relative paths under <paramref name="assetsRoot"/>
    /// without an extension, e.g. <c>"models/knight-garen"</c>,
    /// <c>"maps/dimension-rift"</c>, <c>"buildings/blue-tower"</c>. The loader
    /// dispatches on extension: <c>{assetsRoot}/{key}.glb</c> goes through
    /// <see cref="GltfModelLoader"/>, <c>{assetsRoot}/{key}.obj</c> through
    /// <see cref="ObjModelLoader"/>; GLB wins when both exist.
    /// </summary>
    public static AssetCache<string, Model> AddModelCache(
        this AssetManager assets,
        IGraphicsBackend backend,
        AbsolutePath assetsRoot) =>
        assets.AddCache<string, Model>(relativePath =>
        {
            var glb = assetsRoot / $"{relativePath}.glb";
            if (glb.FileExists)
            {
                return GltfModelLoader.Load(glb, backend, assets);
            }
            var obj = assetsRoot / $"{relativePath}.obj";
            if (obj.FileExists)
            {
                return ObjModelLoader.Load(obj, backend, assets);
            }
            throw new FileNotFoundException(
                $"No '{relativePath}.glb' or '{relativePath}.obj' under '{assetsRoot}'.");
        });

    public static IShader LoadShader(this AssetManager assets, string name) =>
        assets.Cache<string, IShader>().GetOrLoad(name);

    public static ITexture LoadTexture(this AssetManager assets, string filename) =>
        assets.Cache<string, ITexture>().GetOrLoad(filename);

    /// <summary>
    /// Loads a <see cref="Model"/> by its repo-relative path under <c>assets/</c>
    /// without the file extension (e.g. <c>"models/knight-garen"</c>).
    /// </summary>
    public static Model LoadModel(this AssetManager assets, string relativePath) =>
        assets.Cache<string, Model>().GetOrLoad(relativePath);
}
