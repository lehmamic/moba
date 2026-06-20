using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MOBA.Editor;

/// <summary>
/// Shell window — a DockPanel with the right-side placeholder Inspector and
/// the central area reserved for the OpenGL scene viewport (lands in the
/// next commit).
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow() => AvaloniaXamlLoader.Load(this);
}
