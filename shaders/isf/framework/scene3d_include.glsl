// ============================================================
// scene3d_include.glsl — COPY THIS INTO ANY SHADER
// ============================================================
// Unified 3D framework for composable scene layers.
// Paste this block into your ISF fragment shader so that
// all layers share the same camera and projection.
//
// USAGE:
//   1. Add camera ISF INPUTS (see scene3d_inputs.json)
//   2. Paste this include block
//   3. In main(), call scene3d_setup() to get camera state
//   4. Use scene3d_project() / scene3d_line() / scene3d_grid()
//      to render your geometry
//
// EXAMPLE:
//   void main() {
//       Scene3D cam = scene3d_setup(
//           isf_FragNormCoord, RENDERSIZE,
//           camDistance, camHeight, camYaw, camPitch, fov
//       );
//       float intensity = 0.0;
//       // Project 3D point to screen:
//       vec3 s = scene3d_projectPt(vec3(1,0,0), cam);
//       // Draw 3D line:
//       intensity += scene3d_drawLine(cam.uv, vec3(0), vec3(1,0,0), cam, lw);
//       // Ground grid:
//       intensity += scene3d_drawGrid(cam, 1.0, 20.0, 1.5) * 0.6;
//   }
//
// COORDINATE SYSTEM:
//   X = right, Y = up, Z = forward (right-handed)
//   Camera orbits origin at (sin(yaw)*dist, height, cos(yaw)*dist)
//   Camera looks at (0, 0, 0)
// ============================================================

// Camera state — pass this around instead of individual params
struct Scene3D {
    vec2  uv;        // fragment UV (0-1)
    float aspect;    // width / height
    vec3  ro;        // ray origin (camera position)
    vec3  rd;        // ray direction for this fragment
    vec3  fw;        // camera forward (pitched)
    vec3  rt;        // camera right
    vec3  up;        // camera up (pitched)
    float fovScale;  // tan(fov/2)
    float pxSize;    // 1.0 / resolution.y — pixel size in UV
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

    // Pitch rotation around the right axis
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

// ---- Public API ----

// Initialize camera state for this fragment.
// Call once at the top of main().
Scene3D scene3d_setup(vec2 uv, vec2 resolution,
                       float dist, float height,
                       float yaw, float pitch, float fov) {
    Scene3D cam;
    cam.uv      = uv;
    cam.aspect   = resolution.x / resolution.y;
    cam.fovScale = tan(fov * 0.5 * 3.14159);
    cam.pxSize   = 1.0 / resolution.y;

    cam.ro = _s3d_camPos(dist, height, yaw);

    vec3 target = vec3(0.0);
    vec3 fw0, rt0, u0;
    _s3d_lookAt(cam.ro, target, vec3(0.0, 1.0, 0.0), fw0, rt0, u0);

    cam.rd = _s3d_rayDir(uv, cam.aspect, pitch, fw0, rt0, u0, cam.fovScale);
    cam.rt = rt0;

    // Apply pitch to stored basis
    float cp = cos(pitch);
    float sp = sin(pitch);
    cam.fw = fw0 * cp + u0 * sp;
    cam.up = u0 * cp - fw0 * sp;

    return cam;
}

// Project a 3D world point to screen.
// Returns vec3(screenUV.xy, depth). depth < 0 = behind camera.
vec3 scene3d_projectPt(vec3 worldPos, Scene3D cam) {
    vec3 toPoint = worldPos - cam.ro;
    float depth = dot(toPoint, cam.fw);
    if (depth <= 0.0) return vec3(0.0, 0.0, -1.0);

    float sx = dot(toPoint, cam.rt) / (depth * cam.fovScale);
    float sy = dot(toPoint, cam.up) / (depth * cam.fovScale);
    vec2 screenUV = vec2(sx / cam.aspect, sy) * 0.5 + 0.5;

    return vec3(screenUV, depth);
}

// Draw a 3D line segment. Returns anti-aliased intensity (0-1).
// width is in pixel units (will be scaled by cam.pxSize internally).
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

// Ray-plane intersection: horizontal plane at given Y.
// Returns hit distance, or -1 if no hit.
float scene3d_planeHit(Scene3D cam, float planeY) {
    if (abs(cam.rd.y) < 0.0001) return -1.0;
    float t = (planeY - cam.ro.y) / cam.rd.y;
    return t > 0.0 ? t : -1.0;
}

// Get world-space hit position on a horizontal plane.
// Returns vec4(hitPos.xyz, 1.0) if hit, vec4(0,0,0,0) if miss.
vec4 scene3d_planePos(Scene3D cam, float planeY) {
    float t = scene3d_planeHit(cam, planeY);
    if (t < 0.0) return vec4(0.0);
    return vec4(cam.ro + cam.rd * t, 1.0);
}

// Infinite ground grid at y=0.
// spacing: grid cell size in world units
// fadeDistance: how far before grid fades out
// lineW: line thickness multiplier
float scene3d_drawGrid(Scene3D cam, float spacing, float fadeDistance, float lineW) {
    float t = scene3d_planeHit(cam, 0.0);
    if (t < 0.0) return 0.0;

    vec3 hitPos = cam.ro + cam.rd * t;
    vec2 gridUV = hitPos.xz / spacing;

    // Derivative approximation (works in all GLSL versions)
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

// Depth fog: exponential falloff
float scene3d_fog(float depth, float density) {
    return 1.0 - exp(-depth * density);
}

// Depth-based fade: 0.0 at farClip, 1.0 at nearClip
float scene3d_depthFade(float depth, float nearClip, float farClip) {
    return 1.0 - clamp((depth - nearClip) / (farClip - nearClip), 0.0, 1.0);
}

// ============================================================
// END scene3d_include.glsl
// ============================================================
