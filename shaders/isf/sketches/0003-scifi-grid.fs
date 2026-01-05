/*{
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Pattern"],
  "DESCRIPTION": "Sci-fi HUD style hairline grid with animated scan lines",
  "INPUTS": [
    {
      "NAME": "gridColor",
      "LABEL": "Grid Color",
      "TYPE": "color",
      "DEFAULT": [0.0, 0.8, 1.0, 1.0]
    },
    {
      "NAME": "bgColor",
      "LABEL": "Background Color",
      "TYPE": "color",
      "DEFAULT": [0.0, 0.02, 0.05, 1.0]
    },
    {
      "NAME": "majorDivisions",
      "LABEL": "Major Divisions",
      "TYPE": "float",
      "DEFAULT": 8.0,
      "MIN": 2.0,
      "MAX": 24.0
    },
    {
      "NAME": "minorSubdivisions",
      "LABEL": "Minor Subdivisions",
      "TYPE": "float",
      "DEFAULT": 4.0,
      "MIN": 0.0,
      "MAX": 8.0
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
      "NAME": "scanLineSpeed",
      "LABEL": "Scan Speed",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.0,
      "MAX": 2.0
    },
    {
      "NAME": "pulseRate",
      "LABEL": "Pulse Rate",
      "TYPE": "long",
      "DEFAULT": 3,
      "VALUES": [0, 1, 2, 3, 4, 5],
      "LABELS": ["1/16", "1/8", "1/4", "1/2", "1", "Off"]
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
      "NAME": "showCornerMarkers",
      "LABEL": "Corner Markers",
      "TYPE": "bool",
      "DEFAULT": true
    },
    {
      "NAME": "showScanLines",
      "LABEL": "Scan Lines",
      "TYPE": "bool",
      "DEFAULT": true
    }
  ]
}*/

// Pseudo-random function
float hash(float n) {
    return fract(sin(n) * 43758.5453);
}

// Grid line function - returns intensity based on distance to nearest grid line
float gridLine(float coord, float spacing, float width) {
    float halfWidth = width * 0.5;
    float d = abs(mod(coord + halfWidth, spacing) - halfWidth);
    return 1.0 - smoothstep(0.0, width, d);
}

// Soft glow around grid lines
float gridGlow(float coord, float spacing, float width, float glowSize) {
    float halfWidth = width * 0.5;
    float d = abs(mod(coord + halfWidth, spacing) - halfWidth);
    return 1.0 - smoothstep(0.0, width + glowSize, d);
}

