// ============================================================
// scene3d_include.glsl — COPY THIS INTO ANY SHADER
// ============================================================
// Unified 3D framework with infinite tunnel conveyor belt.
// Every shader sits on the same moving conveyor so layers
// composite as one scene — a spaceship cockpit looking out
// into abstract space.
//
// ARCHITECTURE:
//   Camera is fixed (cockpit). The world scrolls toward you.
//   Objects are placed on a repeating conveyor belt using
//   modular arithmetic — no arrays, no allocation, runs forever
//   without memory growth or frame drops.
//
// USAGE:
//   1. Add ISF INPUTS from scene3d_inputs.json
//   2. Paste this include block
//   3. In main():
//
//      Scene3D cam = scene3d_setup(
//          isf_FragNormCoord, RENDERSIZE,
//          camDistance, camHeight, camYaw, camPitch, fov,
//          tunnelSpeed, tunnelDepth, time
//      );
//
//      // Place objects on the conveyor belt:
//      for (int i = 0; i < N; i++) {
//          vec4 slot = scene3d_slot(cam, i, N);
//          // slot.xyz = world position, slot.w = unique ID
//          float id = slot.w;
//          float xPos = (scene3d_hash(id * 7.3) - 0.5) * spread;
//          vec3 objPos = vec3(xPos, 0.0, slot.z);
//          // ... draw object at objPos ...
//      }
//
//      // Scrolling ground grid (cockpit floor):
//      intensity += scene3d_scrollGrid(cam, 1.0, 30.0, 1.5);
//
// COORDINATE SYSTEM:
//   X = right, Y = up, Z = into screen (right-handed)
//   Camera at (0, height, dist) looking toward origin / -Z
//   Tunnel extends from Z=0 (near) to Z=-depth (far)
//   Objects scroll from -depth toward 0 then wrap
//
// PERFORMANCE:
//   - No branching in projection math
//   - Early-out for behind-camera geometry
//   - Conveyor uses fract() — O(1) per slot, no accumulation
//   - mod(scroll, spacing) for grid — prevents float overflow
//   - Safe for 24/7 runtime (float32 precision holds for years)
// ============================================================

// ---- Deterministic hash (framework-provided) ----

float scene3d_hash(float n) {
    return fract(sin(n * 127.1) * 43758.5453);
}

vec2 scene3d_hash2(float n) {
    return vec2(
        fract(sin(n * 127.1) * 43758.5453),
        fract(sin(n * 269.5) * 18397.2741)
    );
}

// ---- Camera + Tunnel state ----

struct Scene3D {
    // Camera
    vec2  uv;        // fragment UV (0-1)
    float aspect;    // width / height
    vec3  ro;        // ray origin (camera position)
    vec3  rd;        // ray direction for this fragment
    vec3  fw;        // camera forward (pitched)
    vec3  rt;        // camera right
    vec3  up;        // camera up (pitched)
    float fovScale;  // tan(fov/2)
    float pxSize;    // 1.0 / resolution.y

    // Tunnel / conveyor belt
    float scroll;    // total scroll distance (time * speed)
    float depth;     // tunnel visible depth (world units)
    float speed;     // scroll speed (world units per time unit)
    float time;      // current time (beats or seconds)
};

// ---- Internal helpers ----

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

// ---- Public API: Setup ----

// Full setup with tunnel parameters.
// tSpeed: conveyor speed in world units per time unit (0 = static scene)
// tDepth: how far the tunnel extends into -Z
// time:   current beat (VIDEOSYNC) or seconds (TIME)
Scene3D scene3d_setup(vec2 uv, vec2 resolution,
                       float dist, float height,
                       float yaw, float pitch, float fov,
                       float tSpeed, float tDepth, float time) {
    Scene3D cam;
    cam.uv       = uv;
    cam.aspect   = resolution.x / resolution.y;
    cam.fovScale = tan(fov * 0.5 * 3.14159);
    cam.pxSize   = 1.0 / resolution.y;

    cam.ro = _s3d_camPos(dist, height, yaw);

    vec3 target = vec3(0.0);
    vec3 fw0, rt0, u0;
    _s3d_lookAt(cam.ro, target, vec3(0.0, 1.0, 0.0), fw0, rt0, u0);

    cam.rd = _s3d_rayDir(uv, cam.aspect, pitch, fw0, rt0, u0, cam.fovScale);
    cam.rt = rt0;

    float cp = cos(pitch);
    float sp = sin(pitch);
    cam.fw = fw0 * cp + u0 * sp;
    cam.up = u0 * cp - fw0 * sp;

    // Tunnel state
    cam.speed  = tSpeed;
    cam.depth  = tDepth;
    cam.time   = time;
    cam.scroll = time * tSpeed;

    return cam;
}

// ---- Public API: Projection ----

