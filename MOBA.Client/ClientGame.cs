using MOBA.Engine.Core;
using MOBA.Engine.Graphics;
using MOBA.Engine.Graphics.OpenGL;
using MOBA.Game;
using MOBA.Game.Client;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace MOBA.Client;

/// <summary>
/// Client-side <see cref="GameHost"/>. Owns the Silk.NET window, the OpenGL backend,
/// the per-session systems (<see cref="InputSystem"/>, <see cref="AssetManager"/>,
/// <see cref="CameraSwitcher"/>), and the <see cref="Renderer"/>. Wires window
/// callbacks to the base host lifecycle.
/// </summary>
public sealed class ClientGame : GameHost
{
    private readonly IWindow _window;

    private OpenGLBackend? _backend;
    private InputSystem? _input;
    private AssetManager? _assets;
    private Renderer? _renderer;
    private CameraSwitcher? _cameraSwitcher;

    public ClientGame()
    {
        var options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(1280, 720),
            Title = "MOBA Skeleton — RH Y-up, OpenGL (F1: camera toggle, RMB+drag: look, WASD/QE: move)",
            VSync = true,
            API = new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Core,
                ContextFlags.ForwardCompatible,
                new APIVersion(3, 3)),
        };
        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
    }

    public void Run() => _window.Run();

    private void OnLoad()
    {
        var gl = GL.GetApi(_window);
        _backend = new OpenGLBackend(gl);

        _input = new InputSystem(_window.CreateInput());
        _renderer = new Renderer(_backend);

        // Single AssetManager for every asset type the client needs. The typed
        // caches come from extension methods that live with the matching project:
        // graphics caches in MOBA.Engine.Graphics, map cache in MOBA.Game, mesh
        // caches in MOBA.Game.Client. AssetManager itself stays free of deps.
        _assets = new AssetManager();
        _assets.AddShaderCache(_backend);
        _assets.AddTextureCache(_backend);
        _assets.AddMeshCache(_backend);
        _assets.AddMapCache();

        var aspectRatio = (float)_window.FramebufferSize.X / _window.FramebufferSize.Y;
        _cameraSwitcher = new CameraSwitcher(_input.Context, aspectRatio);

        AddSystem(_input);
        AddSystem(_assets);
        AddSystem(_cameraSwitcher);
        Initialize();

        var assetsRoot = Path.Combine(AppContext.BaseDirectory, "assets");
        var shadersRoot = Path.Combine(assetsRoot, "shaders");
        var texturesRoot = Path.Combine(assetsRoot, "textures");
        var mapsRoot = Path.Combine(assetsRoot, "maps");

        var shader = _assets.LoadShader(
            Path.Combine(shadersRoot, "unlit_textured.vert"),
            Path.Combine(shadersRoot, "unlit_textured.frag"));
        var groundMaterial = new Material(shader, _assets.LoadTexture(Path.Combine(texturesRoot, "grass.png")));
        var cubeMaterial = new Material(shader, _assets.LoadTexture(Path.Combine(texturesRoot, "dev_checker.png")));

        var map = Map.FromDefinition(_assets.LoadMap(Path.Combine(mapsRoot, "default.json")));
        var groundMesh = _assets.LoadGroundMesh(map.Width, map.Length, worldUnitsPerTile: 2f);
        var cubeMesh = _assets.LoadCubeMesh();

        var world = new MobaWorld(map);
        world.Populate(Game.Scene);

        foreach (var actor in Game.Scene.Actors)
        {
            switch (actor)
            {
                case GroundPlaneActor:
                    _ = new MeshRendererComponent(actor, groundMesh, groundMaterial);
                    break;
                case TestCubeActor:
                    _ = new MeshRendererComponent(actor, cubeMesh, cubeMaterial);
                    break;
            }
        }

        _backend.Resize(_window.FramebufferSize.X, _window.FramebufferSize.Y);

        Console.WriteLine("[MOBA.Client] Loaded. F1 = camera toggle, RMB+drag = look, WASD/QE = move.");
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        _backend?.Resize(size.X, size.Y);
        if (size.Y > 0)
        {
            _cameraSwitcher?.UpdateAspect((float)size.X / size.Y);
        }
    }

    private void OnUpdate(double deltaSeconds) => Update((float)deltaSeconds);

    private void OnRender(double deltaSeconds)
    {
        if (_renderer is null || _cameraSwitcher is null)
        {
            return;
        }
        _renderer.RenderFrame(Game.Scene, _cameraSwitcher.ActiveCamera);
    }

    private void OnClosing()
    {
        // AssetManager (registered as a system) disposes every cached GPU resource
        // — shaders, textures, meshes — in Shutdown's LIFO walk. We only need to
        // dispose the backend itself here.
        Shutdown();
        _backend?.Dispose();
        Console.WriteLine("[MOBA.Client] Shutdown.");
    }

    public override void Dispose()
    {
        base.Dispose();
        _backend?.Dispose();
        _window.Dispose();
    }
}
