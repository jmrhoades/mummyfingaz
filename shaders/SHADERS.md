# mummyfingaz Shader Library

A composable, portable ISF shader collection for live visual performance with Showsync Videosync.

## Goals

**Composable** — Each shader does one thing well. Chain them together for complex results.

**Portable** — Pure ISF 2.0 specification. No Max/MSP-specific dependencies. Compatible with Videosync, VDMX, and any ISF host.

**Performance-ready** — Optimized for real-time use at 60fps in live sets.

**Audio-reactive** — Designed to respond to mummyfingaz productions via Videosync's audio analysis.

## Target Environment

- **Host**: Showsync Videosync (Max for Live)
- **Format**: ISF 2.0 (Interactive Shader Format)
- **Resolution**: 1080p primary, 4K-capable
- **Frame rate**: 60fps minimum

## Directory Structure

```
shaders/
├── isf/
│   ├── examples/           # Reference implementations
│   │   ├── book-of-shaders/   # Educational shaders from The Book of Shaders
│   │   ├── saturday-shaders/  # VJ Zef's weekly shader exercises
│   │   ├── videosync-isf/     # Showsync's bundled ISF collection
│   │   └── vidvox-isf/        # VIDVOX official ISF library
│   │
│   ├── generators/         # Create visuals from nothing (no input image)
│   │   ├── noise/             # Perlin, simplex, fractal, cellular
│   │   ├── shapes/            # Geometric primitives, SDFs
│   │   ├── patterns/          # Grids, stripes, checkers, moiré
│   │   └── feedback/          # Self-referential, recursive
│   │
│   ├── filters/            # Transform an input image
│   │   ├── color/             # HSV shift, posterize, invert, threshold
│   │   ├── distort/           # Warp, displace, kaleidoscope, mirror
│   │   ├── blur/              # Gaussian, directional, radial, zoom
│   │   └── glitch/            # Datamosh, pixel sort, scan lines
│   │
│   ├── blends/             # Combine two or more inputs
│   │   ├── modes/             # Add, multiply, screen, overlay
│   │   └── transitions/       # Crossfade, wipe, morph
│   │
│   ├── utilities/          # Helper shaders
│   │   ├── lut/               # Color lookup tables
│   │   ├── masks/             # Alpha generation, shape masks
│   │   └── audio/             # Audio-reactive modulators
│   │
│   ├── framework/          # Unified 3D scene framework
│   │   ├── scene3d.fs            # ISF version (standalone demo)
│   │   ├── scene3d.webgl.vert    # WebGL vertex shader
│   │   ├── scene3d.webgl.frag    # WebGL fragment shader
│   │   ├── scene3d.gl.vert       # Desktop GL 330 vertex shader
│   │   ├── scene3d.gl.frag       # Desktop GL 330 fragment shader
│   │   ├── scene3d_include.glsl  # Copy-paste include for any shader
│   │   └── scene3d_inputs.json   # Standard camera ISF inputs
│   │
│   ├── presets/            # Curated combinations for performance
│   │
│   └── sketches/           # Work-in-progress experiments (NNNN-name.fs)
```

## ISF Format Overview

Interactive Shader Format (ISF) wraps GLSL fragment shaders with JSON metadata for parameter definition and host integration.

### Basic Structure
```glsl
/*
{
  "ISFVSN": "2",
  "CATEGORIES": ["Generator"],
  "INPUTS": [
    {
      "NAME": "intensity",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.0,
      "MAX": 1.0
    }
  ]
}
*/

void main() {
    vec2 uv = isf_FragNormCoord;
    gl_FragColor = vec4(uv.x * intensity, uv.y * intensity, 0.0, 1.0);
}
```

### ISF Input Types
| Type | Description | Use Case |
|------|-------------|----------|
| `float` | Single value with min/max | Intensity, speed, scale |
| `bool` | Toggle | Enable/disable features |
| `long` | Integer selection | Mode switches |
| `color` | RGBA color picker | Fill colors, tints |
| `point2D` | XY coordinate | Position, offset |
| `image` | Input texture | Video/image processing |

