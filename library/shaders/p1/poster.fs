/*{
  "DESCRIPTION": "The Module C milestone: one still image built only from distance fields, their operators, domain repetition, a cosine palette, and antialiasing measured with fwidth.",
  "CREDIT": "Eduardo Meneses, Learn shader art",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Generator"],
  "INPUTS": [
    { "NAME": "sun",      "TYPE": "point2D", "LABEL": "Sun",        "DEFAULT": [0.50, 0.70], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "sunSize",  "TYPE": "float",   "LABEL": "Sun size",   "DEFAULT": 0.24, "MIN": 0.05, "MAX": 0.50 },
    { "NAME": "horizon",  "TYPE": "float",   "LABEL": "Horizon",    "DEFAULT": -0.06, "MIN": -0.4, "MAX": 0.4 },
    { "NAME": "slats",    "TYPE": "float",   "LABEL": "Slats",      "DEFAULT": 9.0,  "MIN": 0.0,  "MAX": 30.0 },
    { "NAME": "slatBias", "TYPE": "float",   "LABEL": "Slat taper", "DEFAULT": 0.55, "MIN": 0.0,  "MAX": 1.0 },
    { "NAME": "ridge",    "TYPE": "float",   "LABEL": "Ridge",      "DEFAULT": 0.55, "MIN": 0.0,  "MAX": 1.0 },
    { "NAME": "blend",    "TYPE": "float",   "LABEL": "Blend",      "DEFAULT": 0.045, "MIN": 0.001, "MAX": 0.35 },
    { "NAME": "hue",      "TYPE": "float",   "LABEL": "Hue",        "DEFAULT": 0.02, "MIN": 0.0,  "MAX": 1.0 },
    { "NAME": "spread",   "TYPE": "float",   "LABEL": "Hue spread", "DEFAULT": 0.42, "MIN": 0.0,  "MAX": 1.0 },
    { "NAME": "grain",    "TYPE": "float",   "LABEL": "Grain",      "DEFAULT": 0.035, "MIN": 0.0, "MAX": 0.15 },
    { "NAME": "vignette", "TYPE": "float",   "LABEL": "Vignette",   "DEFAULT": 0.35, "MIN": 0.0,  "MAX": 1.0 }
  ]
}*/

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 a = vec3(0.52, 0.42, 0.44);
    vec3 b = vec3(0.46, 0.38, 0.40);
    vec3 c = vec3(1.0);
    vec3 d = vec3(hue) + spread * vec3(0.00, 0.28, 0.58);
    return a + b * cos(TAU * (c * t + d));
}

float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

float box(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

// A cheap hash for the grain. Unit 12 explains why this works and why the
// constants look the way they do; here it is one line of texture.
float hash(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

void main() {
    vec2 p = (isf_FragNormCoord - 0.5) * RENDERSIZE / RENDERSIZE.y;
    vec2 s = (sun - 0.5) * RENDERSIZE / RENDERSIZE.y;

    // The disc, cut by a stack of slats whose gaps widen towards the bottom.
    // The taper is domain repetition with a cell that is not constant, which is
    // the one liberty this milestone takes with Unit 10.
    float disc = length(p - s) - sunSize;

    float y = (p.y - s.y);
    float band = fract(y * slats * (1.0 + slatBias * clamp(-y * 2.0, 0.0, 1.0)));
    float slat = abs(band - 0.5) * 2.0 - 0.45;
    float cut = max(disc, -slat * 0.06);

    // A ground, made from a horizon line softened into the disc so the two read
    // as one object rather than as two stacked ones.
    float ground = p.y - horizon;
    float land = smin(cut, ground, blend);

    // Ridges in the ground: repeated boxes at falling heights, unioned smoothly.
    float hills = 1e9;
    for (int i = 0; i < 5; i++) {
        float fi = float(i);
        // Heights and widths fall off from the centre outwards, and the sine
        // only jitters the placement: a ridge line that is regular reads as a
        // fence, and one that is random reads as noise. This is neither.
        float h = ridge * (0.20 - abs(fi - 2.0) * 0.045);
        float w = 0.17 - abs(fi - 2.0) * 0.028;
        float xoff = (fi - 2.0) * 0.30 + sin(fi * 2.7) * 0.05;
        float b = box(p - vec2(xoff, horizon - h * 0.35), vec2(w, h)) - 0.045;
        hills = smin(hills, b, blend * 1.2);
    }
    float scene = smin(land, hills, blend * 0.5);

    float aa = fwidth(scene);
    float inside = smoothstep(aa, -aa, scene);

    // Colour by the field rather than by the shape: the palette is driven by
    // the distance, so the interior has depth and the exterior has a glow, from
    // one expression.
    vec3 sky = palette(0.62 + scene * 0.55) * (0.55 + 0.45 * exp(-max(scene, 0.0) * 3.0));
    vec3 body = palette(0.05 - scene * 1.15);
    vec3 colour = mix(sky, body, inside);

    // A rim exactly on the boundary, one pixel wide at any resolution.
    colour += palette(0.30) * 0.45 * smoothstep(aa * 2.5, 0.0, abs(scene));

    // Vignette in the same centred space, so it does not shift with aspect.
    colour *= 1.0 - vignette * smoothstep(0.35, 1.15, length(p * vec2(0.85, 1.0)));

    // Grain last, and before nothing: it is there to break up the banding a
    // smooth palette produces on an eight-bit display, which is Unit 04's point.
    colour += (hash(isf_FragNormCoord * RENDERSIZE) - 0.5) * grain;

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
