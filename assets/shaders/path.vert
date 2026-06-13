#version 330 core

// Unlit / colour-only line shader for the F3 path debug overlay. Identical
// to wireframe.vert today; kept as a separate file so a future per-shader
// vertex feature (per-vertex colour, segment fade, etc.) doesn't need to
// touch the navmesh wireframe.

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec2 a_uv;
layout(location = 2) in vec3 a_normal;

uniform mat4 u_mvp;

void main()
{
    gl_Position = u_mvp * vec4(a_position, 1.0);
}
