using Avalonia;

namespace MOBA.Editor;

/// <summary>
/// Avalonia desktop entry. Configures the framework and starts the classic
/// desktop lifetime — the same shape every Avalonia 11 app uses; we deviate
/// only by wiring our own <see cref="App"/> resource dictionary + main
/// window.
/// </summary>
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Exposed for the Avalonia tooling (designer / previewer) to discover the
    /// app builder via reflection. Not actually invoked at runtime past
    /// <see cref="Main"/>.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
