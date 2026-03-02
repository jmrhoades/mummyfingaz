// scene3d — Unified 3D Framework — WebGL Fragment Shader
// ============================================================
// GLSL ES 1.0 — identical tunnel/camera math as ISF and GL.
// Uniforms supplied by host JS application.
// ============================================================

precision highp float;

varying vec2 v_uv;

// Standard uniforms
uniform vec2  u_resolution;
uniform float u_time;
uniform float u_beat;

// Camera uniforms
uniform float u_camDistance;  // default 5.0
uniform float u_camHeight;   // default 2.0
uniform float u_camPitch;    // default 0.3
uniform float u_camYaw;      // default 0.0
uniform float u_fov;         // default 1.0

// Tunnel uniforms
uniform float u_tunnelSpeed; // default 2.0
uniform float u_tunnelDepth; // default 50.0

// Visual uniforms
uniform float u_gridSize;    // default 1.0
uniform float u_gridFade;    // default 30.0
uniform float u_lineWidth;   // default 1.0
uniform vec4  u_lineColor;   // default (1,1,1,1)
uniform vec4  u_bgColor;     // default (0,0,0,1)
uniform bool  u_showGrid;    // default true
uniform bool  u_showAxes;    // default false
uniform bool  u_showMarkers; // default true

// ============================================================
// FRAMEWORK CORE — identical math across ISF / WebGL / GL
// ============================================================

float scene3d_hash(float n) {
    return fract(sin(n * 127.1) * 43758.5453);
}

struct Scene3D {
    vec2  uv;
    float aspect;
    vec3  ro;
    vec3  rd;
    vec3  fw;
    vec3  rt;
    vec3  up;
    float fovScale;
    float pxSize;
    float scroll;
    float depth;
    float speed;
    float time;
};

vec3 _s3d_camPos(float dist, float height, float yaw) {
    return vec3(sin(yaw) * dist, height, cos(yaw) * dist);
}

void _s3d_lookAt(vec3 eye, vec3 target, vec3 worldUp,
                  out vec3 fw, out vec3 rt, out vec3 u) {
    fw = normalize(target - eye);
    rt = normalize(cross(fw, worldUp));
    u  = cross(rt, fw);
}

vec3 _s3d_rayDir(vec2 uv, float aspect, float pitch,
                  vec3 fw, vec3 rt, vec3 u, float fovScale) {
    vec2 ndc = (uv - 0.5) * 2.0;
    ndc.x *= aspect;
    vec2 sp = ndc * fovScale;
    vec3 rd = normalize(fw + rt * sp.x + u * sp.y);
    float cp = cos(pitch);
    float sn = sin(pitch);
    float rdY = dot(rd, u);
    float rdZ = dot(rd, fw);
    return normalize(
        rt * dot(rd, rt) +
        u  * (rdY * cp - rdZ * sn) +
        fw * (rdY * sn + rdZ * cp)
    );
}

Scene3D scene3d_setup(vec2 uv, vec2 resolution,
                       float dist, float height,
                       float yaw, float pitch, float fovIn,
                       float tSpeed, float tDepth, float time) {
    Scene3D cam;
    cam.uv       = uv;
    cam.aspect   = resolution.x / resolution.y;
    cam.fovScale = tan(fovIn * 0.5 * 3.14159);
    cam.pxSize   = 1.0 / resolution.y;
    cam.ro = _s3d_camPos(dist, height, yaw);
    vec3 fw0, rt0, u0;
    _s3d_lookAt(cam.ro, vec3(0.0), vec3(0.0, 1.0, 0.0), fw0, rt0, u0);
    cam.rd = _s3d_rayDir(uv, cam.aspect, pitch, fw0, rt0, u0, cam.fovScale);
    cam.rt = rt0;
    float cp = cos(pitch);
    float sp = sin(pitch);
    cam.fw = fw0 * cp + u0 * sp;
    cam.up = u0 * cp - fw0 * sp;
    cam.speed  = tSpeed;
    cam.depth  = tDepth;
    cam.time   = time;
    cam.scroll = time * tSpeed;
    return cam;
}

vec3 scene3d_projectPt(vec3 worldPos, Scene3D cam) {
    vec3 toPoint = worldPos - cam.ro;
    float depth = dot(toPoint, cam.fw);
    if (depth <= 0.0) return vec3(0.0, 0.0, -1.0);
    float sx = dot(toPoint, cam.rt) / (depth * cam.fovScale);
    float sy = dot(toPoint, cam.up) / (depth * cam.fovScale);
    vec2 screenUV = vec2(sx / cam.aspect, sy) * 0.5 + 0.5;
    return vec3(screenUV, depth);
}

