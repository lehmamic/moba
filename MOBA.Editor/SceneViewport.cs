using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Assets;
using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Graphics;
using MOBA.Engine.Graphics.OpenGL;
using MOBA.Engine.Graphics.Rendering;
using MOBA.Engine.Graphics.Rendering.Passes;
using MOBA.Game;
using MOBA.Game.Actors;
using MOBA.Game.Client;
using MOBA.Game.Client.Factories;
using MOBA.Game.Components;
using MOBA.Game.Factories;
using MOBA.Game.Models;
using MOBA.Game.Scenes;
using MOBA.Utilities;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace MOBA.Editor;

/// <summary>
/// Avalonia-hosted OpenGL viewport. Bridges Avalonia's <see cref="GlInterface"/>
/// to a Silk.NET <see cref="GL"/> instance, owns its own
/// <see cref="OpenGLBackend"/> + <see cref="Renderer"/> + <see cref="Scene"/>,
/// loads <c>dimension-rift.json</c> via the same loader the game uses, and
/// attaches the server-side gameplay components (<see cref="MinionSpawnerComponent"/>
/// on each team, <see cref="AggroComponent"/> + <see cref="AttackComponent"/>
/// on towers / nexus) so play-mode sim ticks alongside the renderer.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "Avalonia controls are not IDisposable; lifetime is managed via OnOpenGlInit / OnOpenGlDeinit.")]
public sealed class SceneViewport : OpenGlControlBase
{
    private GL? _gl;
    private OpenGLBackend? _backend;
    private Renderer? _renderer;
    private Scene? _scene;
    private SceneManager? _sceneManager;
    private AssetManager? _assets;
    private EditorCamera? _camera;
    private DirectionalLight _light;
    private DateTime _lastTick = DateTime.UtcNow;
    private double _totalSeconds;

    protected override void OnOpenGlInit(GlInterface gl)
    {
        _gl = GL.GetApi(gl.GetProcAddress);
        _backend = new OpenGLBackend(_gl);

        _assets = new AssetManager();
        var assetsRoot = AbsolutePath.AppBaseDirectory / "assets";
        _assets.AddShaderCache(_backend, assetsRoot / "shaders");
        _assets.AddTextureCache(_backend, assetsRoot / "textures");
        _assets.AddMeshCache(_backend);
        _assets.AddModelCache(_backend, assetsRoot);
        _assets.AddSceneCache(assetsRoot / "scenes");
        _assets.AddNavMeshCache(assetsRoot / "maps");

        _scene = new Scene();
        var barQuad = _assets.LoadQuadMesh();
        var barShader = _assets.LoadShader("bar");
        var registry = new ActorFactoryRegistry([
            new ClientMapActorFactory(_assets),
            new ClientBuildingActorFactory(_assets, barQuad, barShader, _scene),
            new ClientMonsterActorFactory(_assets, barQuad, barShader, _scene),
            new TeamActorFactory(),
        ]);

        _sceneManager = new SceneManager(_scene, def => new GameSceneActor(def, registry, _assets));
        _sceneManager.LoadScene(_assets.LoadScene("dimension-rift.json"));

        // Play-mode wiring — the server-side gameplay components MOBA.Server
        // would attach. No PlayerActor here (champions are network-spawned),
        // so champion combat is dormant; waves + towers run.
        var navMesh = _scene.GetActor<MapActor>()!.NavMesh;
        foreach (var team in _scene.Actors.OfType<TeamActor>())
        {
            _ = new MinionSpawnerComponent(team, _scene, navMesh);
        }

        foreach (var building in _scene.Actors.OfType<BuildingActor>())
        {
            if (building.Definition.Type is "Tower" or "Nexus")
            {
                _ = new AggroComponent(building, AggroProfiles.ForBuilding(building.Definition.Type), _scene);
                _ = new AttackComponent(building, AttackProfiles.ForBuilding(building.Definition.Type));
            }
        }

        _light = new DirectionalLight(
            Direction: Vector3D.Normalize(new Vector3D<float>(0.3f, 1f, 0.4f)),
            Color: new Vector3D<float>(1f, 0.95f, 0.9f),
            AmbientColor: new Vector3D<float>(0.2f, 0.22f, 0.28f),
            SpecularStrength: 0.5f,
            Shininess: 32f);

        _camera = new EditorCamera(this);

        // Game pipeline today — editor-only passes (grid / lanes / spawns)
        // land in the next commit on top of this composition.
        var pipeline = new RenderPipeline([
            new StaticMeshPass(),
            new SkinnedMeshPass(),
            new BillboardPass(),
        ]);
        _renderer = new Renderer(_backend, pipeline);
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (_gl is null || _backend is null || _renderer is null || _scene is null || _camera is null)
        {
            return;
        }

        // Avalonia hands us its FBO every frame; bind it as the draw target
        // before the backend re-applies viewport / depth / blend state.
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)fb);

        var width = (int)Bounds.Width;
        var height = (int)Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _backend.Resize(width, height);
        _camera.Camera.AspectRatio = (float)width / height;

        var now = DateTime.UtcNow;
        var dt = (float)Math.Min((now - _lastTick).TotalSeconds, 0.1d);
        _lastTick = now;
        _totalSeconds += dt;
        _camera.Tick(dt);

        _scene.Update(new GameTime(dt, _totalSeconds));
        _renderer.RenderFrame(_scene, _camera.Camera, _light);

        RequestNextFrameRendering();
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _scene?.Clear();
        _backend?.Dispose();
        _backend = null;
        _renderer = null;
        _scene = null;
        _sceneManager = null;
        _assets = null;
        _camera = null;
        _gl = null;
    }
}
