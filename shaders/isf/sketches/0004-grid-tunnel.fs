/*{
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Pattern"],
  "DESCRIPTION": "Grid tunnel effect - outlined rectangles rushing toward camera like a HUD display",
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
      "NAME": "numRects",
      "LABEL": "Number of Rectangles",
      "TYPE": "float",
      "DEFAULT": 6.0,
      "MIN": 2.0,
      "MAX": 12.0
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
      "NAME": "showDiagonals",
      "LABEL": "Show Diagonals",
      "TYPE": "bool",
      "DEFAULT": true
    },
    {
      "NAME": "showCrosshair",
      "LABEL": "Show Crosshair",
      "TYPE": "bool",
      "DEFAULT": true
    },
    {
      "NAME": "fadeWithDepth",
      "LABEL": "Fade with Depth",
      "TYPE": "bool",
      "DEFAULT": true
    }
  ]
}*/

// Draw a line segment between two points
float lineSegment(vec2 p, vec2 a, vec2 b, float width) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    float d = length(pa - ba * h);
    return 1.0 - smoothstep(0.0, width, d);
}

// Draw outlined rectangle
float rectOutline(vec2 uv, vec2 center, vec2 size, float width) {
    vec2 halfSize = size * 0.5;

    // Four corners
    vec2 tl = center + vec2(-halfSize.x, halfSize.y);
    vec2 tr = center + vec2(halfSize.x, halfSize.y);
    vec2 bl = center + vec2(-halfSize.x, -halfSize.y);
    vec2 br = center + vec2(halfSize.x, -halfSize.y);

    // Four edges
    float top = lineSegment(uv, tl, tr, width);
    float bottom = lineSegment(uv, bl, br, width);
    float left = lineSegment(uv, bl, tl, width);
    float right = lineSegment(uv, br, tr, width);

    return max(max(top, bottom), max(left, right));
}

// Draw static diagonal lines from screen corners to center vanishing point
// Returns intensity and also outputs normalized distance from center (0=center, 1=corner) via depth param
float diagonalLines(vec2 uv, vec2 center, vec2 cornerOffset, float width, out float depth) {
    // Four corners
    vec2 tl = center + vec2(-cornerOffset.x, cornerOffset.y);
    vec2 tr = center + vec2(cornerOffset.x, cornerOffset.y);
    vec2 bl = center + vec2(-cornerOffset.x, -cornerOffset.y);
    vec2 br = center + vec2(cornerOffset.x, -cornerOffset.y);

    // Four diagonal lines from corners to center
    float d1 = lineSegment(uv, center, tl, width);
    float d2 = lineSegment(uv, center, tr, width);
    float d3 = lineSegment(uv, center, bl, width);
    float d4 = lineSegment(uv, center, br, width);

    // Calculate depth based on distance from center (for fading)
    float maxDist = length(cornerOffset);
    depth = length(uv - center) / maxDist;

    return max(max(d1, d2), max(d3, d4));
}