### ISF Built-in Uniforms
- `RENDERSIZE` — Output resolution (vec2)
- `TIME` — Elapsed time in seconds (float)
- `TIMEDELTA` — Frame delta time (float)
- `FRAMEINDEX` — Current frame number (int)
- `isf_FragNormCoord` — Normalized UV coordinates (vec2)

## ISF Conventions

### Naming
- Lowercase with underscores: `simplex_noise.fs`
- Prefix with category for sorting: `gen_simplex_noise.fs`, `fil_hue_shift.fs`

### Standard Inputs
Every shader should expose consistent parameter names where applicable:

```json
{
  "INPUTS": [
    { "NAME": "intensity", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 10.0 },
    { "NAME": "audio", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 }
  ]
}
```

### Audio Reactivity
Use normalized `audio` input (0.0–1.0) mapped from Videosync's FFT bands. Shaders should work with or without audio input—audio modulates, never controls exclusively.

## Composability Rules

1. **Generators** output to `gl_FragColor` with alpha = 1.0
2. **Filters** expect `inputImage` as primary input
3. **Blends** expect `inputImage` and `inputImage2`
4. **All shaders** normalize UV to 0.0–1.0
5. **Time** uses `#ifdef VIDEOSYNC` pattern for BEAT/TIME (see Videosync Integration)

## Design Principles

### For Generators
- Default to full-screen coverage
- Expose `speed` parameter for time-based animation
- Include `scale` for pattern density control
- Consider `seed` parameter for variation

### For Effects
- Preserve alpha channel when appropriate
- Keep wet/dry `mix` parameter (0.0 = bypass)
- Maintain consistent intensity curves

### For Composability
- Normalize coordinate systems
- Use consistent color space (linear vs sRGB awareness)
- Document expected input ranges in JSON metadata

### Color Scheme
All shaders follow a strict 2-color palette:
- **Foreground**: White `[1.0, 1.0, 1.0, 1.0]` — lines, shapes, text
- **Background**: Black `[0.0, 0.0, 0.0, 1.0]` — always

This ensures visual consistency across the library and allows color to be added downstream via filters, LUTs, or blend modes.

## Videosync Integration

### Videosync-Specific Uniforms

Videosync defines `VIDEOSYNC` as a preprocessor macro, enabling conditional code for beat-synced animation:

```glsl
float t;
#ifdef VIDEOSYNC
  t = BEAT;  // Current time in Ableton Live, measured in beats
#else
  t = TIME;  // Standard ISF time in seconds (fallback for other hosts)
#endif
```

- `BEAT` — Current position in beats from Ableton Live's timeline (float)
- Updates once per rendered frame
- Enables tempo-synced animations that stay locked to the session

All shaders in this library use this pattern for portability while leveraging beat sync in Videosync.

### Beat-Synced Pulse Rates

When creating rhythmic animations in 4/4 time, use musical note values to determine pulse frequency. The `BEAT` uniform increments by 1.0 per beat (quarter note), so a full bar = 4 beats.

```glsl
// Pulse rate multipliers for 4/4 time
// Formula: mult = pow(2.0, 2.0 - float(rate))
float mult = pow(2.0, 2.0 - float(rate));
float pulse = 0.5 + 0.5 * sin(t * mult * 6.28318);
```

| Note Value | rate | Multiplier | Pulses/Beat | Pulses/Bar |
|------------|------|------------|-------------|------------|
| 1/16       | 0    | 4.0        | 4           | 16         |
| 1/8        | 1    | 2.0        | 2           | 8          |
| 1/4        | 2    | 1.0        | 1           | 4          |
| 1/2        | 3    | 0.5        | 0.5         | 2          |
| 1 (whole)  | 4    | 0.25       | 0.25        | 1          |

Standard `rate` input definition:
```json
{
  "NAME": "rate",
  "LABEL": "Pulse Rate",
  "TYPE": "long",
  "DEFAULT": 2,
  "VALUES": [0, 1, 2, 3, 4],
  "LABELS": ["1/16", "1/8", "1/4", "1/2", "1"]
}
```

