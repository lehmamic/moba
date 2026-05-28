using MOBA.Engine.Core;

namespace MOBA.Engine.Graphics;

/// <summary>
/// Caches and disposes GPU resources loaded from disk: GLSL shaders (key =
/// <c>"{vertPath}|{fragPath}"</c>) and textures (key = file path). The backing
/// loaders are <see cref="File.ReadAllText(string)"/> + <see cref="IGraphicsBackend.CreateShader"/>
/// for shaders and <see cref="TextureLoader.LoadRgba"/> + <see cref="IGraphicsBackend.CreateTexture"/>
/// for textures. <see cref="OnShutdown"/> disposes everything while the GL context
/// is still alive.
/// </summary>
public sealed class AssetManager : IEngineSystem
{
    private readonly IGraphicsBackend _backend;
    private readonly Dictionary<string, IShader> _shaders = [];
    private readonly Dictionary<string, ITexture> _textures = [];

    public AssetManager(IGraphicsBackend backend) => _backend = backend;

    public IShader LoadShader(string vertexPath, string fragmentPath)
    {
        var key = $"{vertexPath}|{fragmentPath}";
        if (!_shaders.TryGetValue(key, out var shader))
        {
            shader = _backend.CreateShader(
                File.ReadAllText(vertexPath),
                File.ReadAllText(fragmentPath));
            _shaders[key] = shader;
        }
        return shader;
    }

    public ITexture LoadTexture(string path)
    {
        if (!_textures.TryGetValue(path, out var texture))
        {
            var data = TextureLoader.LoadRgba(path);
            texture = _backend.CreateTexture(data.Pixels, data.Width, data.Height);
            _textures[path] = texture;
        }
        return texture;
    }

    public void OnInitialize() { }

    public void OnUpdate(GameTime time) { }

    public void OnShutdown()
    {
        foreach (var shader in _shaders.Values)
        {
            shader.Dispose();
        }
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }
        _shaders.Clear();
        _textures.Clear();
    }

    public void Dispose() => OnShutdown();
}
