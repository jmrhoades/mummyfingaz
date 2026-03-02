/*{
  "ISFVSN": "2",
  "CATEGORIES": ["Utility"],
  "DESCRIPTION": "Unified 3D scene framework — shared camera, projection, and coordinate system for composable layers",
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
      "NAME": "zNear",
      "LABEL": "Near Clip",
      "TYPE": "float",
      "DEFAULT": 0.1,
      "MIN": 0.01,
      "MAX": 1.0
    },
    {
      "NAME": "zFar",
      "LABEL": "Far Clip",
      "TYPE": "float",
      "DEFAULT": 100.0,
      "MIN": 10.0,
      "MAX": 500.0
    },
    {
      "NAME": "showGrid",
      "LABEL": "Ground Grid",
      "TYPE": "bool",
      "DEFAULT": true
    },
    {
      "NAME": "showAxes",
      "LABEL": "Show Axes",
      "TYPE": "bool",
      "DEFAULT": false
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
      "NAME": "gridSize",
      "LABEL": "Grid Size",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.25,
      "MAX": 4.0
    },
    {
      "NAME": "gridFade",
      "LABEL": "Grid Fade Distance",
      "TYPE": "float",
      "DEFAULT": 20.0,
      "MIN": 5.0,
      "MAX": 50.0
    },
    {
      "NAME": "lineWidth",
      "LABEL": "Line Width",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.5,
      "MAX": 4.0
    }
  ]
}*/

// ============================================================
// UNIFIED 3D FRAMEWORK — scene3d
// ============================================================
// Shared camera, projection, and coordinate system.
// All visualizations must use these functions so that
// layers composite as one scene.
//
// Coordinate system:
//   X = right, Y = up, Z = forward (right-handed)
//   Camera looks down -Z by default
//
// To use in another shader, copy the FRAMEWORK CORE section
// and call scene3d_ray() to get a ray for the current pixel.
// ============================================================

// ---- FRAMEWORK CORE: copy this block into any shader ----

// Camera position derived from distance, height, yaw
vec3 scene3d_camPos(float dist, float height, float yaw) {
    return vec3(
        sin(yaw) * dist,
        height,
        cos(yaw) * dist
    );
}

// Build a look-at view matrix (returns 3 basis vectors)
// eye: camera position, target: look-at point, up: world up
void scene3d_lookAt(vec3 eye, vec3 target, vec3 up,
                     out vec3 fw, out vec3 rt, out vec3 u) {
    fw = normalize(target - eye);
    rt = normalize(cross(fw, up));
    u  = cross(rt, fw);
}

// Generate a ray direction for a given pixel
// uv: normalized coords (0-1), aspect: width/height
// pitch: additional pitch rotation, fovScale: tan(fov/2)
vec3 scene3d_rayDir(vec2 uv, float aspect, float pitch,
                     vec3 fw, vec3 rt, vec3 u, float fovScale) {
    vec2 ndc = (uv - 0.5) * 2.0;
    ndc.x *= aspect;

    // Apply FOV scaling
    vec2 screenPos = ndc * fovScale;

    // Ray in camera space
    vec3 rd = normalize(fw + rt * screenPos.x + u * screenPos.y);

    // Apply pitch rotation around the right axis
    float cp = cos(pitch);
    float sp = sin(pitch);
    vec3 rdPitched = rd;
    float dotFw = dot(rd, fw);
    float dotU  = dot(rd, u);
    rdPitched = rd + fw * (dotFw * (cp - 1.0) - dotU * sp - dotFw)
                   + u  * (dotU  * (cp - 1.0) + dotFw * sp - dotU)
                   + fw * dotFw + u * dotU;

    // Simpler pitch: rotate rd around rt
    float rdY = dot(rd, u);
    float rdZ = dot(rd, fw);
    rdPitched = rt * dot(rd, rt)
              + u  * (rdY * cp - rdZ * sp)
              + fw * (rdY * sp + rdZ * cp);

    return normalize(rdPitched);
}

// Full ray setup: returns camera position and ray direction
// for the current fragment. This is the main entry point.
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

// Project a 3D world point to 2D screen coordinates
// Returns vec3: xy = screen coords (0-1 range), z = depth (distance from camera)
// Returns z < 0 if point is behind camera
vec3 scene3d_project(vec3 worldPos, vec3 camPos, vec3 fw, vec3 rt, vec3 u,
                      float aspect, float fovScale) {
    vec3 toPoint = worldPos - camPos;
    float depth = dot(toPoint, fw);

    if (depth <= 0.0) return vec3(0.0, 0.0, -1.0); // behind camera

    // Project onto camera plane
    float screenX = dot(toPoint, rt) / (depth * fovScale);
    float screenY = dot(toPoint, u) / (depth * fovScale);

    // Convert from NDC (-1..1) to UV (0..1)
    vec2 screenUV = vec2(screenX / aspect, screenY) * 0.5 + 0.5;

    return vec3(screenUV, depth);
}

// Convenience: project with standard camera params
vec3 scene3d_project(vec3 worldPos, float dist, float height, float yaw,
                      float pitch, float aspect, float fovScale) {
    vec3 camPos = scene3d_camPos(dist, height, yaw);
    vec3 target = vec3(0.0, 0.0, 0.0);
    vec3 fw, rt, u;
    scene3d_lookAt(camPos, target, vec3(0.0, 1.0, 0.0), fw, rt, u);

    // Apply pitch to forward and up
    float cp = cos(pitch);
    float sp = sin(pitch);
    vec3 fw2 = fw * cp + u * sp;
    vec3 u2  = u * cp - fw * sp;

    return scene3d_project(worldPos, camPos, fw2, rt, u2, aspect, fovScale);
}