float scene3d_drawLine(vec2 uv, vec3 a3d, vec3 b3d, Scene3D cam, float widthPx) {
    vec3 a2d = scene3d_projectPt(a3d, cam);
    vec3 b2d = scene3d_projectPt(b3d, cam);
    if (a2d.z < 0.0 || b2d.z < 0.0) return 0.0;
    vec2 pa = uv - a2d.xy;
    vec2 ba = b2d.xy - a2d.xy;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    float d = length(pa - ba * h);
    float avgDepth = (a2d.z + b2d.z) * 0.5;
    float w = widthPx * cam.pxSize / max(avgDepth * cam.fovScale, 0.001);
    return 1.0 - smoothstep(0.0, w, d);
}

vec4 scene3d_slot(Scene3D cam, int index, int count) {
    float n = float(count);
    float basePhase = cam.scroll / cam.depth;
    float totalPhase = basePhase + float(index) / n;
    float phase = fract(totalPhase);
    float cycle = floor(totalPhase);
    float z = -cam.depth * (1.0 - phase);
    float id = cycle * n + float(index);
    return vec4(0.0, 0.0, z, id);
}

float scene3d_slotFade(Scene3D cam, float z) {
    return clamp(1.0 + z / cam.depth, 0.0, 1.0);
}

float scene3d_planeHit(Scene3D cam, float planeY) {
    if (abs(cam.rd.y) < 0.0001) return -1.0;
    float t = (planeY - cam.ro.y) / cam.rd.y;
    return t > 0.0 ? t : -1.0;
}

float scene3d_scrollGrid(Scene3D cam, float spacing, float fadeDistance, float lineW) {
    float t = scene3d_planeHit(cam, 0.0);
    if (t < 0.0) return 0.0;
    vec3 hitPos = cam.ro + cam.rd * t;
    float scrollMod = mod(cam.scroll, spacing * 256.0);
    hitPos.z -= scrollMod;
    vec2 gridUV = hitPos.xz / spacing;
    float derivScale = t * 0.002;
    vec2 gridDeriv = vec2(derivScale);
    vec2 grid = abs(fract(gridUV - 0.5) - 0.5);
    vec2 lw = lineW * gridDeriv;
    vec2 gridAA = smoothstep(lw, lw + gridDeriv, grid);
    float gridLine = 1.0 - min(gridAA.x, gridAA.y);
    float dist = length(hitPos.xz - cam.ro.xz);
    float fade = 1.0 - smoothstep(fadeDistance * 0.5, fadeDistance, dist);
    return gridLine * fade;
}

float scene3d_depthFade(float depth, float nearClip, float farClip) {
    return 1.0 - clamp((depth - nearClip) / (farClip - nearClip), 0.0, 1.0);
}

// ============================================================
// DEMO MAIN
// ============================================================

void main() {
    vec2 uv = v_uv;

    Scene3D cam = scene3d_setup(uv, u_resolution,
                                 u_camDistance, u_camHeight, u_camYaw, u_camPitch, u_fov,
                                 u_tunnelSpeed, u_tunnelDepth, u_time);

    float intensity = 0.0;

    if (u_showGrid) {
        intensity += scene3d_scrollGrid(cam, u_gridSize, u_gridFade, 1.5) * 0.6;
    }

    if (u_showAxes) {
        float axisLen = 3.0;
        intensity += scene3d_drawLine(uv, vec3(0.0), vec3(axisLen, 0.0, 0.0),
                                       cam, u_lineWidth * 2.0);
        intensity += scene3d_drawLine(uv, vec3(0.0), vec3(0.0, axisLen, 0.0),
                                       cam, u_lineWidth * 2.0);
        intensity += scene3d_drawLine(uv, vec3(0.0), vec3(0.0, 0.0, -axisLen),
                                       cam, u_lineWidth * 2.0);
    }

    if (u_showMarkers) {
        for (int i = 0; i < 10; i++) {
            vec4 slot = scene3d_slot(cam, i, 10);
            float id = slot.w;
            float fade = scene3d_slotFade(cam, slot.z);

            float xPos = (scene3d_hash(id * 7.3) - 0.5) * 8.0;
            float yPos = scene3d_hash(id * 13.1) * 2.0;
            vec3 center = vec3(xPos, yPos, slot.z);

            float sz = 0.3 + scene3d_hash(id * 41.7) * 0.4;
            float cross_val = 0.0;
            cross_val = max(cross_val, scene3d_drawLine(uv,
                center + vec3(-sz, 0.0, 0.0), center + vec3(sz, 0.0, 0.0),
                cam, u_lineWidth));
            cross_val = max(cross_val, scene3d_drawLine(uv,
                center + vec3(0.0, -sz, 0.0), center + vec3(0.0, sz, 0.0),
                cam, u_lineWidth));

            intensity = max(intensity, cross_val * fade);
        }
    }

    intensity = clamp(intensity, 0.0, 1.0);
    vec3 finalColor = mix(u_bgColor.rgb, u_lineColor.rgb, intensity);
    gl_FragColor = vec4(finalColor, 1.0);
}
