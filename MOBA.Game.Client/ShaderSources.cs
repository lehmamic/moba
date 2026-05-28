namespace MOBA.Game.Client;

/// <summary>
/// GLSL sources. Embedded for the first skeleton slice; the same content also lives at
/// <c>assets/shaders/unlit_textured.vert|frag</c> for IDE syntax highlighting and a future
/// hot-reload path.
/// </summary>
public static class ShaderSources
{
    public const string UnlitTexturedVertex = """
        #version 330 core
        layout(location = 0) in vec3 a_position;
        layout(location = 1) in vec2 a_uv;
        uniform mat4 u_mvp;
        out vec2 v_uv;
        void main()
        {
            v_uv = a_uv;
            gl_Position = u_mvp * vec4(a_position, 1.0);
        }
        """;

    public const string UnlitTexturedFragment = """
        #version 330 core
        in vec2 v_uv;
        uniform sampler2D u_tex;
        out vec4 frag;
        void main()
        {
            frag = texture(u_tex, v_uv);
        }
        """;
}
