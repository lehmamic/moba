#version 330 core

// Solid magenta. Designed for the F2 navmesh debug overlay — the colour is
// loud on purpose so the lines pop against grass and stone textures.

uniform vec3 u_color = vec3(1.0, 0.2, 0.85);

out vec4 frag;

void main()
{
    frag = vec4(u_color, 1.0);
}
