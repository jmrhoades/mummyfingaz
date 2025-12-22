/*{
  "ISFVSN": "2",
  "CATEGORIES": ["Generator"],
  "DESCRIPTION": "Simple shape - first sketch",
  "INPUTS": [
    {
      "NAME": "shape",
      "LABEL": "Shape",
      "TYPE": "long",
      "DEFAULT": 0,
      "VALUES": [0, 1, 2],
      "LABELS": ["Square", "Circle", "Triangle"]
    },
    {
      "NAME": "size",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "color",
      "TYPE": "color",
      "DEFAULT": [1.0, 1.0, 1.0, 1.0]
    },
    {
      "NAME": "shrink",
      "LABEL": "Shrink Amount",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "rate",
      "LABEL": "Pulse Rate",
      "TYPE": "long",
      "DEFAULT": 2,
      "VALUES": [0, 1, 2, 3, 4],
      "LABELS": ["1/16", "1/8", "1/4", "1/2", "1"]
    },
    {
      "NAME": "rotateRate",
      "LABEL": "Rotation Rate",
      "TYPE": "long",
      "DEFAULT": 5,
      "VALUES": [0, 1, 2, 3, 4, 5],
      "LABELS": ["1/16", "1/8", "1/4", "1/2", "1", "Off"]
    }
  ]
}*/

// 2D rotation matrix
vec2 rotate(vec2 p, float angle) {
    float c = cos(angle);
    float s = sin(angle);
    return vec2(p.x * c - p.y * s, p.x * s + p.y * c);
}

// SDF for equilateral triangle centered at origin, pointing up
float sdTriangle(vec2 p, float r) {
    const float k = sqrt(3.0);
    p.x = abs(p.x) - r;
    p.y = p.y + r / k;
    if (p.x + k * p.y > 0.0) {
        p = vec2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
    }
    p.x -= clamp(p.x, -2.0 * r, 0.0);
    return -length(p) * sign(p.y);
}

void main() {
    vec2 uv = isf_FragNormCoord;

    // Correct for aspect ratio (make coordinate space square)
    float aspect = RENDERSIZE.x / RENDERSIZE.y;
    vec2 st = uv - 0.5;  // Center at origin
    if (aspect > 1.0) {
        st.x *= aspect;  // Landscape: stretch x
    } else {
        st.y /= aspect;  // Portrait: stretch y
    }

    // Time (beat-synced in Videosync, seconds elsewhere)
    float t;
    #ifdef VIDEOSYNC
      t = BEAT;
    #else
      t = TIME;
    #endif

    // Modulate size with beat (assuming 4/4 time)
    // rate: 0=1/16, 1=1/8, 2=1/4, 3=1/2, 4=1 (pulses per bar: 16, 8, 4, 2, 1)
    float mult = pow(2.0, 2.0 - float(rate));  // 4, 2, 1, 0.5, 0.25
    float pulse = 0.5 + 0.5 * sin(t * mult * 6.28318);
    float minSize = 1.0 - shrink;  // shrink=1 → minSize=0, shrink=0 → minSize=1
    float animSize = size * mix(minSize, 1.0, pulse);

    // Rotate 90 degrees clockwise per pulse (if enabled)
    if (rotateRate < 5) {
        float rotMult = pow(2.0, 2.0 - float(rotateRate));
        float angle = -t * rotMult * 1.5708;  // -π/2 per pulse (clockwise)
        st = rotate(st, angle);
    }

    // Shape SDF (st is already centered at origin)
    float dist;
    float r = animSize * 0.5;

    if (shape == 0) {
        // Square
        vec2 d = abs(st) - vec2(r);
        dist = max(d.x, d.y);
    } else if (shape == 1) {
        // Circle
        dist = length(st) - r;
    } else {
        // Triangle
        dist = sdTriangle(st, r);
    }

    // Hard edge
    float mask = step(dist, 0.0);

    gl_FragColor = vec4(color.rgb * mask, mask);
}
