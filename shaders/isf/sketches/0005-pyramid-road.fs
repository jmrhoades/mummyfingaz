/*{
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Pattern"],
  "DESCRIPTION": "Wireframe pyramids rushing toward camera on an endless road",
  "INPUTS": [
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
      "DEFAULT": 1.0,
      "MIN": 0.5,
      "MAX": 4.0
    },
    {
      "NAME": "perspective",
      "LABEL": "Perspective",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 1.0
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
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 3.0
    },
    {
      "NAME": "randomSize",
      "LABEL": "Random Size",
      "TYPE": "bool",
      "DEFAULT": false
    },
    {
      "NAME": "fadeWithDepth",
      "LABEL": "Fade with Depth",
      "TYPE": "bool",
      "DEFAULT": true
    },
    {
      "NAME": "opaque",
      "LABEL": "Opaque",
      "TYPE": "bool",
      "DEFAULT": false
    }
  ]
}*/

// Pseudo-random hash function
float hash(float n) {
    return fract(sin(n * 12.9898) * 43758.5453);
}

// 2D rotation
vec2 rotate2D(vec2 p, float angle) {
    float c = cos(angle);
    float s = sin(angle);
    return vec2(p.x * c - p.y * s, p.x * s + p.y * c);
}

// Check if point is inside triangle using barycentric coordinates
float triangleFill(vec2 p, vec2 a, vec2 b, vec2 c) {
    vec2 v0 = c - a;
    vec2 v1 = b - a;
    vec2 v2 = p - a;

    float dot00 = dot(v0, v0);
    float dot01 = dot(v0, v1);
    float dot02 = dot(v0, v2);
    float dot11 = dot(v1, v1);
    float dot12 = dot(v1, v2);

    float invDenom = 1.0 / (dot00 * dot11 - dot01 * dot01);
    float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
    float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

    return (u >= 0.0 && v >= 0.0 && (u + v) <= 1.0) ? 1.0 : 0.0;
}

// Check if triangle is front-facing (counter-clockwise winding)
bool isFrontFacing(vec2 a, vec2 b, vec2 c) {
    // 2D cross product: positive = CCW = front-facing
    return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) > 0.0;
}

// Check if point is inside quadrilateral (two triangles)
float quadFill(vec2 p, vec2 a, vec2 b, vec2 c, vec2 d) {
    float t1 = triangleFill(p, a, b, c);
    float t2 = triangleFill(p, a, c, d);
    return max(t1, t2);
}

// Draw a line segment between two points
float lineSegment(vec2 p, vec2 a, vec2 b, float width) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    float d = length(pa - ba * h);
    return 1.0 - smoothstep(0.0, width, d);
}

