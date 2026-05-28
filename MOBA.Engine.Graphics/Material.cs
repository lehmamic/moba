namespace MOBA.Engine.Graphics;

public sealed class Material
{
    public Material(IShader shader, ITexture? texture = null)
    {
        Shader = shader;
        Texture = texture;
    }

    public IShader Shader { get; }

    public ITexture? Texture { get; }
}
