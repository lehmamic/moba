#version 330 core

// Unlit / colour-only line shader for the F2 navmesh wireframe overlay. Reads
// vertex position only — UV (location 1) and Normal (location 2) are declared
// so the shared Vertex layout fits, but the values are unused. MVP is the same
// `model * viewProjection` matrix every other shader gets.

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec2 a_uv;
layout(location = 2) in vec3 a_normal;

uniform mat4 u_mvp;

void main()
{
    gl_Position = u_mvp * vec4(a_position, 1.0);
}
