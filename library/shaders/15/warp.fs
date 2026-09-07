/*{
  "DESCRIPTION": "Domain warping: feed noise its own output as a coordinate offset, once or twice, with each stage on a control so the contribution of each is separable.",
  "CREDIT": "Eduardo Meneses, Learn shader art. Domain warping, the two-level form and its offset constants are Inigo Quilez's; the quintic curve is Ken Perlin's; the hash is Dave Hoskins's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Noise"],
  "INPUTS": [
    {
      "NAME": "levels",
      "TYPE": "long",
      "LABEL": "Warp levels",
      "VALUES": [0, 1, 2],
      "LABELS": ["none", "one", "two"],
      "DEFAULT": 2
    },
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "View",
      "VALUES": [0, 1, 2],
      "LABELS": ["the result", "the warp field", "how far each point moved"],
      "DEFAULT": 0
    },
    { "NAME": "density", "TYPE": "float", "LABEL": "Base scale",  "DEFAULT": 3.0, "MIN": 0.5, "MAX": 12.0 },
    { "NAME": "warpA",   "TYPE": "float", "LABEL": "Warp 1",      "DEFAULT": 4.0, "MIN": 0.0, "MAX": 12.0 },
    { "NAME": "warpB",   "TYPE": "float", "LABEL": "Warp 2",      "DEFAULT": 4.0, "MIN": 0.0, "MAX": 12.0 },
    { "NAME": "octaves", "TYPE": "float", "LABEL": "Octaves",     "DEFAULT": 4.0, "MIN": 1.0, "MAX": 7.0 },
    { "NAME": "drift",   "TYPE": "float", "LABEL": "Drift",       "DEFAULT": 0.04, "MIN": -0.4, "MAX": 0.4 },
    { "NAME": "hue",     "TYPE": "float", "LABEL": "Hue",         "DEFAULT": 0.06, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spread",  "TYPE": "float", "LABEL": "Hue spread",  "DEFAULT": 0.22, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "shade",   "TYPE": "float", "LABEL": "Shading",     "DEFAULT": 0.45, "MIN": 0.0, "MAX": 1.0 }
  ]
}*/

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 a = vec3(0.5), b = vec3(0.46), c = vec3(1.0);
    vec3 d = vec3(hue) + spread * vec3(0.0, 0.30, 0.62);
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

vec2 quintic(vec2 t) { return t * t * t * (t * (t * 6.0 - 15.0) + 10.0); }

float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    vec2 u = quintic(f);
    float a = dot(hash22(i), f);
    float b = dot(hash22(i + vec2(1.0, 0.0)), f - vec2(1.0, 0.0));
    float c = dot(hash22(i + vec2(0.0, 1.0)), f - vec2(0.0, 1.0));
    float d = dot(hash22(i + vec2(1.0, 1.0)), f - vec2(1.0, 1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y) * 1.4;
}

float fbm(vec2 p) {
    float sum = 0.0, amp = 0.5, norm = 0.0;
    int count = int(floor(octaves + 0.5));
    for (int i = 0; i < 7; i++) {
        if (i >= count) break;
        sum += amp * noise(p);
        norm += amp;
        p *= 2.0;
        amp *= 0.5;
    }
    return sum / max(norm, 1e-5);
}

// Two fbm calls at offset origins give a vector field: one for x, one for y.
// The offsets are arbitrary and only need to be far enough apart that the two
// are uncorrelated; the numbers below are Quilez's.
vec2 warpVector(vec2 p) {
    return vec2(fbm(p + vec2(0.0, 0.0)),
                fbm(p + vec2(5.2, 1.3)));
}

// The technique, in three lines.
//
//   q = fbm of p
//   r = fbm of (p + warpA * q)
//   result = fbm of (p + warpB * r)
//
// Each level asks the noise where to look before looking. One level turns
// featureless clouds into flow. Two levels turns flow into something that reads
// as a material: marble, agate, oil, weather. Three is rarely worth it.
float warped(vec2 p, out vec2 offset) {
    offset = vec2(0.0);
    if (levels == 0) return fbm(p);

    vec2 q = warpVector(p);
    if (levels == 1) {
        offset = warpA * 0.25 * q;
        return fbm(p + offset);
    }

    vec2 r = warpVector(p + warpA * 0.25 * q);
    offset = warpB * 0.25 * r;
    return fbm(p + offset);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y * density
           + vec2(TIME * drift, 0.0);

    vec2 offset;
    float n = warped(p, offset);

    vec3 colour;

    if (view == 1) {
        // The warp vector field itself, as colour: red is how far right the
        // lookup moved, green how far up. Compare it against the result and the
        // relationship becomes obvious rather than magical.
        colour = vec3(0.5 + offset * 0.5, 0.5);

    } else if (view == 2) {
        // Displacement magnitude. The bright regions are where the noise is
        // being asked about somewhere far away, and they are exactly where the
        // result develops its filaments.
        float mag = length(offset);
        colour = palette(0.15 + mag * 0.6) * clamp(mag * 1.6, 0.05, 1.0);

    } else {
        colour = palette(0.5 + n * 0.6);

        // A cheap shading term: treat the field as a height map and light it
        // with its own screen-space gradient. It costs two derivative
        // instructions and no extra samples, and it is what makes the result
        // read as a material rather than as a heat map.
        //
        // The gradient is scaled by a constant rather than divided by fwidth.
        // Dividing amplifies the finest octave, which is the one closest to a
        // pixel in size, so the picture picks up a stipple that is aliasing
        // rather than detail. A constant keeps the relief and leaves the noise
        // where it belongs.
        vec2 g = vec2(dFdx(n), dFdy(n)) * 24.0;
        vec3 normal = normalize(vec3(-g, 0.25));
        float lambert = clamp(dot(normal, normalize(vec3(-0.55, 0.65, 0.52))), 0.0, 1.0);
        colour *= mix(1.0, 0.55 + 0.85 * lambert, shade);
    }

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
