using MOBA.Engine.Core;
using MOBA.Engine.Graphics;
using MOBA.Engine.Graphics.OpenGL;
using MOBA.Engine.Networking.Riptide;
using MOBA.Game;
using MOBA.Game.Client;
using MOBA.Utilities;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace MOBA.Client;

/// <summary>
/// Client-side <see cref="GameHost"/>. Owns the Silk.NET window, the OpenGL backend,
/// the per-session systems (<see cref="InputSystem"/>, <see cref="AssetManager"/>,
/// <see cref="CameraSwitcher"/>, transport, sync, click-input), and the
/// <see cref="Renderer"/>. Connects to the server on Load; click-to-move flows
/// over the Riptide transport.
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
            Title = "MOBA Skeleton — RH Y-up, OpenGL (F1: camera toggle, RMB+drag: look, LMB: move cube, WASD/QE: free-fly)",
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

        _assets = new AssetManager();
        _assets.AddShaderCache(_backend);
        _assets.AddTextureCache(_backend);
        _assets.AddMeshCache(_backend);
        _assets.AddMapCache();

        var aspectRatio = (float)_window.FramebufferSize.X / _window.FramebufferSize.Y;
        _cameraSwitcher = new CameraSwitcher(_input.Context, aspectRatio);

        var assetsRoot = AbsolutePath.AppBaseDirectory / "assets";
        var shadersRoot = assetsRoot / "shaders";
        var texturesRoot = assetsRoot / "textures";
        var mapsRoot = assetsRoot / "maps";

        var shader = _assets.LoadShader(
            shadersRoot / "unlit_textured.vert",
            shadersRoot / "unlit_textured.frag");
        var groundMaterial = new Material(shader, _assets.LoadTexture(texturesRoot / "grass.png"));
        var cubeMaterial = new Material(shader, _assets.LoadTexture(texturesRoot / "dev_checker.png"));
        var markerMaterial = new Material(shader, _assets.LoadTexture(texturesRoot / "marker_magenta.png"));

        var map = Map.FromDefinition(_assets.LoadMap(mapsRoot / "default.json"));
        var groundMesh = _assets.LoadGroundMesh(map.Width, map.Length, worldUnitsPerTile: 2f);
        var cubeMesh = _assets.LoadCubeMesh();

        var world = new MobaWorld(map);
        world.Populate(Game.Scene);

        // Build the local network sync system; we'll register the pre-spawned cube
        // below so the server's position updates land on the right actor.
        var transport = new RiptideClientTransport();
        var syncSystem = new NetworkSyncSystem(Game.Scene, transport, _assets, markerMaterial);

        TestCubeActor? cubeActor = null;
        foreach (var actor in Game.Scene.Actors)
        {
            switch (actor)
            {
                case GroundPlaneActor:
                    _ = new MeshRendererComponent(actor, groundMesh, groundMaterial);
                    break;
                case TestCubeActor cube:
                    _ = new MeshRendererComponent(cube, cubeMesh, cubeMaterial);
                    _ = new NetworkIdentityComponent(cube, 2);
                    _ = new LocalCubeInputComponent(cube, _cameraSwitcher, transport);
                    cubeActor = cube;
                    break;
            }
        }
        if (cubeActor is not null)
        {
            syncSystem.Register(2, cubeActor);
        }

        // Order matters: transport before sync so MessageReceived events fire
        // after polling, sync before camera so any spawn from the server is
        // applied before render. The click-to-move handler lives on the cube
        // actor (LocalCubeInputComponent) and runs via ProcessInput, not as a
        // system.
        AddSystem(_input);
        AddSystem(_assets);
        AddSystem(transport);
        AddSystem(syncSystem);
        AddSystem(_cameraSwitcher);
        Initialize();

        _backend.Resize(_window.FramebufferSize.X, _window.FramebufferSize.Y);

        Console.WriteLine("[MOBA.Client] Loaded. LMB = move cube, F1 = camera toggle, RMB+drag = look, WASD/QE = free-fly.");
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        _backend?.Resize(size.X, size.Y);
        if (size.Y > 0)
        {
            _cameraSwitcher?.UpdateAspect((float)size.X / size.Y);
        }
    }

    private void OnUpdate(double deltaSeconds)
    {
        if (_input is not null)
        {
            ProcessInput(_input.CaptureSnapshot(_window.FramebufferSize));
        }
        Update((float)deltaSeconds);
    }

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
