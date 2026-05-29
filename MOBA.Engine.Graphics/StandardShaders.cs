using MOBA.Engine.Core;

namespace MOBA.Engine.Graphics;

/// <summary>
/// Curated set of project-owned shaders. Imported asset files (glTF materials,
/// future format imports) reference one entry of this registry by key — they
/// never bring their own GLSL. This keeps the shader set under our control and
/// reviewable in <c>assets/shaders/</c>.
/// </summary>
public static class StandardShaders
{
    public const string UnlitTextured = "unlit_textured";
    public const string PhongTextured = "phong_textured";

    public static readonly IReadOnlySet<string> Keys = new HashSet<string>(StringComparer.Ordinal)
    {
        UnlitTextured,
        PhongTextured,
    };

    /// <summary>
    /// Loads the GLSL pair <c>{shadersRoot}/{key}.vert</c> + <c>{key}.frag</c> through
    /// the shared <see cref="AssetManager"/> cache. Unknown keys throw — asset files
    /// must not request shaders that the project does not own.
    /// </summary>
    public static IShader Resolve(string key, AssetManager assets, string shadersRoot)
    {
        if (!Keys.Contains(key))
        {
            throw new ArgumentException(
                $"Unknown standard shader key '{key}'. Known: {string.Join(", ", Keys)}.",
                nameof(key));
        }
        var vert = Path.Combine(shadersRoot, $"{key}.vert");
        var frag = Path.Combine(shadersRoot, $"{key}.frag");
        return assets.LoadShader(vert, frag);
    }
}
