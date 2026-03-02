/*{
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Pattern"],
  "DESCRIPTION": "Sci-fi ground grid on unified 3D framework — infinite floor with scan lines",
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
      "LABEL": "Grid Color",
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
      "NAME": "gridSpacing",
      "LABEL": "Grid Spacing",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.25,
      "MAX": 4.0
    },
    {
      "NAME": "gridFadeDistance",
      "LABEL": "Fade Distance",
      "TYPE": "float",
      "DEFAULT": 30.0,
      "MIN": 5.0,
      "MAX": 60.0
    },
    {
      "NAME": "scanSpeed",
      "LABEL": "Scan Speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 4.0
    },
    {
      "NAME": "glowIntensity",
      "LABEL": "Glow Intensity",
      "TYPE": "float",
      "DEFAULT": 0.3,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "lineWidth",
      "LABEL": "Line Width",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.5,
      "MAX": 3.0
    },
    {
      "NAME": "showScanLines",
      "LABEL": "Scan Lines",
      "TYPE": "bool",
      "DEFAULT": true
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

// ============================================================
// END scene3d_include
// ============================================================

// Pseudo-random
float hash(float n) {
    return fract(sin(n) * 43758.5453);
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

    // Ray-plane intersection at y = 0 (ground plane)
    if (abs(cam.rd.y) > 0.0001) {
        float hitT = -cam.ro.y / cam.rd.y;

        if (hitT > 0.0) {
            vec3 hitPos = cam.ro + cam.rd * hitT;

            // Grid lines
            vec2 gridUV = hitPos.xz / gridSpacing;
            float derivScale = hitT * 0.002 * lineWidth;
            vec2 gridDeriv = vec2(derivScale);

            // Major grid
            vec2 grid = abs(fract(gridUV - 0.5) - 0.5);
            vec2 lw = 1.5 * gridDeriv;
            vec2 gridAA = smoothstep(lw, lw + gridDeriv, grid);
            float majorGrid = 1.0 - min(gridAA.x, gridAA.y);

            // Minor grid (4x subdivision)
            vec2 minorGridUV = gridUV * 4.0;
            vec2 minorGrid = abs(fract(minorGridUV - 0.5) - 0.5);
            vec2 minorLw = 0.75 * gridDeriv * 4.0;
            vec2 minorGridDeriv = gridDeriv * 4.0;
            vec2 minorAA = smoothstep(minorLw, minorLw + minorGridDeriv, minorGrid);
            float minGrid = (1.0 - min(minorAA.x, minorAA.y)) * 0.3;

            // Distance fade
            float dist = length(hitPos.xz - cam.ro.xz);
            float fade = 1.0 - smoothstep(gridFadeDistance * 0.5, gridFadeDistance, dist);

            // Combine grid layers
            float gridTotal = majorGrid + minGrid * (1.0 - majorGrid);

            // Animated scan lines on the ground plane
            float scanIntensity = 0.0;
            if (showScanLines) {
                // Radial scan from origin
                float scanZ = fract(t * scanSpeed * 0.25) * gridFadeDistance - gridFadeDistance * 0.5;
                float scanDist = abs(hitPos.z - scanZ);
                scanIntensity += exp(-scanDist * 3.0) * 0.6;

                // X-axis scan
                float scanX = fract(t * scanSpeed * 0.15 + 0.5) * gridFadeDistance - gridFadeDistance * 0.5;
                float scanDistX = abs(hitPos.x - scanX);
                scanIntensity += exp(-scanDistX * 4.0) * 0.4;

                // Boost grid lines where scan crosses
                if (majorGrid > 0.5) {
                    scanIntensity *= 1.5;
                }
            }

            // Glow around major grid lines
            vec2 glowGrid = abs(fract(gridUV - 0.5) - 0.5);
            vec2 glowLw = 6.0 * gridDeriv;
            vec2 glowAA = smoothstep(glowLw, glowLw + gridDeriv * 2.0, glowGrid);
            float glow = (1.0 - min(glowAA.x, glowAA.y)) * glowIntensity * 0.3;

            // Cell pulsing
            float cellX = floor(gridUV.x);
            float cellY = floor(gridUV.y);
            float cellHash = hash(cellX * 17.0 + cellY * 31.0);
            float cellPulse = 0.0;
            if (cellHash > 0.7) {
                cellPulse = 0.3 * (sin(t * 2.0 + cellHash * 6.28318) * 0.5 + 0.5);
            }

            intensity = (gridTotal + cellPulse * majorGrid + glow + scanIntensity) * fade;
        }
    }

    intensity = clamp(intensity, 0.0, 1.0);
    vec3 finalColor = mix(bgColor.rgb, lineColor.rgb, intensity);
    gl_FragColor = vec4(finalColor, 1.0);
}
