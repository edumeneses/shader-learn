/*{
  "DESCRIPTION": "Easing curves plotted above the motion they produce, with phase offsets, so the shape of a curve and the feel of a movement can be compared directly.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The cosine palette is Inigo Quilez's; the quintic curve is Ken Perlin's; the back and elastic curves are the standard easing catalogue.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course"],
  "INPUTS": [
    {
      "NAME": "easing",
      "TYPE": "long",
      "LABEL": "Easing",
      "VALUES": [0, 1, 2, 3, 4, 5, 6],
      "LABELS": ["linear", "smoothstep", "quintic", "ease in  t^2", "ease out  1-(1-t)^2", "back", "elastic"],
      "DEFAULT": 1
    },
    { "NAME": "period",  "TYPE": "float", "LABEL": "Period s",   "DEFAULT": 3.0, "MIN": 0.4, "MAX": 12.0 },
    { "NAME": "count",   "TYPE": "float", "LABEL": "Followers",  "DEFAULT": 7.0, "MIN": 1.0, "MAX": 24.0 },
    { "NAME": "cascade", "TYPE": "float", "LABEL": "Phase spread","DEFAULT": 0.28, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "pingPong","TYPE": "bool",  "LABEL": "Ping-pong",  "DEFAULT": true },
    { "NAME": "hue",     "TYPE": "float", "LABEL": "Hue",        "DEFAULT": 0.60, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spread",  "TYPE": "float", "LABEL": "Hue spread", "DEFAULT": 0.30, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "trail",   "TYPE": "float", "LABEL": "Trail",      "DEFAULT": 0.45, "MIN": 0.0, "MAX": 1.0 }
  ]
}*/

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 a = vec3(0.5), b = vec3(0.47), c = vec3(1.0);
    vec3 d = vec3(hue) + spread * vec3(0.0, 0.33, 0.67);
    return a + b * cos(TAU * (c * t + d));
}

// Every easing curve is a function from 0..1 to 0..1. That is all an easing
// curve is, and once that lands, writing your own is a matter of taste rather
// than of looking one up.
float ease(float t) {
    t = clamp(t, 0.0, 1.0);
    if (easing == 0) return t;
    if (easing == 1) return t * t * (3.0 - 2.0 * t);
    if (easing == 2) return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
    if (easing == 3) return t * t;
    if (easing == 4) return 1.0 - (1.0 - t) * (1.0 - t);
    if (easing == 5) {
        // Overshoot and settle. Note that it leaves 0..1: an easing curve is
        // not required to stay inside its own range, and the ones that do not
        // are the ones that feel alive.
        const float s = 1.70158;
        float u = t - 1.0;
        return u * u * ((s + 1.0) * u + s) + 1.0;
    }
    // Elastic: a decaying oscillation. Expensive, and the only curve here that
    // a reader will recognise from an interface animation.
    if (t <= 0.0) return 0.0;
    if (t >= 1.0) return 1.0;
    return pow(2.0, -10.0 * t) * sin((t * 10.0 - 0.75) * (TAU / 3.0)) + 1.0;
}

// The phase: where we are in the cycle, from 0 to 1, and it never grows.
//
// This is the habit worth forming. `TIME * speed` grows without bound and loses
// float precision after an hour; fract of a division does not, repeats exactly,
// and can be offset per object to make a cascade. Every animation in this
// course is driven by a phase rather than by a clock.
float phaseAt(float offset) {
    float t = fract(TIME / max(period, 0.001) + offset);
    // Ping-pong folds the phase so the motion returns rather than jumping back.
    // A linear ramp with a hard reset is the single most common way to make an
    // animation look broken.
    return pingPong ? 1.0 - abs(t * 2.0 - 1.0) : t;
}

void main() {
    vec2 uv = isf_FragNormCoord;
    float aspect = RENDERSIZE.x / RENDERSIZE.y;

    vec3 colour = vec3(0.05, 0.06, 0.09);

    float plotTop = 0.55;

    if (uv.y > plotTop) {
        // The curve, plotted. x is the input, y is the output, so a straight
        // diagonal is linear and anything above it is running ahead of time.
        vec2 g = vec2(uv.x, (uv.y - plotTop) / (1.0 - plotTop));
        float w = 2.5 / (RENDERSIZE.y * (1.0 - plotTop));

        // The diagonal, for reference.
        colour = mix(colour, vec3(0.16, 0.18, 0.24),
                     smoothstep(w * 1.2, 0.0, abs(g.y - g.x)));

        float e = ease(g.x);
        colour = mix(colour, palette(0.3), smoothstep(w, 0.0, abs(g.y - e)));

        // A marker riding the curve at the current phase, which is what ties
        // the plot to the motion underneath it.
        float ph = phaseAt(0.0);
        vec2 marker = vec2(ph, ease(ph));
        colour = mix(colour, vec3(1.0),
                     smoothstep(0.012, 0.006, length((g - marker) * vec2(1.0, 1.0))));

        // The band where the curve leaves 0..1, for `back` and `elastic`.
        colour = mix(colour, vec3(0.30, 0.10, 0.14),
                     smoothstep(w * 1.2, 0.0, abs(g.y - 1.0)) * 0.6);

    } else {
        // The motion. One row per follower, each with a phase offset, so the
        // cascade shows what a phase offset is for: the same animation, started
        // at different times, is most of what makes a sequence feel designed.
        int n = int(floor(count + 0.5));
        vec2 p = vec2(uv.x, uv.y / plotTop);

        for (int i = 0; i < 24; i++) {
            if (i >= n) break;
            float fi = float(i);
            float row = (fi + 0.5) / float(n);
            float ph = phaseAt(-fi / float(n) * cascade);
            float x = 0.08 + 0.84 * ease(ph);

            vec2 d = (p - vec2(x, row)) * vec2(aspect * plotTop, 1.0);
            float r = length(d);
            vec3 ink = palette(fi / float(n) * 0.6 + 0.1);

            float dot = smoothstep(0.030, 0.020, r);
            colour = mix(colour, ink, dot);

            // A trail behind it, drawn by sampling the curve at earlier phases.
            // Not feedback: this is the same function asked about the past,
            // which is what Unit 18 will do differently.
            for (int k = 1; k <= 8; k++) {
                float back = float(k) / 60.0;
                float pb = phaseAt(-fi / float(n) * cascade - back);
                float xb = 0.08 + 0.84 * ease(pb);
                float rb = length((p - vec2(xb, row)) * vec2(aspect * plotTop, 1.0));
                colour = mix(colour, ink,
                             smoothstep(0.022, 0.012, rb) * trail * (1.0 - float(k) / 9.0) * 0.5);
            }
        }
    }

    // The seam between the plot and the motion.
    colour = mix(colour, vec3(0.20, 0.23, 0.30),
                 smoothstep(1.5 / RENDERSIZE.y, 0.0, abs(uv.y - plotTop)));

    gl_FragColor = vec4(colour, 1.0);
}
