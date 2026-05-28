using Xunit;

namespace MOBA.Utilities.Tests;

public class PathStringExtensionsTests
{
    [Fact]
    public void String_slash_string_produces_relative_path()
    {
        RelativePath path = "assets" / "shaders";
        Assert.Equal((RelativePath)"assets/shaders", path);
    }

    [Fact]
    public void Chained_string_slashes_combine_all_segments()
    {
        RelativePath path = "assets" / "shaders" / "unlit.vert";
        Assert.Equal((RelativePath)"assets/shaders/unlit.vert", path);
    }

    [Fact]
    public void Result_composes_with_absolute_path_root()
    {
        AbsolutePath root = AbsolutePath.AppBaseDirectory;
        RelativePath leaf = "assets" / "shaders" / "unlit.vert";
        Assert.Equal(root / "assets" / "shaders" / "unlit.vert", root / leaf);
    }
}
