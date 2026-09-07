/*{
  "DESCRIPTION": "Fractal Brownian motion with octaves, lacunarity, and gain on controls, and a view that shows each octave separately so the sum stops being mysterious.",
  "CREDIT": "Eduardo Meneses, Learn shader art. Gradient noise and the quintic curve are Ken Perlin's; the hash is Dave Hoskins's; fbm and the ridged and turbulent variants follow Inigo Quilez.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Noise"],
  "INPUTS": [
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "View",
      "VALUES": [0, 1, 2],
      "LABELS": ["the sum", "octaves side by side", "running sum"],
      "DEFAULT": 0
    },
    { "NAME": "octaves",    "TYPE": "float", "LABEL": "Octaves",    "DEFAULT": 5.0,  "MIN": 1.0, "MAX": 8.0 },
    { "NAME": "density",    "TYPE": "float", "LABEL": "Base scale", "DEFAULT": 3.0,  "MIN": 0.5, "MAX": 16.0 },
    { "NAME": "lacunarity", "TYPE": "float", "LABEL": "Lacunarity", "DEFAULT": 2.0,  "MIN": 1.1, "MAX": 4.0 },
    { "NAME": "gain",       "TYPE": "float", "LABEL": "Gain",       "DEFAULT": 0.5,  "MIN": 0.1, "MAX": 0.9 },
    { "NAME": "ridged",     "TYPE": "bool",  "LABEL": "Ridged",     "DEFAULT": false },
    { "NAME": "turbulent",  "TYPE": "bool",  "LABEL": "Turbulent",  "DEFAULT": false },
    { "NAME": "drift",      "TYPE": "float", "LABEL": "Drift",      "DEFAULT": 0.05, "MIN": -0.5, "MAX": 0.5 },
    { "NAME": "hue",        "TYPE": "float", "LABEL": "Hue",        "DEFAULT": 0.58, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spread",     "TYPE": "float", "LABEL": "Hue spread", "DEFAULT": 0.30, "MIN": 0.0, "MAX": 1.0 }
  ]
}*/

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 a = vec3(0.5), b = vec3(0.48), c = vec3(1.0);
    vec3 d = vec3(hue) + spread * vec3(0.0, 0.33, 0.67);
    return a + b * cos(TAU * (c * t + d));
}

float hash21(vec2 p) {
    vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    q += dot(q, q.yzx + 33.33);
    return fract((q.x + q.y) * q.z);
}

vec2 hash22(vec2 p) {
    float a = hash21(p) * TAU;
    float b = hash21(p + 19.19);
    return vec2(cos(a), sin(a)) * (0.7 + 0.3 * b);
}

vec2 quintic(vec2 t) {
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

// Gradient noise, returned centred on zero rather than on a half. Centring
// matters here: fbm sums octaves, and summing values that are all positive
// gives a field that drifts upward with every octave instead of staying put.
float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    vec2 u = quintic(f);
    float a = dot(hash22(i + vec2(0.0, 0.0)), f - vec2(0.0, 0.0));
    float b = dot(hash22(i + vec2(1.0, 0.0)), f - vec2(1.0, 0.0));
    float c = dot(hash22(i + vec2(0.0, 1.0)), f - vec2(0.0, 1.0));
    float d = dot(hash22(i + vec2(1.0, 1.0)), f - vec2(1.0, 1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y) * 1.4;
}

// One octave, with the two variants that are worth having as switches rather
// than as separate functions:
//
//   turbulent  abs(n), which folds the field at zero and makes creases where it
//              used to pass through the middle. Wisps of smoke, flame.
//   ridged     1 - abs(n), the same fold inverted, so the creases become sharp
//              peaks. Mountain ridges, and the reason for the name.
float octave(float n) {
    if (ridged) { float r = 1.0 - abs(n); return r * r - 0.5; }
    if (turbulent) return abs(n) - 0.5;
    return n;
}

// Fractal Brownian motion: sum octaves, each `lacunarity` times finer and
// `gain` times quieter than the last. Two numbers describe the whole family.
//
//   lacunarity  how much finer each octave is. 2.0 means each octave has twice
//               the frequency, which is where the word octave comes from.
//   gain        how much quieter. 0.5 with lacunarity 2.0 gives amplitude
//               inversely proportional to frequency, which is what most natural
//               surfaces measure, and is why that pair is the default
//               everywhere.
float fbm(vec2 p, int count, out float running[8]) {
    float sum = 0.0;
    float amp = 0.5;
    float freq = 1.0;
    float norm = 0.0;
    for (int i = 0; i < 8; i++) {
        if (i >= count) { running[i] = sum; continue; }
        sum += amp * octave(noise(p * freq));
        norm += amp;
        running[i] = sum;
        freq *= lacunarity;
        amp *= gain;
    }
    return sum / max(norm, 1e-5);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 base = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y * density
              + vec2(TIME * drift, 0.0);

    int count = int(floor(octaves + 0.5));
    float running[8];

    float n;

    if (view == 0) {
        n = fbm(base, count, running);

    } else if (view == 1) {
        // Each octave alone, in a column, so their relative scale and loudness
        // are directly comparable. This is the view that makes lacunarity and
        // gain mean something rather than being two sliders that change how
        // rough it looks.
        float col = floor(uv.x * float(count));
        float freq = pow(lacunarity, col);
        float amp = 0.5 * pow(gain, col);
        n = amp * octave(noise(base * freq)) / 0.5;

    } else {
        // The sum after 1, 2, 3 ... octaves, in columns. Reading left to right
        // is watching detail accumulate on an outline that was settled by the
        // first octave and never moves again.
        float col = min(floor(uv.x * float(count)), float(count) - 1.0);
        fbm(base, count, running);
        float norm = 0.0, amp = 0.5;
        for (int i = 0; i < 8; i++) {
            if (float(i) > col) break;
            norm += amp;
            amp *= gain;
        }
        n = running[int(col)] / max(norm, 1e-5);
    }

    vec3 colour = palette(0.5 + n * 0.55);

    if (view != 0) {
        float cols = float(count);
        float edge = abs(fract(uv.x * cols) - 0.5);
        colour = mix(colour, vec3(0.02), smoothstep(2.5 / RENDERSIZE.x * cols, 0.0, edge) * 0.85);
    }

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
