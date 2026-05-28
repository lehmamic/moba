namespace MOBA.Utilities;

/// <summary>
/// Strongly typed relative path. Normalised to forward-slash form internally so
/// equality and hashing are platform-independent; <see cref="ToString"/> returns
/// the OS-native separator form. Compose with the <c>/</c> operator:
/// <code>
/// RelativePath shader = (RelativePath)"assets" / "shaders" / "unlit_textured.vert";
/// </code>
/// </summary>
public sealed class RelativePath : IEquatable<RelativePath>
{
    private readonly string _normalised;

    private RelativePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (Path.IsPathRooted(path))
        {
            throw new ArgumentException($"'{path}' is an absolute path, not relative.", nameof(path));
        }
        _normalised = NormaliseSeparators(path);
    }

    public string FileName => Path.GetFileName(_normalised);

    public string Extension => Path.GetExtension(_normalised);

    public RelativePath? Parent
    {
        get
        {
            var parent = Path.GetDirectoryName(_normalised);
            return string.IsNullOrEmpty(parent) ? null : new RelativePath(parent);
        }
    }

    public static RelativePath operator /(RelativePath left, string right) =>
        new(JoinNormalised(left._normalised, right));

    public static RelativePath operator /(RelativePath left, RelativePath right) =>
        new(JoinNormalised(left._normalised, right._normalised));

    public static implicit operator string(RelativePath path) => path.ToString();

    public static explicit operator RelativePath(string s) => new(s);

    public override string ToString() => _normalised.Replace('/', Path.DirectorySeparatorChar);

    public bool Equals(RelativePath? other) =>
        other is not null && string.Equals(_normalised, other._normalised, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as RelativePath);

    public override int GetHashCode() => _normalised.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(RelativePath? left, RelativePath? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(RelativePath? left, RelativePath? right) => !(left == right);

    internal static string NormaliseSeparators(string raw) => raw.Replace('\\', '/');

    internal static string JoinNormalised(string left, string right)
    {
        var rNorm = NormaliseSeparators(right);
        return string.IsNullOrEmpty(left)
            ? rNorm
            : left.EndsWith('/') ? left + rNorm : left + "/" + rNorm;
    }
}