### Loading Custom Shaders
1. Place `.fs` files in Videosync's ISF search path
2. Shaders appear in the ISF device's shader browser
3. Parameters auto-map to Live device controls

### Parameter Mapping Tips
- Use 0.0–1.0 ranges for automation-friendly parameters
- Include sensible defaults for quick auditioning
- Group related parameters with consistent naming prefixes
- Use `LABEL` for display-friendly parameter names

### Shader Categories
Videosync recognizes these standard ISF categories:
- `Generator` — Creates output without input
- `Filter` — Processes input image
- `Transition` — A/B source mixing

## Development Workflow

### Sketches
Sketches are freely created experiments in `shaders/isf/sketches/`. Use sequential numbering:

```
0001-hello-square.fs
0002-noise-experiment.fs
0003-beat-pulse.fs
```

Sketches are for rapid iteration without worrying about naming conventions or organization. When a sketch is ready for production use, rename it using the category prefix and move to the appropriate folder:

```
sketches/0012-chromatic-split.fs  →  filters/glitch/fil_rgb_split.fs
```

### Steps
1. Create sketch in `sketches/` with next available number
2. Iterate freely in VS Code with ISF extension
3. Test in ISF Editor (isf.video)
4. Validate in Videosync
5. When stable, rename with category prefix and move to appropriate folder
6. Document parameters and intended use

## Priority Builds

### Phase 1: Foundation
- [ ] `gen_simplex_noise.fs` — Base noise generator
- [ ] `gen_circle_sdf.fs` — Signed distance circle
- [ ] `fil_hue_shift.fs` — HSV color rotation
- [ ] `fil_mirror_quad.fs` — 4-way kaleidoscope
- [ ] `fil_feedback_trail.fs` — Persistence/trails

### Phase 2: Texture
- [ ] `gen_voronoi.fs` — Cellular noise
- [ ] `gen_fbm.fs` — Fractal Brownian motion
- [ ] `fil_pixelate.fs` — Resolution reduction
- [ ] `fil_edge_detect.fs` — Sobel/Canny edges

### Phase 3: Glitch
- [ ] `fil_rgb_split.fs` — Chromatic aberration
- [ ] `fil_scan_lines.fs` — CRT emulation
- [ ] `fil_wave_distort.fs` — Sinusoidal displacement
- [ ] `fil_block_glitch.fs` — Random block displacement

### Phase 4: Performance Presets
- [ ] Ambient drift
- [ ] Hard beat response
- [ ] Bass tunnel
- [ ] Hi-hat scatter

## Web Compatibility

ISF shaders can run in browsers via WebGL with a JavaScript runtime to parse metadata and provide uniforms.

### Browser Requirements
- WebGL-compatible GLSL ES syntax
- JS library to parse ISF JSON and create uniforms
- Shims for ISF built-ins (`isf_FragNormCoord`, `RENDERSIZE`, etc.)

### BEAT Shim
Browsers don't have Ableton Live, but we can simulate `BEAT` at a fixed tempo:

```javascript
// 120 BPM in 4/4 time: 2 beats per second
const bpm = 120;
const beat = (performance.now() / 1000) * (bpm / 60);
gl.uniform1f(beatLocation, beat);
```

The `#ifdef VIDEOSYNC` pattern still works—define `VIDEOSYNC` in the shader prefix to enable `BEAT`, or omit it to fall back to `TIME`.