// Draw a 3D line segment projected to screen
// Returns intensity (0-1) for anti-aliased line
float scene3d_line(vec2 uv, vec3 a3d, vec3 b3d,
                    vec3 camPos, vec3 fw, vec3 rt, vec3 u,
                    float aspect, float fovScale, float width) {
    // Project both endpoints
    vec3 a2d = scene3d_project(a3d, camPos, fw, rt, u, aspect, fovScale);
    vec3 b2d = scene3d_project(b3d, camPos, fw, rt, u, aspect, fovScale);

    // Skip if either point is behind camera
    if (a2d.z < 0.0 || b2d.z < 0.0) return 0.0;

    // 2D line segment distance
    vec2 pa = uv - a2d.xy;
    vec2 ba = b2d.xy - a2d.xy;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    float d = length(pa - ba * h);

    // Depth-adjusted width (thinner when farther)
    float avgDepth = (a2d.z + b2d.z) * 0.5;
    float depthWidth = width / max(avgDepth * fovScale, 0.001);

    return 1.0 - smoothstep(0.0, depthWidth, d);
}

// Ray-plane intersection: y = planeY
// Returns distance along ray, or -1 if no hit
float scene3d_planeHit(vec3 ro, vec3 rd, float planeY) {
    if (abs(rd.y) < 0.0001) return -1.0; // parallel
    float t = (planeY - ro.y) / rd.y;
    return t > 0.0 ? t : -1.0;
}

// Infinite ground grid at y=0
// Returns intensity (0-1) with distance fade
float scene3d_grid(vec3 ro, vec3 rd, float spacing, float fadeDistance, float width) {
    float t = scene3d_planeHit(ro, rd, 0.0);
    if (t < 0.0) return 0.0;

    vec3 hitPos = ro + rd * t;

    // Grid lines via fract
    vec2 gridUV = hitPos.xz / spacing;
    vec2 gridDeriv = fwidth(gridUV);
    vec2 grid = abs(fract(gridUV - 0.5) - 0.5);
    vec2 lineWidth = width * gridDeriv;
    vec2 gridAA = smoothstep(lineWidth, lineWidth + gridDeriv, grid);
    float gridLine = 1.0 - min(gridAA.x, gridAA.y);

    // Distance fade
    float dist = length(hitPos.xz - ro.xz);
    float fade = 1.0 - smoothstep(fadeDistance * 0.5, fadeDistance, dist);

    return gridLine * fade;
}

// Depth fog: exponential falloff
float scene3d_fog(float depth, float density) {
    return 1.0 - exp(-depth * density);
}

// ---- END FRAMEWORK CORE ----


// ============================================================
// DEMO: Ground grid + axes to verify the framework
// ============================================================

void main() {
    vec2 uv = isf_FragNormCoord;
    float aspect = RENDERSIZE.x / RENDERSIZE.y;

    // Time
    float t;
    #ifdef VIDEOSYNC
        t = BEAT;
    #else
        t = TIME;
    #endif

    // Pixel size for line width
    float pxToNorm = 1.0 / RENDERSIZE.y;
    float lw = lineWidth * pxToNorm;

    // Setup camera ray
    vec3 ro, rd;
    float fovScale = tan(fov * 0.5 * 3.14159);
    scene3d_ray(uv, aspect, camDistance, camHeight, camYaw, camPitch, fovScale, ro, rd);

    // Camera basis vectors for projection
    vec3 target = vec3(0.0, 0.0, 0.0);
    vec3 fw, rt, u;
    scene3d_lookAt(ro, target, vec3(0.0, 1.0, 0.0), fw, rt, u);

    // Apply pitch
    float cp = cos(camPitch);
    float sp = sin(camPitch);
    vec3 fw2 = fw * cp + u * sp;
    vec3 u2  = u * cp - fw * sp;

    float intensity = 0.0;

    // Ground grid
    if (showGrid) {
        intensity += scene3d_grid(ro, rd, gridSize, gridFade, 1.5) * 0.6;
    }

    // Coordinate axes
    if (showAxes) {
        float axisLen = 3.0;

        // X axis (red would be here, but we're monochrome — thick line)
        intensity += scene3d_line(uv, vec3(0.0), vec3(axisLen, 0.0, 0.0),
                                   ro, fw2, rt, u2, aspect, fovScale, lw * 2.0);

        // Y axis
        intensity += scene3d_line(uv, vec3(0.0), vec3(0.0, axisLen, 0.0),
                                   ro, fw2, rt, u2, aspect, fovScale, lw * 2.0);

        // Z axis
        intensity += scene3d_line(uv, vec3(0.0), vec3(0.0, 0.0, axisLen),
                                   ro, fw2, rt, u2, aspect, fovScale, lw * 2.0);
    }

    intensity = clamp(intensity, 0.0, 1.0);

    vec3 finalColor = mix(bgColor.rgb, lineColor.rgb, intensity);
    gl_FragColor = vec4(finalColor, 1.0);
}
