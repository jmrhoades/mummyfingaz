/*{
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Pattern"],
  "DESCRIPTION": "Wireframe pyramids on the unified 3D scene framework — composable layer",
  "CREDIT": "Mummyfingaz",
  "INPUTS": [
    {
      "NAME": "camDistance",
      "LABEL": "Camera Distance",
      "TYPE": "float",
      "DEFAULT": 5.0,
      "MIN": 1.0,
      "MAX": 20.0
    },
    {
      "NAME": "camHeight",
      "LABEL": "Camera Height",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": -5.0,
      "MAX": 10.0
    },
    {
      "NAME": "camPitch",
      "LABEL": "Camera Pitch",
      "TYPE": "float",
      "DEFAULT": 0.3,
      "MIN": -1.5708,
      "MAX": 1.5708
    },
    {
      "NAME": "camYaw",
      "LABEL": "Camera Yaw",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -3.14159,
      "MAX": 3.14159
    },
    {
      "NAME": "fov",
      "LABEL": "Field of View",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.2,
      "MAX": 3.0
    },
    {
      "NAME": "lineColor",
      "LABEL": "Line Color",
      "TYPE": "color",
      "DEFAULT": [1.0, 1.0, 1.0, 1.0]
    },
    {
      "NAME": "bgColor",
      "LABEL": "Background Color",
      "TYPE": "color",
      "DEFAULT": [0.0, 0.0, 0.0, 1.0]
    },
    {
      "NAME": "cycleRate",
      "LABEL": "Cycle Rate",
      "TYPE": "long",
      "DEFAULT": 2,
      "VALUES": [0, 1, 2, 3, 4, 5],
      "LABELS": ["1/4", "1/2", "1 Bar", "2 Bars", "4 Bars", "8 Bars"]
    },
    {
      "NAME": "numPyramids",
      "LABEL": "Number of Pyramids",
      "TYPE": "float",
      "DEFAULT": 8.0,
      "MIN": 1.0,
      "MAX": 16.0
    },
    {
      "NAME": "lineWidth",
      "LABEL": "Line Width",
      "TYPE": "float",
      "DEFAULT": 1.5,
      "MIN": 0.5,
      "MAX": 4.0
    },
    {
      "NAME": "pyramidSize",
      "LABEL": "Pyramid Size",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 4.0
    },
    {
      "NAME": "rotationSpeed",
      "LABEL": "Rotation Speed",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 2.0
    },
    {
      "NAME": "spreadX",
      "LABEL": "X Spread",
      "TYPE": "float",
      "DEFAULT": 4.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "roadLength",
      "LABEL": "Road Length",
      "TYPE": "float",
      "DEFAULT": 30.0,
      "MIN": 10.0,
      "MAX": 60.0
    },
    {
      "NAME": "fadeWithDepth",
      "LABEL": "Fade with Depth",
      "TYPE": "bool",
      "DEFAULT": true
    },
    {
      "NAME": "showGrid",
      "LABEL": "Ground Grid",
      "TYPE": "bool",
      "DEFAULT": false
    }
  ]
}*/

