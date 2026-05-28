#version 330 core

in vec2 v_uv;

uniform sampler2D u_tex;

out vec4 frag;

void main()
{
    frag = texture(u_tex, v_uv);
}
