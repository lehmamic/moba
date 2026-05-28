using Xunit;

namespace MOBA.Utilities.Tests;

public class AbsolutePathTests
{
    [Fact]
    public void Cast_from_relative_string_throws()
    {
        Assert.Throws<ArgumentException>(() => (AbsolutePath)"relative/path");
    }

    [Fact]
    public void Cast_from_absolute_string_returns_typed_path()
    {
        var raw = Path.GetTempPath();
        var typed = (AbsolutePath)raw;
        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(raw)), (string)typed);
    }

    [Fact]
    public void AppBaseDirectory_returns_existing_directory()
    {
        var path = AbsolutePath.AppBaseDirectory;
        Assert.True(path.DirectoryExists);
        Assert.True(Path.IsPathFullyQualified((string)path));
    }

    [Fact]
    public void CurrentDirectory_returns_fully_qualified_path()
    {
        var path = AbsolutePath.CurrentDirectory;
        Assert.True(Path.IsPathFullyQualified((string)path));
    }

    [Fact]
    public void Slash_with_string_appends_segment()
    {
        var root = AbsolutePath.AppBaseDirectory;
        var sub = root / "assets";
        Assert.Equal(Path.Combine((string)root, "assets"), (string)sub);
    }

    [Fact]
    public void Slash_with_relative_path_appends_segments()
    {
        var root = AbsolutePath.AppBaseDirectory;
        var rel = (RelativePath)"assets" / "shaders";
        Assert.Equal(Path.Combine((string)root, "assets", "shaders"), (string)(root / rel));
    }

    [Fact]
    public void Chained_slashes_combine_segments_in_order()
    {
        var root = AbsolutePath.AppBaseDirectory;
        var deep = root / "assets" / "shaders" / "unlit.vert";
        Assert.Equal(Path.Combine((string)root, "assets", "shaders", "unlit.vert"), (string)deep);
    }

    [Fact]
    public void FileName_returns_last_segment()
    {
        var path = AbsolutePath.AppBaseDirectory / "shader.frag";
        Assert.Equal("shader.frag", path.FileName);
    }

    [Fact]
    public void Extension_returns_file_extension()
    {
        var path = AbsolutePath.AppBaseDirectory / "shader.frag";
        Assert.Equal(".frag", path.Extension);
    }

    [Fact]
    public void Parent_returns_containing_directory()
    {
        var root = AbsolutePath.AppBaseDirectory;
        var child = root / "sub";
        Assert.Equal(root, child.Parent);
    }

    [Fact]
    public void Equal_paths_are_equal_and_share_hash_code()
    {
        var a = AbsolutePath.AppBaseDirectory / "foo";
        var b = AbsolutePath.AppBaseDirectory / "foo";
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Different_paths_are_not_equal()
    {
        var a = AbsolutePath.AppBaseDirectory / "foo";
        var b = AbsolutePath.AppBaseDirectory / "bar";
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void Implicit_string_conversion_returns_normalised_form()
    {
        var path = AbsolutePath.AppBaseDirectory / "x";
        string asString = path;
        Assert.Equal(path.ToString(), asString);
    }
}