// Draw a wireframe pyramid
// baseCenter: center of the base on the ground plane
// baseSize: width/depth of the square base
// height: height of pyramid apex above base
// angle: rotation angle around Y axis (affects how we see the base)
// fill: output parameter for solid fill (1.0 if inside pyramid)
// opaqueMode: if true, only draw front-facing edges
float pyramid(vec2 uv, vec2 baseCenter, float baseSize, float height, float width, float angle, bool opaqueMode, out float fill) {
    float halfBase = baseSize * 0.5;

    // Base corners in local 3D space (x, z), y is up
    // We rotate around Y axis, then project to 2D
    vec2 cornerFL = vec2(-halfBase, -halfBase);  // front-left
    vec2 cornerFR = vec2(halfBase, -halfBase);   // front-right
    vec2 cornerBL = vec2(-halfBase, halfBase);   // back-left
    vec2 cornerBR = vec2(halfBase, halfBase);    // back-right

    // Rotate corners around Y axis
    cornerFL = rotate2D(cornerFL, angle);
    cornerFR = rotate2D(cornerFR, angle);
    cornerBL = rotate2D(cornerBL, angle);
    cornerBR = rotate2D(cornerBR, angle);

    // Project to 2D screen (simple orthographic with depth hint)
    float depthScale = 0.3;  // how much Z affects Y position
    vec2 baseFrontLeft = baseCenter + vec2(cornerFL.x, cornerFL.y * depthScale);
    vec2 baseFrontRight = baseCenter + vec2(cornerFR.x, cornerFR.y * depthScale);
    vec2 baseBackLeft = baseCenter + vec2(cornerBL.x, cornerBL.y * depthScale);
    vec2 baseBackRight = baseCenter + vec2(cornerBR.x, cornerBR.y * depthScale);

    // Apex (centered above base)
    vec2 apex = baseCenter + vec2(0.0, height);

    // Determine which faces are front-facing
    bool frontFace = isFrontFacing(baseFrontLeft, baseFrontRight, apex);   // front
    bool rightFace = isFrontFacing(baseFrontRight, baseBackRight, apex);   // right
    bool backFace = isFrontFacing(baseBackRight, baseBackLeft, apex);      // back
    bool leftFace = isFrontFacing(baseBackLeft, baseFrontLeft, apex);      // left

    // Calculate fill (all faces for occlusion)
    float face1 = triangleFill(uv, baseFrontLeft, baseFrontRight, apex);
    float face2 = triangleFill(uv, baseFrontRight, baseBackRight, apex);
    float face3 = triangleFill(uv, baseBackRight, baseBackLeft, apex);
    float face4 = triangleFill(uv, baseBackLeft, baseFrontLeft, apex);
    float baseFill = quadFill(uv, baseFrontLeft, baseFrontRight, baseBackRight, baseBackLeft);
    fill = max(max(max(face1, face2), max(face3, face4)), baseFill);

    // Draw edges - in opaque mode, only draw edges of front-facing faces
    float wireframe = 0.0;

    // Base edges (only if adjacent face is front-facing, or not in opaque mode)
    if (!opaqueMode || frontFace)
        wireframe = max(wireframe, lineSegment(uv, baseFrontLeft, baseFrontRight, width));
    if (!opaqueMode || leftFace)
        wireframe = max(wireframe, lineSegment(uv, baseFrontLeft, baseBackLeft, width));
    if (!opaqueMode || rightFace)
        wireframe = max(wireframe, lineSegment(uv, baseFrontRight, baseBackRight, width));
    if (!opaqueMode || backFace)
        wireframe = max(wireframe, lineSegment(uv, baseBackLeft, baseBackRight, width));

    // Apex edges (visible if either adjacent face is front-facing)
    if (!opaqueMode || frontFace || leftFace)
        wireframe = max(wireframe, lineSegment(uv, baseFrontLeft, apex, width));
    if (!opaqueMode || frontFace || rightFace)
        wireframe = max(wireframe, lineSegment(uv, baseFrontRight, apex, width));
    if (!opaqueMode || backFace || leftFace)
        wireframe = max(wireframe, lineSegment(uv, baseBackLeft, apex, width));
    if (!opaqueMode || backFace || rightFace)
        wireframe = max(wireframe, lineSegment(uv, baseBackRight, apex, width));

    return wireframe;
}

