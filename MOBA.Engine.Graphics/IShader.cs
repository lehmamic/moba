using Silk.NET.Maths;

namespace MOBA.Engine.Graphics;

public interface IShader : IDisposable
{
    void Use();

    void SetUniform(string name, Matrix4X4<float> value);

    void SetUniform(string name, int value);

    void SetUniform(string name, float value);

    void SetUniform(string name, Vector3D<float> value);
}
