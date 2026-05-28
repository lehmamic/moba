using Xunit;

namespace MOBA.Utilities.Tests;

public class RelativePathTests
{
    [Fact]
    public void Cast_from_absolute_string_throws()
    {
        Assert.Throws<ArgumentException>(() => (RelativePath)Path.GetTempPath());
    }

    [Fact]
    public void Cast_from_relative_string_returns_typed_path()
    {
        var typed = (RelativePath)"assets/shaders";
        Assert.Equal((RelativePath)"assets/shaders", typed);
    }

    [Fact]
    public void Slash_with_string_appends_segment()
    {
        var path = (RelativePath)"assets" / "shaders";
        Assert.Equal((RelativePath)"assets/shaders", path);
    }

    [Fact]
    public void Slash_with_relative_path_appends_segments()
    {
        var a = (RelativePath)"assets";
        var b = (RelativePath)"shaders";
        Assert.Equal((RelativePath)"assets/shaders", a / b);
    }

    [Fact]
    public void Chained_slashes_combine_segments_in_order()
    {
        var path = (RelativePath)"assets" / "shaders" / "unlit.vert";
        Assert.Equal((RelativePath)"assets/shaders/unlit.vert", path);
    }

    [Fact]
    public void Backslashes_in_input_are_normalised_to_forward_slashes()
    {
        var path = (RelativePath)"assets\\shaders";
        Assert.Equal((RelativePath)"assets/shaders", path);
    }

    [Fact]
    public void ToString_uses_OS_native_separator()
    {
        var path = (RelativePath)"assets" / "shaders" / "unlit.vert";
        Assert.Equal(Path.Combine("assets", "shaders", "unlit.vert"), path.ToString());
    }

    [Fact]
    public void FileName_returns_last_segment()
    {
        var path = (RelativePath)"assets" / "shaders" / "shader.frag";
        Assert.Equal("shader.frag", path.FileName);
    }

    [Fact]
    public void Extension_returns_file_extension()
    {
        var path = (RelativePath)"file.txt";
        Assert.Equal(".txt", path.Extension);
    }

    [Fact]
    public void Parent_returns_containing_directory()
    {
        var path = (RelativePath)"assets" / "shaders" / "shader.frag";
        Assert.Equal((RelativePath)"assets/shaders", path.Parent);
    }

    [Fact]
    public void Parent_of_top_level_segment_is_null()
    {
        var path = (RelativePath)"file.txt";
        Assert.Null(path.Parent);
    }

    [Fact]
    public void Equal_paths_are_equal_and_share_hash_code()
    {
        var a = (RelativePath)"assets" / "shaders";
        var b = (RelativePath)"assets" / "shaders";
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Different_paths_are_not_equal()
    {
        var a = (RelativePath)"assets" / "shaders";
        var b = (RelativePath)"assets" / "textures";
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void Implicit_string_conversion_returns_normalised_form()
    {
        var path = (RelativePath)"assets" / "shaders";
        string asString = path;
        Assert.Equal(path.ToString(), asString);
    }
}
