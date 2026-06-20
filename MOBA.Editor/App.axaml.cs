using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace MOBA.Editor;

/// <summary>
/// Application root — loads the XAML resource dictionary (theme + global
/// styles) and creates the <see cref="MainWindow"/> on the classic desktop
/// lifetime that <see cref="Program.Main"/> starts.
/// </summary>
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
