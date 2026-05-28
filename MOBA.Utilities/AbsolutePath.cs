namespace MOBA.Utilities;

/// <summary>
/// Strongly typed, fully-qualified path. Normalised via <see cref="Path.GetFullPath(string)"/>
/// so equality is reliable across "./" and "../" segments; <see cref="ToString"/> returns
/// the OS-native separator form. Compose with the <c>/</c> operator:
/// <code>
/// AbsolutePath shader = AppContext.BaseDirectory / "assets" / "shaders" / "unlit_textured.vert";
/// </code>
/// </summary>
public sealed class AbsolutePath : IEquatable<AbsolutePath>
{
    private readonly string _normalised;

    private AbsolutePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException($"'{path}' is not a fully-qualified absolute path.", nameof(path));
        }
        // Trim the trailing directory separator so e.g. AppContext.BaseDirectory
        // ("/.../net10.0/") and its own Parent compare as equal. The BCL helper
        // is root-safe — it leaves "/" and "C:\" intact.
        _normalised = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    /// <summary>
    /// The directory the running .NET executable resides in (<see cref="AppContext.BaseDirectory"/>).
    /// Typed entry point for paths into the deployed application — use this instead of
    /// <c>(AbsolutePath)AppContext.BaseDirectory</c> at the callsite.
    /// </summary>
    public static AbsolutePath AppBaseDirectory => new(AppContext.BaseDirectory);

    /// <summary>The current working directory at call time (<see cref="Directory.GetCurrentDirectory"/>).</summary>
    public static AbsolutePath CurrentDirectory => new(Directory.GetCurrentDirectory());

    public string FileName => Path.GetFileName(_normalised);

    public string Extension => Path.GetExtension(_normalised);

    public AbsolutePath? Parent
    {
        get
        {
            var parent = Path.GetDirectoryName(_normalised);
            return parent is null ? null : new AbsolutePath(parent);
        }
    }

    public bool FileExists => File.Exists(_normalised);

    public bool DirectoryExists => Directory.Exists(_normalised);

    public static AbsolutePath operator /(AbsolutePath left, string right) =>
        new(Path.Combine(left._normalised, RelativePath.NormaliseSeparators(right)));

    public static AbsolutePath operator /(AbsolutePath left, RelativePath right) =>
        new(Path.Combine(left._normalised, ((string)right)));

    public static implicit operator string(AbsolutePath path) => path._normalised;

    public static explicit operator AbsolutePath(string s) => new(s);

    public override string ToString() => _normalised;

    public bool Equals(AbsolutePath? other) =>
        other is not null && string.Equals(_normalised, other._normalised, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as AbsolutePath);

    public override int GetHashCode() => _normalised.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(AbsolutePath? left, AbsolutePath? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(AbsolutePath? left, AbsolutePath? right) => !(left == right);
}
