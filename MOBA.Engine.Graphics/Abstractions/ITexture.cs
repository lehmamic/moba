namespace MOBA.Engine.Graphics.Abstractions;

public interface ITexture : IDisposable
{
    int Width { get; }

    int Height { get; }
}
