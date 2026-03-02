/*{
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Pattern"],
  "DESCRIPTION": "Infinite rectangle tunnel on the conveyor belt — spaceship corridor viewport",
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
      "NAME": "numRects",
      "LABEL": "Number of Rectangles",
      "TYPE": "float",
      "DEFAULT": 8.0,
      "MIN": 2.0,
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
      "NAME": "tunnelWidth",
      "LABEL": "Tunnel Width",
      "TYPE": "float",
      "DEFAULT": 4.0,
      "MIN": 1.0,
      "MAX": 10.0
    },
    {
      "NAME": "tunnelHeight",
      "LABEL": "Tunnel Height",
      "TYPE": "float",
      "DEFAULT": 3.0,
      "MIN": 1.0,
      "MAX": 8.0
    },
    {
      "NAME": "showRails",
      "LABEL": "Show Rails",
      "TYPE": "bool",
      "DEFAULT": true
    },
    {
      "NAME": "showGrid",
      "LABEL": "Scrolling Grid",
      "TYPE": "bool",
      "DEFAULT": false
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


void main() {
    vec2 uv = isf_FragNormCoord;

    float t;
    #ifdef VIDEOSYNC
        t = BEAT;
    #else
        t = TIME;
    #endif

    Scene3D cam = scene3d_setup(uv, RENDERSIZE,
                                 camDistance, camHeight, camYaw, camPitch, fov,
                                 tunnelSpeed, tunnelDepth, t);

    float intensity = 0.0;

    // Scrolling floor grid
    if (showGrid) {
        intensity += scene3d_scrollGrid(cam, 1.0, 30.0, 1.5) * 0.3;
    }

    float hw = tunnelWidth * 0.5;
    float hh = tunnelHeight * 0.5;
    float centerY = tunnelHeight * 0.5;
    int n = int(numRects);

    // Rectangle frames on the conveyor belt
    for (int i = 0; i < 16; i++) {
        if (i >= n) break;

        vec4 slot = scene3d_slot(cam, i, n);
        float fade = scene3d_slotFade(cam, slot.z);

        // Rectangle center in world space — all at same XY, riding the belt
        vec3 center = vec3(0.0, centerY, slot.z);

        // Four corners
        vec3 tl = center + vec3(-hw,  hh, 0.0);
        vec3 tr = center + vec3( hw,  hh, 0.0);
        vec3 bl = center + vec3(-hw, -hh, 0.0);
        vec3 br = center + vec3( hw, -hh, 0.0);

        float rect = 0.0;
        rect = max(rect, scene3d_drawLine(cam.uv, tl, tr, cam, lineWidth));
        rect = max(rect, scene3d_drawLine(cam.uv, tr, br, cam, lineWidth));
        rect = max(rect, scene3d_drawLine(cam.uv, br, bl, cam, lineWidth));
        rect = max(rect, scene3d_drawLine(cam.uv, bl, tl, cam, lineWidth));

        intensity += rect * fade;
    }

    // Rail lines along tunnel edges (connect near to far)
    if (showRails) {
        float nearZ = 0.0;
        float farZ = -tunnelDepth;

        intensity += scene3d_drawLine(cam.uv,
            vec3(-hw, 0.0, nearZ), vec3(-hw, 0.0, farZ),
            cam, lineWidth * 0.5) * 0.4;
        intensity += scene3d_drawLine(cam.uv,
            vec3( hw, 0.0, nearZ), vec3( hw, 0.0, farZ),
            cam, lineWidth * 0.5) * 0.4;
        intensity += scene3d_drawLine(cam.uv,
            vec3(-hw, tunnelHeight, nearZ), vec3(-hw, tunnelHeight, farZ),
            cam, lineWidth * 0.5) * 0.4;
        intensity += scene3d_drawLine(cam.uv,
            vec3( hw, tunnelHeight, nearZ), vec3( hw, tunnelHeight, farZ),
            cam, lineWidth * 0.5) * 0.4;
    }

    intensity = clamp(intensity, 0.0, 1.0);
    vec3 finalColor = mix(bgColor.rgb, lineColor.rgb, intensity);
    gl_FragColor = vec4(finalColor, 1.0);
}
