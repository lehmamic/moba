#version 330 core

// Health-bar fragment: thin outline rim of uniform world-space thickness on
// every side, then a horizontal fill — u_fillColor up to u_fillRatio,
// u_bgColor beyond. u_outlineWidth is given as a fraction of bar HEIGHT;
// the horizontal rim is scaled by aspect = width / height so the outline
// stays a constant world-space strip instead of stretching with the bar.
// Hard transitions deliberately (no AA) so the bar reads crisply at every
// zoom level.

in vec2 v_uv;

uniform vec3 u_fillColor;
uniform vec3 u_bgColor;
uniform vec3 u_outlineColor;
uniform vec2 u_size;
uniform float u_fillRatio;
uniform float u_outlineWidth;

out vec4 frag;

void main()
{
    float aspect = u_size.x / max(u_size.y, 1e-5);
    float outlineX = u_outlineWidth / aspect;
    bool inOutline = v_uv.x < outlineX
        || v_uv.x > 1.0 - outlineX
        || v_uv.y < u_outlineWidth
        || v_uv.y > 1.0 - u_outlineWidth;
    if (inOutline)
    {
        frag = vec4(u_outlineColor, 1.0);
        return;
    }
    vec3 color = v_uv.x < u_fillRatio ? u_fillColor : u_bgColor;
    frag = vec4(color, 1.0);
}
