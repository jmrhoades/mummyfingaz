// scene3d — Unified 3D Framework — WebGL Vertex Shader
// Fullscreen quad with UV passthrough

attribute vec2 position;
varying vec2 v_uv;

void main() {
    v_uv = position * 0.5 + 0.5;
    gl_Position = vec4(position, 0.0, 1.0);
}
