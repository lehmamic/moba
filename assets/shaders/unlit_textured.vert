#version 330 core

// Unlit + textured. RH Y-up world; MVP is built in C# as
// `model * view * projection` (row-vector / row-major Silk.NET.Maths).
// Uploaded with `transpose=false` → GLSL sees MVP^T and multiplies column-vector:
// `gl_Position = u_mvp * vec4(a_position, 1.0)` is mathematically the same.

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec2 a_uv;

uniform mat4 u_mvp;

out vec2 v_uv;

void main()
{
    v_uv = a_uv;
    gl_Position = u_mvp * vec4(a_position, 1.0);
}
