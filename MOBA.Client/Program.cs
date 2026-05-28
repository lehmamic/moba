using MOBA.Engine.Core;
using MOBA.Engine.Graphics;
using MOBA.Engine.Graphics.OpenGL;
using MOBA.Game;
using MOBA.Game.Client;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

var windowOptions = WindowOptions.Default with
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

var window = Window.Create(windowOptions);

GL? gl = null;
IGraphicsBackend? backend = null;
IInputContext? input = null;
Game? game = null;
IShader? shader = null;
ITexture? grassTexture = null;
ITexture? checkerTexture = null;
IMesh? groundMesh = null;
IMesh? cubeMesh = null;
CameraSwitcher? cameraSwitcher = null;

window.Load += () =>
{
    gl = GL.GetApi(window);
    backend = new OpenGLBackend(gl);
    input = window.CreateInput();

    var assetsRoot = Path.Combine(AppContext.BaseDirectory, "assets");
    var shadersRoot = Path.Combine(assetsRoot, "shaders");
    var texturesRoot = Path.Combine(assetsRoot, "textures");

    shader = backend.CreateShader(
        File.ReadAllText(Path.Combine(shadersRoot, "unlit_textured.vert")),
        File.ReadAllText(Path.Combine(shadersRoot, "unlit_textured.frag")));

    var grass = TextureLoader.LoadRgba(Path.Combine(texturesRoot, "grass.png"));
    grassTexture = backend.CreateTexture(grass.Pixels, grass.Width, grass.Height);

    var checker = TextureLoader.LoadRgba(Path.Combine(texturesRoot, "dev_checker.png"));
    checkerTexture = backend.CreateTexture(checker.Pixels, checker.Width, checker.Height);

    var map = Map.LeagueSized();
    groundMesh = GroundMesh.CreatePlane(backend, map.Width, map.Length, worldUnitsPerTile: 2f);
    cubeMesh = CubeMesh.CreateUnitCube(backend);

    var groundMaterial = new Material(shader, grassTexture);
    var cubeMaterial = new Material(shader, checkerTexture);

    game = new Game();
    var world = new MobaWorld(map);
    world.Populate(game.Scene);

    // Attach render components to sim actors. In the real netcode flow this would come from
    // a "spawn" replication event from the server; in the loopback skeleton we wire it directly.
    foreach (var actor in game.Scene.Actors)
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

    var aspectRatio = (float)window.FramebufferSize.X / window.FramebufferSize.Y;
    cameraSwitcher = new CameraSwitcher(input, aspectRatio);
    backend.Resize(window.FramebufferSize.X, window.FramebufferSize.Y);

    Console.WriteLine("[MOBA.Client] Loaded. F1 = camera toggle, RMB+drag = look, WASD/QE = move.");
};

window.FramebufferResize += sz =>
{
    backend?.Resize(sz.X, sz.Y);
    if (sz.Y > 0)
    {
        cameraSwitcher?.UpdateAspect((float)sz.X / sz.Y);
    }
};

window.Update += dt =>
{
    cameraSwitcher?.Update((float)dt);
    game?.Tick((float)dt);
};

window.Render += _ =>
{
    if (backend is null || game is null || cameraSwitcher is null)
    {
        return;
    }

    backend.BeginFrame(0.1f, 0.15f, 0.2f);

    var viewProjection = cameraSwitcher.ActiveCamera.ViewProjection;
    foreach (var actor in game.Scene.Actors)
    {
        var renderer = actor.GetComponent<MeshRendererComponent>();
        if (renderer is null)
        {
            continue;
        }
        var mvp = actor.WorldMatrix * viewProjection;
        backend.DrawMesh(renderer.Mesh, renderer.Material, mvp);
    }

    backend.EndFrame();
};

window.Closing += () =>
{
    cubeMesh?.Dispose();
    groundMesh?.Dispose();
    checkerTexture?.Dispose();
    grassTexture?.Dispose();
    shader?.Dispose();
    backend?.Dispose();
    input?.Dispose();
    game?.Shutdown();
    Console.WriteLine("[MOBA.Client] Shutdown.");
};

window.Run();
window.Dispose();
