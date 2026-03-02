/*{
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Pattern"],
  "DESCRIPTION": "Wireframe pyramids on the infinite tunnel conveyor belt — spaceship viewport layer",
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
      "NAME": "tunnelSpeed",
      "LABEL": "Tunnel Speed",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "tunnelDepth",
      "LABEL": "Tunnel Depth",
      "TYPE": "float",
      "DEFAULT": 50.0,
      "MIN": 10.0,
      "MAX": 100.0
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
      "DEFAULT": 0.5,
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
      "NAME": "showGrid",
      "LABEL": "Scrolling Grid",
      "TYPE": "bool",
      "DEFAULT": true
    }
  ]
}*/

// ============================================================
// scene3d_include.glsl — UNIFIED 3D FRAMEWORK
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
// END scene3d_include
// ============================================================


// Draw wireframe pyramid at a world-space base position
float drawPyramid(vec3 base, float sz, float ht, float angle, Scene3D cam, float lw) {
    float ca = cos(angle);
    float sa = sin(angle);

    vec2 lc0 = vec2(-sz, -sz);
    vec2 lc1 = vec2( sz, -sz);
    vec2 lc2 = vec2( sz,  sz);
    vec2 lc3 = vec2(-sz,  sz);

    vec3 c0 = base + vec3(lc0.x * ca - lc0.y * sa, 0.0, lc0.x * sa + lc0.y * ca);
    vec3 c1 = base + vec3(lc1.x * ca - lc1.y * sa, 0.0, lc1.x * sa + lc1.y * ca);
    vec3 c2 = base + vec3(lc2.x * ca - lc2.y * sa, 0.0, lc2.x * sa + lc2.y * ca);
    vec3 c3 = base + vec3(lc3.x * ca - lc3.y * sa, 0.0, lc3.x * sa + lc3.y * ca);

    vec3 apex = base + vec3(0.0, ht, 0.0);

    float i = 0.0;
    // Base edges
    i = max(i, scene3d_drawLine(cam.uv, c0, c1, cam, lw));
    i = max(i, scene3d_drawLine(cam.uv, c1, c2, cam, lw));
    i = max(i, scene3d_drawLine(cam.uv, c2, c3, cam, lw));
    i = max(i, scene3d_drawLine(cam.uv, c3, c0, cam, lw));
    // Apex edges
    i = max(i, scene3d_drawLine(cam.uv, c0, apex, cam, lw));
    i = max(i, scene3d_drawLine(cam.uv, c1, apex, cam, lw));
    i = max(i, scene3d_drawLine(cam.uv, c2, apex, cam, lw));
    i = max(i, scene3d_drawLine(cam.uv, c3, apex, cam, lw));

    return i;
}


void main() {
    vec2 uv = isf_FragNormCoord;

    float t;
    #ifdef VIDEOSYNC
        t = BEAT;
    #else
        t = TIME;
    #endif

    // Setup camera + tunnel
    Scene3D cam = scene3d_setup(uv, RENDERSIZE,
                                 camDistance, camHeight, camYaw, camPitch, fov,
                                 tunnelSpeed, tunnelDepth, t);

    float intensity = 0.0;

    // Scrolling cockpit floor grid
    if (showGrid) {
        intensity += scene3d_scrollGrid(cam, 1.0, 30.0, 1.5) * 0.3;
    }

    // Pyramids on the conveyor belt
    int n = int(numPyramids);
    for (int i = 0; i < 16; i++) {
        if (i >= n) break;

        // Get slot position + unique ID from the conveyor
        vec4 slot = scene3d_slot(cam, i, n);
        float id = slot.w;
        float fade = scene3d_slotFade(cam, slot.z);

        // Randomize from ID — different every wrap, no memory
        float xPos = (scene3d_hash(id * 7.3) - 0.5) * spreadX * 2.0;
        float sz = pyramidSize * 0.5 * (0.7 + scene3d_hash(id * 23.1) * 0.6);
        float ht = sz * 1.7;
        float angle = t * rotationSpeed * 3.14159 + scene3d_hash(id * 13.7) * 6.28318;

        vec3 base = vec3(xPos, 0.0, slot.z);

        float pyr = drawPyramid(base, sz, ht, angle, cam, lineWidth);
        intensity = max(intensity, pyr * fade);
    }

    intensity = clamp(intensity, 0.0, 1.0);
    vec3 finalColor = mix(bgColor.rgb, lineColor.rgb, intensity);
    gl_FragColor = vec4(finalColor, 1.0);
}
