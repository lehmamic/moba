using MOBA.Engine.Core;

namespace MOBA.Engine.Graphics;

/// <summary>
/// Registers and reads GPU resources through a shared <see cref="AssetManager"/>:
/// shaders keyed by the <c>(vertex, fragment)</c> path pair, textures keyed by file
/// path. The loader lambdas keep all <c>IGraphicsBackend</c> knowledge local to
/// this assembly — <see cref="AssetManager"/> itself stays graphics-agnostic.
/// </summary>
public static class AssetManagerExtensions
{
    public static AssetCache<(string Vertex, string Fragment), IShader> AddShaderCache(
        this AssetManager assets,
        IGraphicsBackend backend) =>
        assets.AddCache<(string, string), IShader>(key =>
            backend.CreateShader(
                File.ReadAllText(key.Item1),
                File.ReadAllText(key.Item2)));

    public static AssetCache<string, ITexture> AddTextureCache(
        this AssetManager assets,
        IGraphicsBackend backend) =>
        assets.AddCache<string, ITexture>(path =>
        {
            var data = TextureLoader.LoadRgba(path);
            return backend.CreateTexture(data.Pixels, data.Width, data.Height);
        });

    public static IShader LoadShader(this AssetManager assets, string vertexPath, string fragmentPath) =>
        assets.Cache<(string, string), IShader>().GetOrLoad((vertexPath, fragmentPath));

    public static ITexture LoadTexture(this AssetManager assets, string path) =>
        assets.Cache<string, ITexture>().GetOrLoad(path);
}
