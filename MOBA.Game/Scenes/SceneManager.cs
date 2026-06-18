using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Core.World;

namespace MOBA.Game.Scenes;

/// <summary>
/// Single entry point for scene loading + switching. Holds a reference to the
/// active <see cref="GameSceneActor"/>, removes it (cascading through its
/// subtree) before loading a new one. Network-spawned actors (PlayerActor,
/// MarkerActor) live on the scene top-level — not as children of the scene
/// root — so a scene switch leaves them alone.
///
/// <para>
/// Side-specific scene-root construction goes through the constructor
/// delegate; server hands in <c>def =&gt; new GameSceneActor(def, ...)</c>, client hands
/// in <c>def =&gt; new ClientGameSceneActor(def, ...)</c>. Future SceneTypes
/// (Loading, Menu, EndGame) move to a registry keyed on
/// <see cref="SceneType"/> when there's more than one to switch between.
/// </para>
/// </summary>
public sealed class SceneManager : IEngineSystem
{
    private readonly Scene _scene;
    private readonly Func<SceneDefinition, GameSceneActor> _sceneActorFactory;

    public SceneManager(Scene scene, Func<SceneDefinition, GameSceneActor> sceneActorFactory)
    {
        _scene = scene;
        _sceneActorFactory = sceneActorFactory;
    }

    /// <summary>The scene-root currently in <see cref="Scene.Actors"/>, if any.</summary>
    public GameSceneActor? Current { get; private set; }

    public void LoadScene(SceneDefinition def)
    {
        if (Current is not null)
        {
            _scene.RemoveActor(Current);   // cascading remove of every child
            Current = null;
        }
        Current = _sceneActorFactory(def);
        _scene.AddActor(Current);          // cascading add of every child
    }

    public void OnInitialize() { }

    public void OnUpdate(GameTime time) { }   // future: async-load progress / scene transitions

    public void OnShutdown() => Current = null;

    public void Dispose() { }
}
