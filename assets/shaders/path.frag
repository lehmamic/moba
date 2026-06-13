#version 330 core

// Solid cyan for the F3 player-path debug overlay. Contrasts both against
// the magenta F2 navmesh wireframe and against the blue background — when
// both overlays are on at the same time the layers stay distinguishable.

uniform vec3 u_color = vec3(0.2, 1.0, 0.9);

out vec4 frag;

void main()
{
    frag = vec4(u_color, 1.0);
}
