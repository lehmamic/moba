namespace MOBA.Game.Scenes;

/// <summary>
/// Selects which scene-root subclass / decorator the host uses post-load.
/// <see cref="Game"/> is the only type implemented today; the others are
/// reserved so adding a UI renderer + loading screen later does not break
/// the on-disk format.
/// </summary>
public enum SceneType
{
    Game,
    Loading,
    Menu,
    EndGame,
}