void main() {
    vec2 uv = isf_FragNormCoord;

    // Aspect ratio correction
    float screenAspect = RENDERSIZE.x / RENDERSIZE.y;
    vec2 st = uv;
    st.x = (st.x - 0.5) * screenAspect + 0.5;

    // Vanishing point / horizon at center
    vec2 vanishingPoint = vec2(0.5, 0.5);

    // Time
    float t;
    #ifdef VIDEOSYNC
      t = BEAT;
    #else
      t = TIME;
    #endif

    // Line width in normalized coordinates
    float pxToNorm = 1.0 / RENDERSIZE.y;
    float width = lineWidth * pxToNorm;

    // Perspective projection parameters
    float zFar = 12.0;
    float zNear = 0.15;  // Smaller zNear so pyramids grow larger before wrapping

    // Rectangle frame sizing - use a reference zNear for sizing to match grid-tunnel scale
    float zNearRef = 0.4;
    float baseWidth = screenAspect * 1.3 * zNearRef;
    float baseHeight = baseWidth / screenAspect;

    // Size range for flat mode (SAME as grid-tunnel)
    float minWidth = baseWidth / zFar;
    float maxWidth = baseWidth / zNear;
    float minHeight = baseHeight / zFar;
    float maxHeight = baseHeight / zNear;

    float totalIntensity = 0.0;

    // cycleRate: 0=1/4, 1=1/2, 2=1bar, 3=2bars, 4=4bars, 5=8bars
    float beatsPerCycle = pow(2.0, float(cycleRate));

    int n = int(numPyramids);

    // Compute draw order mathematically (no sorting needed)
    // Phases are evenly spaced: fract(base + i/n)
    // The pyramid with smallest phase (farthest) is the one that just wrapped
    float basePhase = fract(t / beatsPerCycle);
    // Starting index: which pyramid has the smallest phase (just wrapped past 0)
    int startIdx = int(mod(ceil((1.0 - basePhase) * numPyramids), numPyramids));

    // Draw pyramids in sorted order (back to front)
    for (int di = 0; di < 16; di++) {
        if (di >= n) break;

        // Calculate actual pyramid index from draw order
        int i = int(mod(float(startIdx + di), numPyramids));

        // Calculate this pyramid's total phase (not wrapped) to get stable cycle count
        float totalPhase = t / beatsPerCycle + float(i) / numPyramids;
        float phase = fract(totalPhase);

        // Cycle count - increments each time THIS pyramid wraps (not others)
        // This ensures random properties stay stable throughout the pyramid's journey
        float cycleCount = floor(totalPhase);

        // Unique ID for this specific pyramid instance (changes only when THIS pyramid wraps)
        float pyramidID = cycleCount * 100.0 + float(i);

        // Random X offset - unique per pyramid instance, stable during its journey
        float randX = hash(pyramidID * 7.3) * 2.0 - 1.0;
        randX *= spreadX;

        // Calculate depth (SAME as grid-tunnel)
        float z = mix(zFar, zNear, phase);

        // Calculate frame size at this depth (SAME as grid-tunnel)
        float perspW = baseWidth / z;
        float perspH = baseHeight / z;
        float flatW = mix(minWidth, maxWidth, phase);
        float flatH = mix(minHeight, maxHeight, phase);
        float frameW = mix(flatW, perspW, perspective);
        float frameH = mix(flatH, perspH, perspective);

        // Pyramid size proportional to frame
        float pyrSize = frameH * 0.3;

        // Calculate screen position
        // X: random position within the frame width
        float offsetX = randX * (frameW * 0.5 - pyrSize * 0.5);

        // Y: pyramid base sits on bottom edge of the frame (floor)
        // Frame is centered at (0.5, 0.5), bottom edge is at 0.5 - frameH/2
        float floorY = vanishingPoint.y - frameH * 0.5;

        vec2 baseCenter = vec2(vanishingPoint.x + offsetX, floorY);

        // Calculate actual pyramid dimensions for visibility check
        float sizeMultiplier = randomSize ? (0.5 + hash(pyramidID * 23.1) * 0.5) : 1.0;
        float pyrBase = pyrSize * pyramidSize * sizeMultiplier;
        float pyrHeight = pyrBase * 0.85;
        float pyrHalfWidth = pyrBase * 0.5;

        // Pyramid bounds
        float pyrLeft = baseCenter.x - pyrHalfWidth;
        float pyrRight = baseCenter.x + pyrHalfWidth;
        float pyrBottom = baseCenter.y;  // base sits on floor
        float pyrTop = baseCenter.y + pyrHeight;  // apex

        // Viewport bounds in aspect-corrected space
        float viewLeft = 0.5 - screenAspect * 0.5;
        float viewRight = 0.5 + screenAspect * 0.5;
        float viewBottom = 0.0;
        float viewTop = 1.0;

        // Skip only if pyramid has FULLY exited the viewport
        bool fullyOffLeft = pyrRight < viewLeft;
        bool fullyOffRight = pyrLeft > viewRight;
        bool fullyOffBottom = pyrTop < viewBottom;
        bool fullyOffTop = pyrBottom > viewTop;

        if (fullyOffLeft || fullyOffRight || fullyOffBottom || fullyOffTop) continue;

        // Depth-based fade
        float normalizedDepth = (z - zNear) / (zFar - zNear);
        float alpha = fadeWithDepth ? (1.0 - normalizedDepth) : 1.0;

        // Calculate rotation angle for this pyramid (unique starting angle per instance)
        float pyrAngle = t * rotationSpeed * 3.14159 + hash(pyramidID * 13.7) * 6.28318;

        // Draw pyramid
        float pyrFill;
        float pyr = pyramid(st, baseCenter, pyrBase, pyrHeight, width, pyrAngle, opaque, pyrFill);

        // If opaque, fill with background color (subtract from intensity)
        if (opaque && pyrFill > 0.0) {
            totalIntensity *= (1.0 - pyrFill);
        }

        totalIntensity += pyr * alpha;
    }

    // Clamp intensity
    totalIntensity = clamp(totalIntensity, 0.0, 1.0);

    // Final color
    vec3 finalColor = mix(bgColor.rgb, lineColor.rgb, totalIntensity);

    gl_FragColor = vec4(finalColor, 1.0);
}
