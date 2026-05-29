#version 330 core

// Skinned-mesh fragment shader. Identical to phong_textured.frag — the skinning
// happens in the vertex stage; the lighting math doesn't care.

in vec3 v_worldPos;
in vec3 v_normal;
in vec2 v_uv;

uniform sampler2D u_tex;
uniform vec3 u_viewPos;
uniform vec3 u_lightDir;
uniform vec3 u_lightColor;
uniform vec3 u_ambientColor;
uniform float u_specularStrength;
uniform float u_shininess;

out vec4 frag;

void main()
{
    vec4 baseColor = texture(u_tex, v_uv);

    vec3 N = normalize(v_normal);
    vec3 L = normalize(u_lightDir);
    vec3 V = normalize(u_viewPos - v_worldPos);
    vec3 R = reflect(-L, N);

    vec3 ambient = u_ambientColor;
    vec3 diffuse = max(dot(N, L), 0.0) * u_lightColor;
    vec3 specular = u_specularStrength * pow(max(dot(V, R), 0.0), u_shininess) * u_lightColor;

    vec3 lit = (ambient + diffuse + specular) * baseColor.rgb;
    frag = vec4(lit, baseColor.a);
}