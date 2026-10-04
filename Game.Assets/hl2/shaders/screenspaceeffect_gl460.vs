#version 460

layout(location = 0) in vec3 v_Position;
layout(location = 10) in vec2 v_TexCoord;

out vec2 vs_BaseTexCoord;

void main()
{
    gl_Position = vec4(v_Position, 1.0);
    vs_BaseTexCoord = vec2(v_TexCoord.x, 1.0 - v_TexCoord.y);
}
