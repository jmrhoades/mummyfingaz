// scene3d — Unified 3D Framework — WebGL Fragment Shader
// ============================================================
// Pure GLSL ES 1.0 — no ISF dependencies.
// Provides identical camera, projection, and coordinate system
// as scene3d.fs (ISF) and scene3d.gl.frag (desktop GL).
//
// All three versions share the same math so layers composite
// perfectly when rendered to separate textures and blended.
//
// Uniforms are supplied by the host JS application.
// ============================================================

precision highp float;

varying vec2 v_uv;

// Standard uniforms (host must supply)
uniform vec2  u_resolution;   // canvas size in pixels
uniform float u_time;         // elapsed time in seconds
uniform float u_beat;         // beat position (bpm/60 * time)

// Camera uniforms
uniform float u_camDistance;  // default 5.0
uniform float u_camHeight;   // default 2.0
uniform float u_camPitch;    // default 0.3
uniform float u_camYaw;      // default 0.0
uniform float u_fov;         // default 1.0  (radians-ish scale)
uniform float u_zNear;       // default 0.1
uniform float u_zFar;        // default 100.0

// Visual uniforms
uniform float u_gridSize;    // default 1.0
uniform float u_gridFade;    // default 20.0
uniform float u_lineWidth;   // default 1.0
uniform vec4  u_lineColor;   // default (1,1,1,1)
uniform vec4  u_bgColor;     // default (0,0,0,1)
uniform bool  u_showGrid;    // default true
uniform bool  u_showAxes;    // default false

// ============================================================
// FRAMEWORK CORE — identical math across all three versions
// ============================================================

vec3 scene3d_camPos(float dist, float height, float yaw) {
    return vec3(
        sin(yaw) * dist,
        height,
        cos(yaw) * dist
    );
}

void scene3d_lookAt(vec3 eye, vec3 target, vec3 up,
                     out vec3 fw, out vec3 rt, out vec3 u) {
    fw = normalize(target - eye);
    rt = normalize(cross(fw, up));
    u  = cross(rt, fw);
}

vec3 scene3d_rayDir(vec2 uv, float aspect, float pitch,
                     vec3 fw, vec3 rt, vec3 u, float fovScale) {
    vec2 ndc = (uv - 0.5) * 2.0;
    ndc.x *= aspect;
    vec2 screenPos = ndc * fovScale;

    vec3 rd = normalize(fw + rt * screenPos.x + u * screenPos.y);

    // Pitch rotation around the right axis
    float cp = cos(pitch);
    float sp = sin(pitch);
    float rdY = dot(rd, u);
    float rdZ = dot(rd, fw);
    vec3 rdPitched = rt * dot(rd, rt)
                   + u  * (rdY * cp - rdZ * sp)
                   + fw * (rdY * sp + rdZ * cp);

    return normalize(rdPitched);
}

void scene3d_ray(vec2 uv, float aspect,
                  float dist, float height, float yaw, float pitch,
                  float fovScale,
                  out vec3 ro, out vec3 rd) {
    ro = scene3d_camPos(dist, height, yaw);
    vec3 target = vec3(0.0, 0.0, 0.0);
    vec3 fw, rt, u;
    scene3d_lookAt(ro, target, vec3(0.0, 1.0, 0.0), fw, rt, u);
    rd = scene3d_rayDir(uv, aspect, pitch, fw, rt, u, fovScale);
}

vec3 scene3d_project(vec3 worldPos, vec3 camPos, vec3 fw, vec3 rt, vec3 u,
                      float aspect, float fovScale) {
    vec3 toPoint = worldPos - camPos;
    float depth = dot(toPoint, fw);
    if (depth <= 0.0) return vec3(0.0, 0.0, -1.0);

    float screenX = dot(toPoint, rt) / (depth * fovScale);
    float screenY = dot(toPoint, u) / (depth * fovScale);
    vec2 screenUV = vec2(screenX / aspect, screenY) * 0.5 + 0.5;

    return vec3(screenUV, depth);
}

