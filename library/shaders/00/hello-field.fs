/*{
  "DESCRIPTION": "The course in one shader: a signed distance field, a cosine palette, an antialiased edge, and every number exposed as a named parameter.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The cosine palette is Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Generator", "Course"],
  "INPUTS": [
    { "NAME": "focus",    "TYPE": "point2D", "LABEL": "Focus",     "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "radius",   "TYPE": "float",   "LABEL": "Radius",    "DEFAULT": 0.28, "MIN": 0.02, "MAX": 0.70 },
    { "NAME": "softness", "TYPE": "float",   "LABEL": "Softness",  "DEFAULT": 0.012, "MIN": 0.0, "MAX": 0.30 },
    { "NAME": "rings",    "TYPE": "float",   "LABEL": "Rings",     "DEFAULT": 7.0, "MIN": 0.0, "MAX": 30.0 },
    { "NAME": "drift",    "TYPE": "float",   "LABEL": "Drift",     "DEFAULT": 0.35, "MIN": -2.0, "MAX": 2.0 },
    { "NAME": "hue",      "TYPE": "float",   "LABEL": "Hue",       "DEFAULT": 0.62, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spread",   "TYPE": "float",   "LABEL": "Hue spread","DEFAULT": 0.28, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "glow",     "TYPE": "float",   "LABEL": "Glow",      "DEFAULT": 0.55, "MIN": 0.0, "MAX": 1.0 }
  ]
}*/

const float TAU = 6.28318530718;

// The cosine palette. Four vec3 constants describe a whole family of gradients
// for the price of three cosines, which is cheaper than a lookup texture and,
// more usefully, differentiable: the palette can be driven by a distance rather
// than sampled at one.
vec3 palette(float t) {
    vec3 a = vec3(0.50, 0.48, 0.52);
    vec3 b = vec3(0.48, 0.46, 0.50);
    vec3 c = vec3(1.00, 1.00, 1.00);
    vec3 d = vec3(hue) + spread * vec3(0.00, 0.33, 0.67);
    return a + b * cos(TAU * (c * t + d));
}

void main() {
    // Divide by the shorter side, not by both, so a circle stays a circle at
    // any aspect ratio. Unit 03 is about the version of this that gets it wrong.
    vec2 uv = (isf_FragNormCoord * RENDERSIZE - 0.5 * RENDERSIZE) / RENDERSIZE.y;
    vec2 centre = (focus * RENDERSIZE - 0.5 * RENDERSIZE) / RENDERSIZE.y;

    // The field: signed distance to a circle. Negative inside, zero on the
    // edge, positive outside, and it means the same thing everywhere on screen.
    float d = length(uv - centre) - radius;

    float phase = TIME * drift;

    // Rings read the raw distance rather than only its sign, which is the
    // point: the field carries information everywhere, not just at zero.
    float band = 0.5 + 0.5 * cos((d * rings - phase) * TAU);
    vec3 inside = palette(d * 2.2 + phase * 0.15) * (0.30 + 0.85 * band);

    // Outside, the same field becomes a falloff. One expression, two jobs.
    float halo = glow * exp(-max(d, 0.0) * 9.0);
    vec3 outside = palette(0.45 + d * 0.8 + phase * 0.15) * halo;

    // fwidth is the field's rate of change across one pixel, so this edge is
    // one pixel wide at any resolution and at any zoom. Unit 11 is this line.
    float aa = fwidth(d);
    float edge = smoothstep(softness + aa, -aa, d);

    vec3 colour = mix(outside, inside, edge);

    // A thin bright rim exactly on the zero crossing, to show where the edge
    // actually is once softness is turned up and it stops being obvious.
    colour += palette(phase * 0.15 + 0.2) * 0.5
            * smoothstep(aa * 2.0 + softness, 0.0, abs(d));

    gl_FragColor = vec4(colour, 1.0);
}