### Libraries
- [interactive-shader-format-js](https://github.com/msfeldstein/interactive-shader-format-js) — ISF → WebGL renderer
- [ISF Editor](https://editor.isf.video) — Browser-based ISF editor

### Test Harness
See `isf-test.html` for a browser-based gallery with transport controls and BPM setting.

To run:
```bash
cd shaders && python3 -m http.server 8000
```
Then open `http://localhost:8000/isf-test.html`

## Unified 3D Framework (scene3d)

All visualizations — even 2D representations — use a shared 3D camera and projection system so that layers composite as one scene when stacked.

### Why

When multiple shaders render separate layers (e.g. ground grid + pyramids + tunnel), they must share **identical camera parameters** to align perfectly. The scene3d framework provides:

- **Shared camera** — orbital camera with distance, height, yaw, pitch, FOV
- **Shared projection** — consistent perspective projection across all layers
- **Shared coordinate system** — right-handed: X=right, Y=up, Z=forward
- **Three shader versions** — ISF, WebGL, desktop GL (all share the same math)

### Versions

| File | Format | Use Case |
|------|--------|----------|
| `scene3d.fs` | ISF 2.0 | Videosync, VDMX, any ISF host |
| `scene3d.webgl.vert/frag` | GLSL ES 1.0 | Browser / WebGL |
| `scene3d.gl.vert/frag` | GLSL 330 core | Desktop OpenGL |
| `scene3d_include.glsl` | Portable GLSL | Copy-paste into any shader |
| `scene3d_inputs.json` | JSON | Standard ISF camera inputs |

### Usage — Adding the Framework to a Shader

1. **Copy the camera INPUTS** from `scene3d_inputs.json` into your ISF metadata
2. **Paste** the `scene3d_include.glsl` block into your shader
3. **Initialize** in `main()`:

```glsl
Scene3D cam = scene3d_setup(
    isf_FragNormCoord, RENDERSIZE,
    camDistance, camHeight, camYaw, camPitch, fov
);
```

4. **Use the API** to draw your content:

```glsl
// Project a 3D point to screen
vec3 screenPt = scene3d_projectPt(vec3(1.0, 0.0, -5.0), cam);

// Draw a 3D line (returns intensity 0-1)
float line = scene3d_drawLine(cam.uv, vec3(0.0), vec3(1.0, 1.0, 0.0), cam, 1.5);

// Draw ground grid
float grid = scene3d_drawGrid(cam, 1.0, 20.0, 1.5);

// Ray-plane intersection (ground plane at y=0)
vec4 hit = scene3d_planePos(cam, 0.0);  // .w = 1.0 if hit

// Depth-based fade
float fade = scene3d_depthFade(screenPt.z, 1.0, 50.0);
```

### Camera Parameters

All shaders sharing these parameters will have identical perspective:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `camDistance` | 5.0 | Distance from origin |
| `camHeight` | 2.0 | Camera Y position |
| `camPitch` | 0.3 | Vertical angle (radians) |
| `camYaw` | 0.0 | Horizontal angle (radians) |
| `fov` | 1.0 | Field of view scale |

### Performance Notes

- **No branching in core math** — all projection is pure arithmetic
- **Early-out for behind-camera** — lines/points behind camera return immediately
- **Depth-scaled line width** — thinner lines at distance = fewer pixels to anti-alias
- **Grid uses analytical derivatives** — no texture sampling, no multi-pass
- Each shader is one layer — keep per-layer cost low for compositing

### Compositing Layers

To composite multiple scene3d layers in Videosync:

1. Set all layers to the **same camera values** (camDistance, camHeight, camPitch, camYaw, fov)
2. Use **additive blending** (black background + white lines)
3. Each layer's `bgColor` should be `[0, 0, 0, 1]`
4. Stack as many layers as needed — the shared perspective ensures alignment

### Example Sketches Using the Framework

- `0006-pyramid-road-3d.fs` — Wireframe pyramids in true 3D space
- `0007-grid-tunnel-3d.fs` — Rectangle tunnel with depth
- `0008-scifi-grid-3d.fs` — Infinite ground grid with scan lines

## Resources

- [ISF Specification](https://github.com/mrRay/ISF_Spec)
- [Videosync Documentation](https://showsync.info/videosync)
- [Videosync Custom Plugins](https://support.showsync.com/videosync/building-plugins/more-about-custom-plugins)
- [The Book of Shaders](https://thebookofshaders.com)
- [ISF Editor](https://isf.video)
- [VIDVOX ISF Files](https://github.com/VIDVOX/ISF-Files)
- [Shadertoy](https://shadertoy.com) — GLSL reference (needs ISF conversion)
