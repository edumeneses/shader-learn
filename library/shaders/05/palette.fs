/*{
  "DESCRIPTION": "The cosine palette, with its four constants on controls, shown as a strip and applied to a field, so the relationship between the numbers and the gradient is visible.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The palette form is Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Color"],
  "INPUTS": [
    {
      "NAME": "preset",
      "TYPE": "long",
      "LABEL": "Preset",
      "VALUES": [0, 1, 2, 3, 4],
      "LABELS": ["custom", "warm rainbow", "teal and orange", "ember", "ice"],
      "DEFAULT": 2
    },
    { "NAME": "bias",   "TYPE": "color", "LABEL": "a  bias",      "DEFAULT": [0.50, 0.50, 0.50, 1.0] },
    { "NAME": "amp",    "TYPE": "color", "LABEL": "b  amplitude", "DEFAULT": [0.50, 0.50, 0.50, 1.0] },
    { "NAME": "freq",   "TYPE": "color", "LABEL": "c  frequency", "DEFAULT": [1.00, 1.00, 1.00, 1.0] },
    { "NAME": "phase",  "TYPE": "color", "LABEL": "d  phase",     "DEFAULT": [0.00, 0.33, 0.67, 1.0] },
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "View",
      "VALUES": [0, 1, 2],
      "LABELS": ["strip and channels", "applied to a field", "both"],
      "DEFAULT": 2
    },
    { "NAME": "offset", "TYPE": "float", "LABEL": "Offset",  "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0 },
    { "NAME": "cycles", "TYPE": "float", "LABEL": "Cycles",  "DEFAULT": 1.0, "MIN": 0.25, "MAX": 6.0 },
    { "NAME": "drift",  "TYPE": "float", "LABEL": "Drift",   "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0 }
  ]
}*/

const float TAU = 6.28318530718;

// colour(t) = a + b * cos(TAU * (c * t + d))
//
// Four vec3 constants. `a` is the average colour, `b` is how far it swings, `c`
// is how many times each channel cycles as t goes 0 to 1, and `d` is where each
// channel starts. Offsetting the three channels of `d` by roughly a third is
// what produces a rainbow; leaving them equal produces a single hue that only
// changes in brightness.
vec3 palette(float t, vec3 a, vec3 b, vec3 c, vec3 d) {
    return a + b * cos(TAU * (c * t + d));
}

void constants(out vec3 a, out vec3 b, out vec3 c, out vec3 d) {
    if (preset == 1) {           // warm rainbow
        a = vec3(0.5); b = vec3(0.5); c = vec3(1.0); d = vec3(0.00, 0.10, 0.20);
    } else if (preset == 2) {    // teal and orange
        a = vec3(0.50, 0.45, 0.42); b = vec3(0.45, 0.42, 0.45);
        c = vec3(1.00, 1.00, 1.00); d = vec3(0.00, 0.15, 0.55);
    } else if (preset == 3) {    // ember
        a = vec3(0.55, 0.28, 0.18); b = vec3(0.45, 0.30, 0.18);
        c = vec3(1.00, 1.00, 0.60); d = vec3(0.00, 0.08, 0.20);
    } else if (preset == 4) {    // ice
        a = vec3(0.42, 0.52, 0.60); b = vec3(0.35, 0.38, 0.42);
        c = vec3(0.90, 1.00, 1.10); d = vec3(0.55, 0.62, 0.70);
    } else {                     // custom: whatever the four colour pickers say
        a = bias.rgb; b = amp.rgb; c = freq.rgb; d = phase.rgb;
    }
}

void main() {
    vec2 uv = isf_FragNormCoord;

    vec3 a, b, c, d;
    constants(a, b, c, d);

    float t = fract(uv.x * cycles + offset + TIME * drift);

    bool showStrip = (view != 1);
    bool showField = (view != 0);
    float stripTop = showField ? 0.34 : 1.0;

    vec3 colour;

    if (showStrip && uv.y < stripTop) {
        float inner = uv.y / stripTop;

        if (inner > 0.55) {
            // The gradient itself.
            colour = palette(t, a, b, c, d);
        } else {
            // The three channels plotted as curves, because the point of the
            // form is that each channel is one cosine and the picker moves it.
            vec3 v = palette(t, a, b, c, d);
            float y = (inner / 0.55);
            colour = vec3(0.05, 0.06, 0.09);
            float w = 2.5 / (RENDERSIZE.y * 0.55 * stripTop);
            colour = mix(colour, vec3(1.0, 0.30, 0.35), smoothstep(w, 0.0, abs(y - clamp(v.r, 0.0, 1.0))));
            colour = mix(colour, vec3(0.35, 1.0, 0.45), smoothstep(w, 0.0, abs(y - clamp(v.g, 0.0, 1.0))));
            colour = mix(colour, vec3(0.40, 0.55, 1.0), smoothstep(w, 0.0, abs(y - clamp(v.b, 0.0, 1.0))));
        }
    } else {
        // The same palette driven by a field rather than by x, which is how it
        // is actually used everywhere else in the course.
        vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y;
        float r = length(p * vec2(1.0, 1.35));
        float ang = atan(p.y, p.x) / TAU;
        float f = fract(r * 1.6 * cycles - ang * 2.0 + offset + TIME * drift);
        colour = palette(f, a, b, c, d);
    }

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
