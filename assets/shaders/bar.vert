#version 330 core

// Camera-facing health-bar billboard. Takes a unit XY quad (mesh local coords
// in [-0.5, 0.5]) and constructs a screen-aligned quad of u_size world units
// around the world position carried by u_model's translation. u_cameraRight
// and u_cameraUp are the camera's world-space basis vectors uploaded once
// per pass — the quad rotates with them so it always faces the camera.

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec2 a_uv;
layout(location = 2) in vec3 a_normal;

uniform mat4 u_viewProjection;
uniform mat4 u_model;
uniform vec3 u_cameraRight;
uniform vec3 u_cameraUp;
uniform vec2 u_size;

out vec2 v_uv;

void main()
{
    vec3 worldCenter = (u_model * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    vec3 worldPos = worldCenter
        + (a_position.x * u_size.x) * u_cameraRight
        + (a_position.y * u_size.y) * u_cameraUp;
    gl_Position = u_viewProjection * vec4(worldPos, 1.0);
    v_uv = a_uv;
}
