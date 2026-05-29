#version 330 core

// Classical Phong + textured. RH Y-up world.
// MVP is built in C# as `model * view * projection` (row-vector / row-major Silk.NET.Maths)
// and uploaded with transpose=false → GLSL sees MVP^T and multiplies as column-vector
// (see ADR-002, ADR-012).

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec2 a_uv;
layout(location = 2) in vec3 a_normal;

uniform mat4 u_mvp;
uniform mat4 u_model;

out vec3 v_worldPos;
out vec3 v_normal;
out vec2 v_uv;

void main()
{
    vec4 worldPos = u_model * vec4(a_position, 1.0);
    v_worldPos = worldPos.xyz;
    // Assumes uniform (or no) scale on the model matrix; non-uniform scale would
    // require the inverse-transpose of mat3(u_model).
    v_normal = mat3(u_model) * a_normal;
    v_uv = a_uv;
    gl_Position = u_mvp * vec4(a_position, 1.0);
}
