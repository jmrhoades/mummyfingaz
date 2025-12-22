# Mummyfingaz Shader Library

A composable, portable ISF shader library designed to create reactive visuals for mummyfingaz sounds, optimized for Showsync's Videosync Max for Live device.

## Goals

### Composability
- Build shaders from reusable components (SDF primitives, noise functions, color utilities)
- Design shaders that chain well together in Videosync's effect pipeline
- Standardize input parameter conventions for consistent automation

### Portability
- Full ISF 2.0 specification compliance
- Compatible with Videosync, VDMX, and any ISF-compatible host
- No proprietary extensions or host-specific features

### Audio-Visual Integration
- Expose parameters suitable for audio-reactive mapping
- Design with Ableton Live automation in mind
- Support both generative patterns and image/video processing

## Directory Structure

```
shaders/
└── isf/
    ├── examples/           # Reference implementations
    │   ├── book-of-shaders/   # Educational shaders from The Book of Shaders
    │   ├── saturday-shaders/  # VJ Zef's weekly shader exercises
    │   ├── videosync-isf/     # Showsync's bundled ISF collection
    │   └── vidvox-isf/        # VIDVOX official ISF library
    ├── generators/         # (planned) Source/pattern generators
    ├── effects/            # (planned) Image processing effects
    ├── transitions/        # (planned) A/B transition effects
    └── lib/                # (planned) Shared GLSL utility functions
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
- `RENDERSIZE` - Output resolution (vec2)
- `TIME` - Elapsed time in seconds (float)
- `TIMEDELTA` - Frame delta time (float)
- `FRAMEINDEX` - Current frame number (int)
- `isf_FragNormCoord` - Normalized UV coordinates (vec2)

## Videosync Integration

### Loading Custom Shaders
1. Place `.fs` files in Videosync's ISF search path
2. Shaders appear in the ISF device's shader browser
3. Parameters auto-map to Live device controls

### Parameter Mapping Tips
- Use 0.0-1.0 ranges for automation-friendly parameters
- Include sensible defaults for quick auditioning
- Group related parameters with consistent naming prefixes
- Use `LABEL` for display-friendly parameter names

### Shader Categories
Videosync recognizes these standard ISF categories:
- `Generator` - Creates output without input
- `Filter` - Processes input image
- `Transition` - A/B source mixing

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

## Resources

- [ISF Specification](https://github.com/mrRay/ISF_Spec)
- [Videosync Documentation](https://showsync.info/videosync)
- [The Book of Shaders](https://thebookofshaders.com/)
- [ISF Editor](https://isf.video/)
- [VIDVOX ISF Files](https://github.com/VIDVOX/ISF-Files)
