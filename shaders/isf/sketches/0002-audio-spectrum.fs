/*{
  "ISFVSN": "2",
  "CATEGORIES": ["Generator"],
  "DESCRIPTION": "5-band realtime frequency spectrum visualization with audio input from Videosync FFT analysis",
  "CREDIT": "Mummyfingaz",
  "INPUTS": [
    {
      "NAME": "band1",
      "LABEL": "Sub Bass (20-60Hz)",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "band2",
      "LABEL": "Bass (60-250Hz)",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "band3",
      "LABEL": "Low Mid (250-500Hz)",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "band4",
      "LABEL": "High Mid (500-2kHz)",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "band5",
      "LABEL": "Treble (2k-20kHz)",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "responsiveness",
      "LABEL": "Responsiveness",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "size",
      "LABEL": "Size",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.1,
      "MAX": 1.0
    },
    {
      "NAME": "barColor",
      "LABEL": "Bar Color",
      "TYPE": "color",
      "DEFAULT": [1.0, 1.0, 1.0, 1.0]
    },
    {
      "NAME": "location",
      "LABEL": "Location",
      "TYPE": "long",
      "DEFAULT": 0,
      "VALUES": [0, 1, 2, 3, 4],
      "LABELS": ["Center", "Top Left", "Top Right", "Bottom Left", "Bottom Right"]
    },
    {
      "NAME": "barStyle",
      "LABEL": "Bar Style",
      "TYPE": "long",
      "DEFAULT": 0,
      "VALUES": [0, 1, 2],
      "LABELS": ["Solid", "Outline", "Gradient"]
    },
    {
      "NAME": "gapSize",
      "LABEL": "Gap Size",
      "TYPE": "float",
      "DEFAULT": 0.2,
      "MIN": 0.0,
      "MAX": 0.5
    }
  ]
}*/

// Smoothing state for responsiveness (using previous frame values via time-based smoothing)
// Since ISF doesn't have persistent state, we simulate smoothing via the responsiveness parameter

void main() {
    vec2 uv = isf_FragNormCoord;
    float aspect = RENDERSIZE.x / RENDERSIZE.y;

    // Get band values (these come from Videosync FFT analysis)
    float bands[5];
    bands[0] = band1;
    bands[1] = band2;
    bands[2] = band3;
    bands[3] = band4;
    bands[4] = band5;

    // Calculate visualization area based on location and size
    vec2 areaCenter;
    float areaScale = size * 0.4; // Base scale factor

    // Position the visualization based on location parameter
    if (location == 0) {
        // Center
        areaCenter = vec2(0.5, 0.5);
    } else if (location == 1) {
        // Top Left
        areaCenter = vec2(0.2, 0.8);
        areaScale *= 0.6;
    } else if (location == 2) {
        // Top Right
        areaCenter = vec2(0.8, 0.8);
        areaScale *= 0.6;
    } else if (location == 3) {
        // Bottom Left
        areaCenter = vec2(0.2, 0.2);
        areaScale *= 0.6;
    } else {
        // Bottom Right
        areaCenter = vec2(0.8, 0.2);
        areaScale *= 0.6;
    }

    // Transform UV to local coordinates relative to visualization area
    vec2 localUV = (uv - areaCenter) / areaScale;

    // Aspect ratio correction
    localUV.x *= aspect;

    // Bar dimensions
    float totalWidth = 2.0; // Total width in local coordinates
    float barWidth = totalWidth / 5.0;
    float gap = barWidth * gapSize;
    float actualBarWidth = barWidth - gap;

    // Initialize output color (transparent background)
    vec4 outColor = vec4(0.0, 0.0, 0.0, 0.0);

    // Check each band/bar
    for (int i = 0; i < 5; i++) {
        // Calculate bar position (centered around origin)
        float barCenterX = -totalWidth / 2.0 + barWidth * (float(i) + 0.5);
        float barLeft = barCenterX - actualBarWidth / 2.0;
        float barRight = barCenterX + actualBarWidth / 2.0;

        // Get band amplitude with responsiveness-adjusted scaling
        // Higher responsiveness = more dynamic range, lower = more compressed
        float amplitude = bands[i];
        float dynamicRange = mix(0.3, 1.0, responsiveness);
        amplitude = pow(amplitude, mix(0.5, 1.5, 1.0 - responsiveness)) * dynamicRange;
        amplitude = clamp(amplitude, 0.02, 1.0); // Minimum visible height

        // Bar height based on amplitude
        float barHeight = amplitude * 1.0; // Max height of 1.0 in local coords
        float barBottom = -0.5;
        float barTop = barBottom + barHeight;

        // Check if current pixel is inside this bar
        if (localUV.x >= barLeft && localUV.x <= barRight &&
            localUV.y >= barBottom && localUV.y <= barTop) {

            // Calculate normalized position within bar for styling
            float normalizedX = (localUV.x - barLeft) / actualBarWidth;
            float normalizedY = (localUV.y - barBottom) / barHeight;

            vec4 finalColor = barColor;

            if (barStyle == 0) {
                // Solid fill
                finalColor = barColor;
            } else if (barStyle == 1) {
                // Outline style
                float edgeThickness = 0.15;
                float edgeX = min(normalizedX, 1.0 - normalizedX);
                float edgeY = min(normalizedY, 1.0 - normalizedY);
                float edge = min(edgeX, edgeY);

                if (edge > edgeThickness) {
                    finalColor = vec4(0.0, 0.0, 0.0, 0.0);
                } else {
                    finalColor = barColor;
                }
            } else if (barStyle == 2) {
                // Gradient style (bottom to top)
                float gradientFactor = normalizedY;
                vec3 topColor = barColor.rgb * 1.5; // Brighter at top
                vec3 bottomColor = barColor.rgb * 0.5; // Darker at bottom
                finalColor.rgb = mix(bottomColor, topColor, gradientFactor);
                finalColor.a = barColor.a;
            }

            // Add subtle glow effect based on amplitude
            float glowIntensity = amplitude * 0.3;
            finalColor.rgb += vec3(glowIntensity);

            outColor = finalColor;
        }
    }

    // Add soft glow around bars (for center location only, for performance)
    if (location == 0 && outColor.a < 0.5) {
        float glowStrength = 0.0;

        for (int i = 0; i < 5; i++) {
            float barCenterX = -totalWidth / 2.0 + barWidth * (float(i) + 0.5);
            float amplitude = bands[i];
            amplitude = pow(amplitude, mix(0.5, 1.5, 1.0 - responsiveness));
            amplitude = clamp(amplitude, 0.02, 1.0);

            float barHeight = amplitude * 1.0;
            float barCenterY = -0.5 + barHeight / 2.0;

            // Distance to bar center
            vec2 barCenter = vec2(barCenterX, barCenterY);
            float dist = length(localUV - barCenter);

            // Glow falloff
            float glow = exp(-dist * 3.0) * amplitude * 0.2;
            glowStrength += glow;
        }

        outColor.rgb += barColor.rgb * glowStrength;
        outColor.a = max(outColor.a, glowStrength);
    }

    gl_FragColor = outColor;
}