// Corner marker L-bracket shape
float cornerMarker(vec2 uv, vec2 corner, float size, float thickness) {
    vec2 d = abs(uv - corner);
    float arm = size * 0.15;  // Length of the L arms
    float t = thickness * 0.5;

    // Horizontal arm
    float h = step(d.x, arm) * step(d.y, t);
    // Vertical arm
    float v = step(d.y, arm) * step(d.x, t);

    return max(h, v);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 px = gl_FragCoord.xy;

    // Correct for aspect ratio
    float aspect = RENDERSIZE.x / RENDERSIZE.y;
    vec2 st = uv;
    st.x *= aspect;

    // Time (beat-synced in Videosync, seconds elsewhere)
    float t;
    #ifdef VIDEOSYNC
      t = BEAT;
    #else
      t = TIME;
    #endif

    // Calculate pulse multiplier (for beat-sync)
    float pulse = 1.0;
    if (pulseRate < 5) {
        float mult = pow(2.0, 2.0 - float(pulseRate));
        pulse = 0.7 + 0.3 * sin(t * mult * 6.28318);
    }

    // Grid spacing in normalized coordinates
    float majorSpacing = aspect / majorDivisions;
    float minorSpacing = (minorSubdivisions > 0.0) ? majorSpacing / minorSubdivisions : majorSpacing;

    // Line widths in pixels, converted to normalized space
    float pxToNorm = 1.0 / RENDERSIZE.y;
    float majorWidth = lineWidth * 1.5 * pxToNorm;
    float minorWidth = lineWidth * 0.75 * pxToNorm;
    float glowWidth = lineWidth * 8.0 * pxToNorm;

    // Calculate grid intensities
    float majorGridH = gridLine(st.x, majorSpacing, majorWidth);
    float majorGridV = gridLine(st.y, 1.0 / majorDivisions, majorWidth);
    float majorGrid = max(majorGridH, majorGridV);

    float minorGrid = 0.0;
    if (minorSubdivisions > 0.0) {
        float minorGridH = gridLine(st.x, minorSpacing, minorWidth);
        float minorGridV = gridLine(st.y, 1.0 / (majorDivisions * minorSubdivisions), minorWidth);
        minorGrid = max(minorGridH, minorGridV) * 0.4;
    }

    // Glow effect for major grid
    float glowH = gridGlow(st.x, majorSpacing, majorWidth, glowWidth);
    float glowV = gridGlow(st.y, 1.0 / majorDivisions, majorWidth, glowWidth);
    float glow = max(glowH, glowV) * glowIntensity * 0.3;

    // Animated scan lines
    float scanIntensity = 0.0;
    if (showScanLines) {
        // Horizontal scan line
        float scanY = fract(t * scanLineSpeed * 0.25);
        float scanDistH = abs(uv.y - scanY);
        float hScan = exp(-scanDistH * 30.0) * 0.8;

        // Vertical scan line (slower, offset phase)
        float scanX = fract(t * scanLineSpeed * 0.15 + 0.5);
        float scanDistV = abs(uv.x - scanX);
        float vScan = exp(-scanDistV * 40.0) * 0.5;

        // Diagonal sweep
        float diag = fract(t * scanLineSpeed * 0.1);
        float diagDist = abs((uv.x + uv.y) * 0.5 - diag);
        float dScan = exp(-diagDist * 25.0) * 0.3;

        scanIntensity = hScan + vScan + dScan;

        // Make scan lines pulse grid lines they cross
        if (majorGrid > 0.5) {
            scanIntensity *= 1.5;
        }
    }

    // Animated grid segment brightness (some cells pulse)
    float cellX = floor(st.x / majorSpacing);
    float cellY = floor(st.y / (1.0 / majorDivisions));
    float cellHash = hash(cellX * 17.0 + cellY * 31.0);
    float cellPulse = 0.0;
    if (cellHash > 0.7) {
        // This cell pulses
        float pulsePhase = cellHash * 6.28318;
        cellPulse = 0.3 * sin(t * 2.0 + pulsePhase) * 0.5 + 0.5;
    }

    // Combine grid layers
    float gridTotal = majorGrid + minorGrid * (1.0 - majorGrid);
    gridTotal = clamp(gridTotal + cellPulse * majorGrid, 0.0, 1.0);

    // Apply pulse modulation to overall grid
    gridTotal *= pulse;

    // Corner markers
    float corners = 0.0;
    if (showCornerMarkers) {
        float markerSize = 0.12;
        float markerThick = lineWidth * 2.0 * pxToNorm;
        float margin = 0.02;

        // Four corners (in aspect-corrected space for x)
        corners += cornerMarker(st, vec2(margin * aspect, margin), markerSize, markerThick);
        corners += cornerMarker(st, vec2(aspect - margin * aspect, margin), markerSize, markerThick);
        corners += cornerMarker(st, vec2(margin * aspect, 1.0 - margin), markerSize, markerThick);
        corners += cornerMarker(st, vec2(aspect - margin * aspect, 1.0 - margin), markerSize, markerThick);

        // Pulse the corners
        corners *= 0.8 + 0.2 * sin(t * 3.0);
    }

    // Compose final color
    vec3 finalColor = bgColor.rgb;

    // Add glow layer (underneath)
    finalColor = mix(finalColor, gridColor.rgb * 0.5, glow);

    // Add minor grid
    finalColor = mix(finalColor, gridColor.rgb * 0.6, minorGrid * pulse);

    // Add major grid
    finalColor = mix(finalColor, gridColor.rgb, majorGrid * pulse);

    // Add scan line effect
    finalColor += gridColor.rgb * scanIntensity;

    // Add corner markers
    finalColor = mix(finalColor, gridColor.rgb, corners);

    // Subtle vignette for depth
    float vignette = 1.0 - length(uv - 0.5) * 0.5;
    finalColor *= vignette;

    // Add subtle noise for texture (very faint)
    float noise = hash(px.x + px.y * RENDERSIZE.x + t * 1000.0) * 0.02;
    finalColor += noise * gridColor.rgb;

    gl_FragColor = vec4(finalColor, 1.0);
}