// Draw crosshair
float crosshair(vec2 uv, vec2 center, float size, float width) {
    float h = lineSegment(uv, center - vec2(size, 0.0), center + vec2(size, 0.0), width);
    float v = lineSegment(uv, center - vec2(0.0, size), center + vec2(0.0, size), width);
    return max(h, v);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 center = vec2(0.5, 0.5);

    // Correct for aspect ratio
    float screenAspect = RENDERSIZE.x / RENDERSIZE.y;
    vec2 st = uv;
    st.x = (st.x - 0.5) * screenAspect + 0.5;
    vec2 stCenter = vec2(0.5, 0.5);

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

    // Viewport bounds in aspect-corrected space
    float viewLeft = 0.5 - screenAspect * 0.5;
    float viewRight = 0.5 + screenAspect * 0.5;
    float viewBottom = 0.0;
    float viewTop = 1.0;

    // Perspective projection parameters
    // z ranges from zFar (small on screen) to zNear (large, exiting screen)
    float zFar = 12.0;
    float zNear = 0.4;

    // Base size chosen so rectangle clears viewport when z = zNear
    // At z = zNear, width = baseWidth / zNear, should be > screenAspect
    // Rectangle aspect ratio matches canvas aspect ratio
    float baseWidth = screenAspect * 1.3 * zNear;  // ensures exit at zNear
    float baseHeight = baseWidth / screenAspect;

    // Size range (same for both perspective and flat modes)
    float minWidth = baseWidth / zFar;   // smallest (far)
    float maxWidth = baseWidth / zNear;  // largest (near)
    float minHeight = baseHeight / zFar;
    float maxHeight = baseHeight / zNear;

    float totalIntensity = 0.0;

    // cycleRate: 0=1/4, 1=1/2, 2=1bar, 3=2bars, 4=4bars, 5=8bars
    // Convert to beats per cycle: 1, 2, 4, 8, 16, 32
    float beatsPerCycle = pow(2.0, float(cycleRate));

    // Draw multiple rectangles at different depths
    int n = int(numRects);
    for (int i = 0; i < 12; i++) {
        if (i >= n) break;

        // Phase for this rectangle (0 to 1, wraps around)
        float phase = fract(t / beatsPerCycle + float(i) / numRects);

        // Calculate size based on perspective setting
        // perspective=1: true 1/z perspective (bunched near center, spread at edges)
        // perspective=0: linear/even spacing (same distance between all rectangles)

        // Perspective mode: size = 1/z where z is linear
        float z = mix(zFar, zNear, phase);
        float perspW = baseWidth / z;
        float perspH = baseHeight / z;

        // Flat mode: size varies linearly with phase (even spacing)
        float flatW = mix(minWidth, maxWidth, phase);
        float flatH = mix(minHeight, maxHeight, phase);

        // Blend between flat and perspective
        float w = mix(flatW, perspW, perspective);
        float h = mix(flatH, perspH, perspective);

        // Check if rectangle is still visible (any edge within viewport)
        float halfW = w * 0.5;
        float halfH = h * 0.5;
        float rectLeft = stCenter.x - halfW;
        float rectRight = stCenter.x + halfW;
        float rectBottom = stCenter.y - halfH;
        float rectTop = stCenter.y + halfH;

        // Rectangle is visible if any edge is still inside viewport
        bool leftVisible = rectLeft > viewLeft && rectLeft < viewRight;
        bool rightVisible = rectRight > viewLeft && rectRight < viewRight;
        bool topVisible = rectTop > viewBottom && rectTop < viewTop;
        bool bottomVisible = rectBottom > viewBottom && rectBottom < viewTop;

        // Skip this rectangle if all edges have cleared the viewport
        if (!leftVisible && !rightVisible && !topVisible && !bottomVisible) {
            continue;
        }

        // Depth-based fade: farther = dimmer (z is larger when far)
        // Normalize z to 0-1 range for alpha: closer (small z) = brighter
        float normalizedDepth = (z - zNear) / (zFar - zNear);  // 0 at near, 1 at far
        float alpha = fadeWithDepth ? (1.0 - normalizedDepth) : 1.0;

        // Draw the rectangle outline
        float rect = rectOutline(st, stCenter, vec2(w, h), width);
        totalIntensity += rect * alpha;

    }

    // Draw static diagonal lines from corners to center
    if (showDiagonals) {
        // Corner offset in aspect-corrected space (to screen corners)
        vec2 cornerOffset = vec2(screenAspect * 0.5, 0.5);
        float diagDepth;
        float diag = diagonalLines(st, stCenter, cornerOffset, width * 0.7, diagDepth);

        // Fade with depth: brighter at edges, dimmer toward center
        float diagAlpha = fadeWithDepth ? diagDepth : 1.0;
        totalIntensity += diag * diagAlpha;
    }

    // Draw crosshair at center
    if (showCrosshair) {
        float ch = crosshair(st, stCenter, 0.015, width * 0.75);
        totalIntensity += ch * 0.8;
    }

    // Clamp intensity
    totalIntensity = clamp(totalIntensity, 0.0, 1.0);

    // Final color
    vec3 finalColor = mix(bgColor.rgb, lineColor.rgb, totalIntensity);

    gl_FragColor = vec4(finalColor, 1.0);
}
