using MOBA.Engine.Core;
using MOBA.Utilities;

namespace MOBA.Engine.Graphics;

/// <summary>
/// Registers and reads GPU resources through a shared <see cref="AssetManager"/>.
/// Each cache is registered with the root folder it owns (<c>shadersRoot</c>,
/// <c>texturesRoot</c>, <c>modelsRoot</c>) so callers reference assets by short
/// name or filename, not by full path. Cache keys are plain <see cref="string"/>;
/// public-API root paths are typed as <see cref="AbsolutePath"/>.
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
    /// and model caches: the registered root owns the lookup; callers only ever
    /// see filenames.
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
    /// Registers a model cache keyed by short name (e.g. <c>"knight-garen"</c>).
    /// Files resolve to <c>{modelsRoot}/{name}.glb</c>. The glTF loader reads the
    /// per-material shader name from <c>material.extras.shader</c> and goes through
    /// the shader cache; if absent it defaults to <c>"phong_textured"</c>.
    /// </summary>
    public static AssetCache<string, Model> AddModelCache(
        this AssetManager assets,
        IGraphicsBackend backend,
        AbsolutePath modelsRoot) =>
        assets.AddCache<string, Model>(name =>
            GltfModelLoader.Load(
                modelsRoot / $"{name}.glb",
                backend,
                assets));

    public static IShader LoadShader(this AssetManager assets, string name) =>
        assets.Cache<string, IShader>().GetOrLoad(name);

    public static ITexture LoadTexture(this AssetManager assets, string filename) =>
        assets.Cache<string, ITexture>().GetOrLoad(filename);

    public static Model LoadModel(this AssetManager assets, string name) =>
        assets.Cache<string, Model>().GetOrLoad(name);
}