// ============================================================
// scene3d_include.glsl — UNIFIED 3D FRAMEWORK
// ============================================================

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
                       float yaw, float pitch, float fovIn) {
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

float scene3d_drawGrid(Scene3D cam, float spacing, float fadeDistance, float lineW) {
    if (abs(cam.rd.y) < 0.0001) return 0.0;
    float t = -cam.ro.y / cam.rd.y;
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

float scene3d_depthFade(float depth, float nearClip, float farClip) {
    return 1.0 - clamp((depth - nearClip) / (farClip - nearClip), 0.0, 1.0);
}

// ============================================================
// END scene3d_include
// ============================================================

// Pseudo-random hash
float hash(float n) {
    return fract(sin(n * 12.9898) * 43758.5453);
}

// Draw a wireframe pyramid in 3D world space
// base: center of base on ground (y=0), sz: base half-width, ht: apex height
// angle: Y-axis rotation, cam: Scene3D, lw: line width in pixels
float drawPyramid(vec3 base, float sz, float ht, float angle, Scene3D cam, float lw) {
    // Four base corners in local XZ, rotated around Y
    float ca = cos(angle);
    float sa = sin(angle);

    vec3 corners[4];
    // Local corners before rotation
    vec2 lc0 = vec2(-sz, -sz);
    vec2 lc1 = vec2( sz, -sz);
    vec2 lc2 = vec2( sz,  sz);
    vec2 lc3 = vec2(-sz,  sz);

    // Rotate and offset to world
    corners[0] = base + vec3(lc0.x * ca - lc0.y * sa, 0.0, lc0.x * sa + lc0.y * ca);
    corners[1] = base + vec3(lc1.x * ca - lc1.y * sa, 0.0, lc1.x * sa + lc1.y * ca);
    corners[2] = base + vec3(lc2.x * ca - lc2.y * sa, 0.0, lc2.x * sa + lc2.y * ca);
    corners[3] = base + vec3(lc3.x * ca - lc3.y * sa, 0.0, lc3.x * sa + lc3.y * ca);

    vec3 apex = base + vec3(0.0, ht, 0.0);

    float intensity = 0.0;

    // Base edges
    intensity = max(intensity, scene3d_drawLine(cam.uv, corners[0], corners[1], cam, lw));
    intensity = max(intensity, scene3d_drawLine(cam.uv, corners[1], corners[2], cam, lw));
    intensity = max(intensity, scene3d_drawLine(cam.uv, corners[2], corners[3], cam, lw));
    intensity = max(intensity, scene3d_drawLine(cam.uv, corners[3], corners[0], cam, lw));

    // Apex edges
    intensity = max(intensity, scene3d_drawLine(cam.uv, corners[0], apex, cam, lw));
    intensity = max(intensity, scene3d_drawLine(cam.uv, corners[1], apex, cam, lw));
    intensity = max(intensity, scene3d_drawLine(cam.uv, corners[2], apex, cam, lw));
    intensity = max(intensity, scene3d_drawLine(cam.uv, corners[3], apex, cam, lw));

    return intensity;
}


void main() {
    vec2 uv = isf_FragNormCoord;

    // Time
    float t;
    #ifdef VIDEOSYNC
        t = BEAT;
    #else
        t = TIME;
    #endif

    // Setup unified 3D camera
    Scene3D cam = scene3d_setup(uv, RENDERSIZE,
                                 camDistance, camHeight, camYaw, camPitch, fov);

    float intensity = 0.0;

    // Optional ground grid
    if (showGrid) {
        intensity += scene3d_drawGrid(cam, 1.0, 30.0, 1.5) * 0.3;
    }

    // Cycle timing
    float beatsPerCycle = pow(2.0, float(cycleRate));
    float numPyr = floor(numPyramids + 0.5);
    int n = int(numPyr);

    // Draw pyramids traveling down the -Z axis
    for (int i = 0; i < 16; i++) {
        if (i >= n) break;

        float totalPhase = t / beatsPerCycle + float(i) / numPyr;
        float phase = fract(totalPhase);
        float cycleCount = floor(totalPhase);
        float pyramidID = cycleCount * 100.0 + float(i);

        // World-space Z position: travel from far to near along -Z
        float z = mix(-roadLength, 2.0, phase);

        // Random X offset
        float randX = (hash(pyramidID * 7.3) * 2.0 - 1.0) * spreadX;

        // Pyramid size (with optional random variation)
        float sz = pyramidSize * 0.5 * (0.7 + hash(pyramidID * 23.1) * 0.6);
        float ht = sz * 1.7;

        // Y-axis rotation
        float angle = t * rotationSpeed * 3.14159 + hash(pyramidID * 13.7) * 6.28318;

        // Base center on the ground plane
        vec3 base = vec3(randX, 0.0, z);

        // Draw
        float pyr = drawPyramid(base, sz, ht, angle, cam, lineWidth);

        // Depth fade
        if (fadeWithDepth) {
            vec3 projected = scene3d_projectPt(base, cam);
            float fade = scene3d_depthFade(projected.z, 1.0, roadLength);
            pyr *= fade;
        }

        intensity = max(intensity, pyr);
    }

    intensity = clamp(intensity, 0.0, 1.0);
    vec3 finalColor = mix(bgColor.rgb, lineColor.rgb, intensity);
    gl_FragColor = vec4(finalColor, 1.0);
}