// Project a 3D world point to screen.
// Returns vec3(screenUV.xy, depth). depth < 0 = behind camera (culled).
vec3 scene3d_projectPt(vec3 worldPos, Scene3D cam) {
    vec3 toPoint = worldPos - cam.ro;
    float depth = dot(toPoint, cam.fw);
    if (depth <= 0.0) return vec3(0.0, 0.0, -1.0);

    float sx = dot(toPoint, cam.rt) / (depth * cam.fovScale);
    float sy = dot(toPoint, cam.up) / (depth * cam.fovScale);
    vec2 screenUV = vec2(sx / cam.aspect, sy) * 0.5 + 0.5;

    return vec3(screenUV, depth);
}

// ---- Public API: Drawing primitives ----

// Draw a 3D line segment. Returns anti-aliased intensity (0-1).
// widthPx: line width in pixels (auto-scales with depth).
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

// Draw a 3D point (small circle). Returns intensity (0-1).
float scene3d_drawPoint(vec3 worldPos, Scene3D cam, float radiusPx) {
    vec3 s = scene3d_projectPt(worldPos, cam);
    if (s.z < 0.0) return 0.0;

    float d = length(cam.uv - s.xy);
    float r = radiusPx * cam.pxSize / max(s.z * cam.fovScale, 0.001);

    return 1.0 - smoothstep(0.0, r, d);
}

// ---- Public API: Tunnel / Conveyor Belt ----

// Get a slot on the infinite conveyor belt.
// index: which slot (0..count-1), count: total visible slots.
// Returns vec4(x, y, z, uniqueID).
//   z ranges from -depth (far) to 0 (near camera).
//   uniqueID changes every time the slot wraps — hash it for random properties.
//   x and y are 0 — the shader offsets them using hash(id).
//
// PERFORMANCE: Pure arithmetic. O(1) per slot.
// INFINITE: Uses fract() — no accumulation, runs forever.
vec4 scene3d_slot(Scene3D cam, int index, int count) {
    float n = float(count);
    float basePhase = cam.scroll / cam.depth;
    float totalPhase = basePhase + float(index) / n;
    float phase = fract(totalPhase);
    float cycle = floor(totalPhase);

    // Z: map phase 0→1 to z -depth→0
    float z = -cam.depth * (1.0 - phase);

    // Unique ID: changes each wrap so objects look different every pass
    float id = cycle * n + float(index);

    return vec4(0.0, 0.0, z, id);
}

// Slot fade: how bright the slot should be based on depth.
// Returns 1.0 at camera (z=0), 0.0 at far end (z=-depth).
// Use this for depth-based intensity falloff.
float scene3d_slotFade(Scene3D cam, float z) {
    return clamp(1.0 + z / cam.depth, 0.0, 1.0);
}

// ---- Public API: Ground plane ----

// Ray-plane intersection: horizontal plane at given Y.
// Returns hit distance along ray, or -1 if no hit.
float scene3d_planeHit(Scene3D cam, float planeY) {
    if (abs(cam.rd.y) < 0.0001) return -1.0;
    float t = (planeY - cam.ro.y) / cam.rd.y;
    return t > 0.0 ? t : -1.0;
}

// Get world-space hit position on a horizontal plane.
// Returns vec4(hitPos.xyz, 1.0) on hit, vec4(0) on miss.
vec4 scene3d_planePos(Scene3D cam, float planeY) {
    float t = scene3d_planeHit(cam, planeY);
    if (t < 0.0) return vec4(0.0);
    return vec4(cam.ro + cam.rd * t, 1.0);
}

// Static ground grid (no scroll).
float scene3d_drawGrid(Scene3D cam, float spacing, float fadeDistance, float lineW) {
    float t = scene3d_planeHit(cam, 0.0);
    if (t < 0.0) return 0.0;

    vec3 hitPos = cam.ro + cam.rd * t;
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

// Scrolling ground grid — the cockpit floor rushing beneath you.
// Uses mod(scroll, spacing) to prevent float overflow on long runs.
float scene3d_scrollGrid(Scene3D cam, float spacing, float fadeDistance, float lineW) {
    float t = scene3d_planeHit(cam, 0.0);
    if (t < 0.0) return 0.0;

    vec3 hitPos = cam.ro + cam.rd * t;

    // Offset Z by scroll (mod spacing to prevent float overflow)
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

// ---- Public API: Depth utilities ----

// Depth fog: exponential falloff. Returns fog amount (0 = clear, 1 = opaque).
float scene3d_fog(float depth, float density) {
    return 1.0 - exp(-depth * density);
}

// Depth-based fade: 1.0 at nearClip, 0.0 at farClip.
float scene3d_depthFade(float depth, float nearClip, float farClip) {
    return 1.0 - clamp((depth - nearClip) / (farClip - nearClip), 0.0, 1.0);
}

// ============================================================
// END scene3d_include.glsl
// ============================================================
