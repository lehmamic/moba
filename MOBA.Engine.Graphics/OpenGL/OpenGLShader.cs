using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace MOBA.Engine.Graphics.OpenGL;

internal sealed class OpenGLShader : IShader
{
    private readonly GL _gl;
    private readonly uint _program;
    private readonly Dictionary<string, int> _uniformLocations = [];

    public OpenGLShader(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;

        var vert = Compile(ShaderType.VertexShader, vertexSource);
        var frag = Compile(ShaderType.FragmentShader, fragmentSource);

        _program = _gl.CreateProgram();
        _gl.AttachShader(_program, vert);
        _gl.AttachShader(_program, frag);
        _gl.LinkProgram(_program);

        _gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out var linkStatus);
        if (linkStatus == 0)
        {
            var log = _gl.GetProgramInfoLog(_program);
            throw new InvalidOperationException("Shader program link failed: " + log);
        }

        _gl.DetachShader(_program, vert);
        _gl.DetachShader(_program, frag);
        _gl.DeleteShader(vert);
        _gl.DeleteShader(frag);
    }

    private uint Compile(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
        if (status == 0)
        {
            var log = _gl.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"{type} compile failed: " + log);
        }
        return shader;
    }

    public void Use() => _gl.UseProgram(_program);

    private int GetLocation(string name)
    {
        if (!_uniformLocations.TryGetValue(name, out var loc))
        {
            loc = _gl.GetUniformLocation(_program, name);
            _uniformLocations[name] = loc;
        }
        return loc;
    }

    public void SetUniform(string name, Matrix4X4<float> value)
    {
        // Silk.NET.Maths Matrix4X4<float> is row-major in memory.
        // We upload with transpose=false; GLSL reads it as column-major and therefore
        // sees the transpose of our C# matrix. Because we use row-vector convention in C#
        // (model * view * projection), this matches GLSL's column-vector form
        // `gl_Position = u_mvp * vec4(pos, 1.0)`.
        Span<float> data =
        [
            value.M11, value.M12, value.M13, value.M14,
            value.M21, value.M22, value.M23, value.M24,
            value.M31, value.M32, value.M33, value.M34,
            value.M41, value.M42, value.M43, value.M44,
        ];
        _gl.UniformMatrix4(GetLocation(name), 1, false, data);
    }

    public void SetUniform(string name, int value) => _gl.Uniform1(GetLocation(name), value);

    public void SetUniform(string name, Vector3D<float> value) =>
        _gl.Uniform3(GetLocation(name), value.X, value.Y, value.Z);

    public void Dispose() => _gl.DeleteProgram(_program);
}
