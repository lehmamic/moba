#version 330 core

// Skinned classical Phong + textured. Mirrors Madhav Game Programming in C++
// ch.12 Skinned.vert: each vertex skins its position and normal via a weighted
// sum of bone palette matrices. RH Y-up world. Row-vector / row-major
// Silk.NET.Maths matrices uploaded with transpose=false; GLSL sees their
// transpose and multiplies column-vector (see ADR-002 / ADR-012).

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec3 a_normal;
layout(location = 2) in uvec4 a_boneIndices;
layout(location = 3) in vec4 a_boneWeights;
layout(location = 4) in vec2 a_uv;

uniform mat4 u_mvp;
uniform mat4 u_model;
uniform mat4 u_palette[96];

out vec3 v_worldPos;
out vec3 v_normal;
out vec2 v_uv;

void main()
{
    // Weighted sum of palette matrices for this vertex's bone influences.
    mat4 skin = u_palette[a_boneIndices.x] * a_boneWeights.x
              + u_palette[a_boneIndices.y] * a_boneWeights.y
              + u_palette[a_boneIndices.z] * a_boneWeights.z
              + u_palette[a_boneIndices.w] * a_boneWeights.w;

    vec4 skinnedPos = skin * vec4(a_position, 1.0);
    vec4 worldPos = u_model * skinnedPos;
    v_worldPos = worldPos.xyz;

    // Normals: same skin matrix, w=0 to skip translation. Assumes uniform (or
    // no) scale on the model matrix; non-uniform scale would require the
    // inverse-transpose of mat3(u_model * skin).
    vec3 skinnedNormal = (skin * vec4(a_normal, 0.0)).xyz;
    v_normal = mat3(u_model) * skinnedNormal;

    v_uv = a_uv;
    gl_Position = u_mvp * skinnedPos;
}