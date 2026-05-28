namespace MOBA.Utilities;

/// <summary>
/// C# 14 extension operator that lets a path expression start from a plain string
/// literal:
/// <code>
/// RelativePath shader = "assets" / "shaders" / "unlit_textured.vert";
/// </code>
/// The first <c>/</c> here is the extension operator on <see cref="string"/>;
/// subsequent <c>/</c> calls bind to <see cref="RelativePath.op_Division(RelativePath, string)"/>.
/// </summary>
public static class PathStringExtensions
{
    extension(string)
    {
        public static RelativePath operator /(string left, string right) =>
            (RelativePath)left / right;
    }
}
