using MOBA.Engine.Core.Assets;
using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Graphics;
using MOBA.Engine.Graphics.OpenGL;
using MOBA.Engine.Graphics.Rendering;
using MOBA.Engine.Networking.Riptide;
using MOBA.Game;
using MOBA.Game.Actors;
using MOBA.Game.Client;
using MOBA.Game.Client.Actors;
using MOBA.Game.Client.Cameras;
using MOBA.Game.Client.Factories;
using MOBA.Game.Client.Input;
using MOBA.Game.Client.Systems;
using MOBA.Game.Components;
using MOBA.Game.Factories;
using MOBA.Game.Models;
using MOBA.Game.Scenes;
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
        var scenesRoot = assetsRoot / "scenes";

        _assets = new AssetManager();
        _assets.AddShaderCache(_backend, shadersRoot);
        _assets.AddTextureCache(_backend, texturesRoot);
        _assets.AddMeshCache(_backend);
        _assets.AddModelCache(_backend, assetsRoot);
        _assets.AddSceneCache(scenesRoot);
        _assets.AddNavMeshCache(mapsRoot);

        var sceneDef = _assets.LoadScene("dimension-rift.json");

        var aspectRatio = (float)_window.FramebufferSize.X / _window.FramebufferSize.Y;
        _cameraSwitcher = new CameraSwitcher(_input.Context, aspectRatio, () => _window.FramebufferSize, sceneDef.Cameras);

        var markerMaterial = new Material(_assets.LoadShader("phong_textured"), _assets.LoadTexture("marker_magenta.png"));

        // Player character: glTF model in bind pose (no skinning yet — that's a later
        // iteration). Knight + marker are dynamic/network-spawned content, not
        // scene-authored, so they stay client-resolved here rather than in the JSON.
        var knight = _assets.LoadModel("models/knight-garen");

        // Client-side factory list — each factory attaches the render component
        // for its actor type. Dependencies are constructor-injected per factory;
        // no separate Actor subclass per side, no central build-context bag.
        var registry = new ActorFactoryRegistry([
            new ClientMapActorFactory(_assets),
            new ClientBuildingActorFactory(_assets),
            new ClientMonsterActorFactory(_assets),
            new TeamActorFactory(),
        ]);
        var sceneManager = new SceneManager(Game.Scene, BuildClientGameScene);
        AddSystem(sceneManager);

        GameSceneActor BuildClientGameScene(SceneDefinition definition)
        {
            var sceneActor = new GameSceneActor(definition, registry, _assets!);
            if (sceneActor.GetComponent<EnvironmentComponent>() is { Definition: { } env })
            {
                ApplyEnvironment(env);
            }
            if (sceneActor.GetChild<MapActor>() is { NavMesh: { } nav })
            {
                SpawnDebugOverlays(sceneActor, nav);
            }
            return sceneActor;
        }

        // F4 smoke-test: cycle through the debug scenes via the SceneManager.
        // PlayerActor stays on the scene top-level (no parent) so it survives;
        // the SceneActor's subtree (Buildings, Monsters, GroundPlane, overlays)
        // is cascaded out and replaced.
        var debugScenes = new[] { "dimension-rift.json", "empty-room.json" };
        var debugSceneIdx = 0;
        void SwitchScene()
        {
            debugSceneIdx = (debugSceneIdx + 1) % debugScenes.Length;
            var next = _assets!.LoadScene(debugScenes[debugSceneIdx]);
            sceneManager.LoadScene(next);
            Console.WriteLine($"[MOBA.Client] switched scene: {next.Name}, actors={Game.Scene.Actors.Count}");
        }

        sceneManager.LoadScene(sceneDef);
        var mapActor = Game.Scene.GetActor<MapActor>()!;
        var navMesh = mapActor.NavMesh;
        _cameraSwitcher.TopDown.SetPanBounds(mapActor.Map.BoundsMin, mapActor.Map.BoundsMax);
        Console.WriteLine($"[MOBA.Client] navmesh polys = {navMesh.PolyCount}");

        AddSystem(new DebugOverlaySystem(_input.Context, Game.Scene, SwitchScene));

        var transport = new RiptideClientTransport();
        var syncSystem = new NetworkSyncSystem(
            Game.Scene,
            transport,
            _assets,
            markerMaterial,
            knight,
            _cameraSwitcher,
            navMesh);

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

        Console.WriteLine("[MOBA.Client] Loaded. LMB = move, F1 = camera, F2 = navmesh, F3 = paths, F4 = scene switch, F5 = log player pos, RMB+drag = look, WASD/QE = free-fly, Arrows / edge / wheel = top-down pan & zoom.");
    }

    private void ApplyEnvironment(EnvironmentDefinition environment)
    {
        if (environment.ClearColor is { Length: 3 } c)
        {
            _renderer!.ClearColor = (c[0], c[1], c[2]);
        }
        if (environment.Light is { } l)
        {
            _light = new DirectionalLight(
                Direction: Vector3D.Normalize(new Vector3D<float>(l.Direction[0], l.Direction[1], l.Direction[2])),
                Color: new Vector3D<float>(l.Color[0], l.Color[1], l.Color[2]),
                AmbientColor: new Vector3D<float>(l.Ambient[0], l.Ambient[1], l.Ambient[2]),
                SpecularStrength: l.SpecularStrength,
                Shininess: l.Shininess);
        }
    }

    private void SpawnDebugOverlays(GameSceneActor sceneActor, NavMesh navMesh)
    {
        var assets = _assets!;
        var wireframeMaterial = new Material(assets.LoadShader("wireframe"));
        sceneActor.AddChild(new NavMeshOverlayActor(_backend!, navMesh, wireframeMaterial));

        var pathMaterial = new Material(assets.LoadShader("path"));
        sceneActor.AddChild(new PathOverlayActor(_backend!, Game.Scene, pathMaterial));
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
