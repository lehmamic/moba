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
    private DirectionalLight _light;

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

        var assetsRoot = AbsolutePath.AppBaseDirectory / "assets";
        var shadersRoot = assetsRoot / "shaders";
        var texturesRoot = assetsRoot / "textures";
        var mapsRoot = assetsRoot / "maps";
        var modelsRoot = assetsRoot / "models";

        _assets = new AssetManager();
        _assets.AddShaderCache(_backend, shadersRoot);
        _assets.AddTextureCache(_backend, texturesRoot);
        _assets.AddMeshCache(_backend);
        _assets.AddModelCache(_backend, modelsRoot);
        _assets.AddMapCache(mapsRoot);

        var aspectRatio = (float)_window.FramebufferSize.X / _window.FramebufferSize.Y;
        _cameraSwitcher = new CameraSwitcher(_input.Context, aspectRatio);

        var shader = _assets.LoadShader("phong_textured");
        var groundMaterial = new Material(shader, _assets.LoadTexture("grass.png"));
        var markerMaterial = new Material(shader, _assets.LoadTexture("marker_magenta.png"));

        // Player character: glTF model in bind pose (no skinning yet — that's a later
        // iteration). Material's shader resolves through the shader cache (default
        // "phong_textured" unless the glTF material's extras.shader overrides).
        // Multi-part assets walk knight.Parts directly.
        var knight = _assets.LoadModel("knight-garen");

        // Sun-like directional light from upper-front-right. Direction points *toward*
        // the light source — see DirectionalLight XML doc.
        _light = new DirectionalLight(
            Direction: Vector3D.Normalize(new Vector3D<float>(0.3f, 1.0f, 0.4f)),
            Color: new Vector3D<float>(1.0f, 0.95f, 0.9f),
            AmbientColor: new Vector3D<float>(0.2f, 0.22f, 0.28f),
            SpecularStrength: 0.5f,
            Shininess: 32f);

        var map = Map.FromDefinition(_assets.LoadMap("default.json"));
        var groundMesh = _assets.LoadGroundMesh(map.Width, map.Length, worldUnitsPerTile: 2f);

        var world = new MobaWorld(map);
        world.Populate(Game.Scene);

        // Attach a renderer to the only pre-spawned actor (the ground). Player
        // actors are spawned dynamically by NetworkSyncSystem when the server
        // sends an ActorSpawn(Player) in reply to our JoinMessage.
        foreach (var actor in Game.Scene.Actors)
        {
            if (actor is GroundPlaneActor)
            {
                _ = new MeshRendererComponent(actor, groundMesh, groundMaterial);
            }
        }

        var transport = new RiptideClientTransport();
        var syncSystem = new NetworkSyncSystem(
            Game.Scene,
            transport,
            _assets,
            markerMaterial,
            knight,
            _cameraSwitcher);

        // Order matters: transport before sync so MessageReceived events fire
        // after polling. NetworkSyncSystem.OnInitialize sends the Join message
        // — by then RiptideClientTransport's blocking OnInitialize has already
        // confirmed the connection.
        AddSystem(_input);
        AddSystem(_assets);
        AddSystem(transport);
        AddSystem(syncSystem);
        AddSystem(_cameraSwitcher);
        Initialize();

        _backend.Resize(_window.FramebufferSize.X, _window.FramebufferSize.Y);

        Console.WriteLine("[MOBA.Client] Loaded. LMB = move player, F1 = camera toggle, RMB+drag = look, WASD/QE = free-fly.");
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
        _renderer.RenderFrame(Game.Scene, _cameraSwitcher.ActiveCamera, _light);
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
