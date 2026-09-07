/*{
  "DESCRIPTION": "Value noise and gradient noise side by side, with the interpolation curve on a control and a view that shows the grid both of them are built on.",
  "CREDIT": "Eduardo Meneses, Learn shader art. Gradient noise and the quintic curve are Ken Perlin's; the hash is Dave Hoskins's; the presentation follows Inigo Quilez.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Noise"],
  "INPUTS": [
    {
      "NAME": "kind",
      "TYPE": "long",
      "LABEL": "Noise",
      "VALUES": [0, 1, 2],
      "LABELS": ["value noise", "gradient noise", "both, split"],
      "DEFAULT": 2
    },
    {
      "NAME": "curve",
      "TYPE": "long",
      "LABEL": "Interpolation",
      "VALUES": [0, 1, 2],
      "LABELS": ["linear", "smoothstep  3t2-2t3", "quintic  6t5-15t4+10t3"],
      "DEFAULT": 2
    },
    { "NAME": "density", "TYPE": "float", "LABEL": "Density",  "DEFAULT": 5.0, "MIN": 1.0, "MAX": 40.0 },
    { "NAME": "drift",   "TYPE": "float", "LABEL": "Drift",    "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0 },
    { "NAME": "contrast","TYPE": "float", "LABEL": "Contrast", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "showGrid","TYPE": "bool",  "LABEL": "Show the lattice", "DEFAULT": true },
    { "NAME": "warm",    "TYPE": "color", "LABEL": "High",     "DEFAULT": [0.98, 0.86, 0.62, 1.0] },
    { "NAME": "cool",    "TYPE": "color", "LABEL": "Low",      "DEFAULT": [0.10, 0.16, 0.32, 1.0] }
  ]
}*/

const float TAU = 6.28318530718;

float hash21(vec2 p) {
    vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    q += dot(q, q.yzx + 33.33);
    return fract((q.x + q.y) * q.z);
}

// A random unit-ish vector per lattice point, for gradient noise.
vec2 hash22(vec2 p) {
    float a = hash21(p) * TAU;
    float b = hash21(p + 19.19);
    return vec2(cos(a), sin(a)) * (0.7 + 0.3 * b);
}

// The interpolation curve, which is the whole difference between noise that
// looks like noise and noise that looks like a grid of diamonds.
//
//   linear     has a kink at every lattice line; the grid is visible.
//   smoothstep has zero first derivative at the lattice, so the kink goes.
//   quintic    has zero second derivative too, which matters as soon as the
//              noise is differentiated, which fbm and normal mapping both do.
vec2 fade(vec2 t) {
    if (curve == 0) return t;
    if (curve == 1) return t * t * (3.0 - 2.0 * t);
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

// Value noise: a random value at each lattice point, interpolated. Cheap, and
// it has a characteristic blobbiness because the extremes sit on the lattice.
float valueNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = fade(f);
    float a = hash21(i + vec2(0.0, 0.0));
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Gradient noise: a random *direction* at each lattice point, and the value is
// the dot product of that direction with the offset from the lattice point. It
// is therefore zero at every lattice point, which is why it has no blobs and
// why its extremes fall between the lattice points rather than on them.
float gradientNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = fade(f);
    float a = dot(hash22(i + vec2(0.0, 0.0)), f - vec2(0.0, 0.0));
    float b = dot(hash22(i + vec2(1.0, 0.0)), f - vec2(1.0, 0.0));
    float c = dot(hash22(i + vec2(0.0, 1.0)), f - vec2(0.0, 1.0));
    float d = dot(hash22(i + vec2(1.0, 1.0)), f - vec2(1.0, 1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y) * 0.7 + 0.5;
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y * density
           + vec2(TIME * drift, 0.0);

    float n;
    if (kind == 0) {
        n = valueNoise(p);
    } else if (kind == 1) {
        n = gradientNoise(p);
    } else {
        n = uv.x < 0.5 ? valueNoise(p) : gradientNoise(p);
    }

    // Contrast, by smoothstep rather than by multiplication, so it pushes the
    // middle apart without clipping the ends. Unit 06 called this the other use
    // of smoothstep and this is it.
    n = mix(n, smoothstep(0.30, 0.70, n), contrast);

    vec3 colour = mix(cool.rgb, warm.rgb, clamp(n, 0.0, 1.0));

    if (showGrid) {
        // The lattice both kinds of noise are built on. Every feature in the
        // picture is anchored to it, which is the single most useful thing to
        // know about this family of functions and the least obvious.
        vec2 g = abs(fract(p) - 0.5) / fwidth(p);
        float lines = 1.0 - min(min(g.x, g.y), 1.0);
        colour = mix(colour, vec3(0.0), lines * 0.35);
    }

    if (kind == 2) {
        // The split, and a label side chosen so a reader knows which is which.
        float seam = smoothstep(1.5 / RENDERSIZE.x, 0.0, abs(uv.x - 0.5));
        colour = mix(colour, vec3(1.0), seam * 0.8);
    }

    gl_FragColor = vec4(colour, 1.0);
}