float scene3d_line(vec2 uv, vec3 a3d, vec3 b3d,
                    vec3 camPos, vec3 fw, vec3 rt, vec3 u,
                    float aspect, float fovScale, float width) {
    vec3 a2d = scene3d_project(a3d, camPos, fw, rt, u, aspect, fovScale);
    vec3 b2d = scene3d_project(b3d, camPos, fw, rt, u, aspect, fovScale);

    if (a2d.z < 0.0 || b2d.z < 0.0) return 0.0;

    vec2 pa = uv - a2d.xy;
    vec2 ba = b2d.xy - a2d.xy;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    float d = length(pa - ba * h);

    float avgDepth = (a2d.z + b2d.z) * 0.5;
    float depthWidth = width / max(avgDepth * fovScale, 0.001);

    return 1.0 - smoothstep(0.0, depthWidth, d);
}

float scene3d_planeHit(vec3 ro, vec3 rd, float planeY) {
    if (abs(rd.y) < 0.0001) return -1.0;
    float t = (planeY - ro.y) / rd.y;
    return t > 0.0 ? t : -1.0;
}

// WebGL 1.0 does not have fwidth(), so we approximate
// grid anti-aliasing using a depth-based derivative estimate
float scene3d_grid(vec3 ro, vec3 rd, float spacing, float fadeDistance, float lineW) {
    float t = scene3d_planeHit(ro, rd, 0.0);
    if (t < 0.0) return 0.0;

    vec3 hitPos = ro + rd * t;

    vec2 gridUV = hitPos.xz / spacing;

    // Approximate derivatives from ray distance
    // (fwidth not available in WebGL 1.0 without OES_standard_derivatives)
    float derivScale = t * 0.002;
    vec2 gridDeriv = vec2(derivScale);

    vec2 grid = abs(fract(gridUV - 0.5) - 0.5);
    vec2 lw = lineW * gridDeriv;
    vec2 gridAA = smoothstep(lw, lw + gridDeriv, grid);
    float gridLine = 1.0 - min(gridAA.x, gridAA.y);

    float dist = length(hitPos.xz - ro.xz);
    float fade = 1.0 - smoothstep(fadeDistance * 0.5, fadeDistance, dist);

    return gridLine * fade;
}

float scene3d_fog(float depth, float density) {
    return 1.0 - exp(-depth * density);
}

// ============================================================
// DEMO MAIN — same as ISF version
// ============================================================

void main() {
    vec2 uv = v_uv;
    float aspect = u_resolution.x / u_resolution.y;
    float pxToNorm = 1.0 / u_resolution.y;
    float lw = u_lineWidth * pxToNorm;

    float fovScale = tan(u_fov * 0.5 * 3.14159);

    vec3 ro, rd;
    scene3d_ray(uv, aspect, u_camDistance, u_camHeight, u_camYaw, u_camPitch,
                fovScale, ro, rd);

    vec3 target = vec3(0.0, 0.0, 0.0);
    vec3 fw, rt, u;
    scene3d_lookAt(ro, target, vec3(0.0, 1.0, 0.0), fw, rt, u);

    float cp = cos(u_camPitch);
    float sp = sin(u_camPitch);
    vec3 fw2 = fw * cp + u * sp;
    vec3 u2  = u * cp - fw * sp;

    float intensity = 0.0;

    if (u_showGrid) {
        intensity += scene3d_grid(ro, rd, u_gridSize, u_gridFade, 1.5) * 0.6;
    }

    if (u_showAxes) {
        float axisLen = 3.0;
        intensity += scene3d_line(uv, vec3(0.0), vec3(axisLen, 0.0, 0.0),
                                   ro, fw2, rt, u2, aspect, fovScale, lw * 2.0);
        intensity += scene3d_line(uv, vec3(0.0), vec3(0.0, axisLen, 0.0),
                                   ro, fw2, rt, u2, aspect, fovScale, lw * 2.0);
        intensity += scene3d_line(uv, vec3(0.0), vec3(0.0, 0.0, axisLen),
                                   ro, fw2, rt, u2, aspect, fovScale, lw * 2.0);
    }

    intensity = clamp(intensity, 0.0, 1.0);
    vec3 finalColor = mix(u_bgColor.rgb, u_lineColor.rgb, intensity);
    gl_FragColor = vec4(finalColor, 1.0);
}
