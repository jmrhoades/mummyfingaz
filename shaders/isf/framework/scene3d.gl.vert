// scene3d — Unified 3D Framework — Desktop GL Vertex Shader
// GLSL 330 core — fullscreen quad

#version 330 core

layout(location = 0) in vec2 position;
out vec2 v_uv;

void main() {
    v_uv = position * 0.5 + 0.5;
    gl_Position = vec4(position, 0.0, 1.0);
}
