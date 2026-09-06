/*{
  "DESCRIPTION": "A deliberately hostile scene rendered four ways: no antialiasing, fwidth, and two supersampled ground truths, so that what fwidth buys and where it stops helping are both visible.",
  "CREDIT": "Eduardo Meneses, Learn shader art",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course"],
  "INPUTS": [
    {
      "NAME": "method",
      "TYPE": "long",
      "LABEL": "Method",
      "VALUES": [0, 1, 2, 3],
      "LABELS": ["none  step", "fwidth  1 sample", "supersample  4", "supersample  16"],
      "DEFAULT": 0
    },
    {
      "NAME": "scene",
      "TYPE": "long",
      "LABEL": "Scene",
      "VALUES": [0, 1, 2],
      "LABELS": ["zone plate", "converging rays", "both"],
      "DEFAULT": 2
    },
    { "NAME": "detail",  "TYPE": "float", "LABEL": "Detail",     "DEFAULT": 60.0, "MIN": 5.0, "MAX": 260.0 },
    { "NAME": "arms",    "TYPE": "float", "LABEL": "Rays",       "DEFAULT": 24.0, "MIN": 3.0, "MAX": 96.0 },
    { "NAME": "thin",    "TYPE": "float", "LABEL": "Ray width",  "DEFAULT": 0.35, "MIN": 0.02, "MAX": 1.00 },
    { "NAME": "drift",   "TYPE": "float", "LABEL": "Drift",      "DEFAULT": 0.10, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "prefilter","TYPE": "bool", "LABEL": "Fade thin rays", "DEFAULT": false },
    { "NAME": "ink",     "TYPE": "color", "LABEL": "Ink",        "DEFAULT": [0.90, 0.94, 1.00, 1.0] }
  ]
}*/

const float TAU = 6.28318530718;

// A zone plate: rings whose frequency rises with the square of the radius. Near
// the edge the pattern is finer than a pixel, so it is the standard torture
// test for a sampler. Whatever a renderer does wrong, it does wrong here first.
float zone(vec2 p) {
    float r = dot(p, p);
    return sin(r * detail + TIME * drift * 3.0);
}

// A fan of rays converging on a point. Every ray is the same width in angle, so
// its width in pixels goes to zero at the centre: an honest sampler fades them
// out, and a naive one turns them into a moiré rosette.
float fan(vec2 p) {
    float a = atan(p.y, p.x) / TAU + TIME * drift * 0.05;
    float w = fract(a * arms);
    return (abs(w - 0.5) * 2.0) - (1.0 - thin);
}

// The scene, as a signed value: positive is ink.
float field(vec2 p) {
    if (scene == 0) return zone(p);
    if (scene == 1) return -fan(p);
    return max(zone(p) * 0.8, -fan(p));
}

// One sample, thresholded hard. This is what the hardware would do with no help
// at all, and it is the ground truth that supersampling averages.
float hard(vec2 p) {
    return step(0.0, field(p));
}

void main() {
    vec2 p = (isf_FragNormCoord - 0.5) * RENDERSIZE / RENDERSIZE.y;
    float px = 1.0 / RENDERSIZE.y;   // one pixel, in field units

    float cov;

    if (method == 0) {
        cov = hard(p);

    } else if (method == 1) {
        // One sample, plus the derivative. fwidth measures how fast the field
        // changes between neighbouring pixels, so the smoothstep width is
        // exactly the band the edge occupies. It costs almost nothing and it
        // fixes an edge. It cannot fix detail finer than a pixel, because there
        // is no information in one sample about what happened between samples.
        float d = field(p);
        float w = fwidth(d);
        cov = smoothstep(-w, w, d);

        if (prefilter) {
            // What to do about rays too thin to sample: do not try to draw
            // them, fade them. Coverage below a pixel is genuinely partial, so
            // scaling the ink by the sub-pixel width is the correct answer and
            // it is what a good line renderer has always done.
            float rayPx = length(p) * TAU / max(arms, 1.0) * thin / px;
            cov *= clamp(rayPx, 0.0, 1.0);
        }

    } else {
        // Supersampling: take the samples that were missing. Cost is linear in
        // the sample count and it is the only thing that helps with detail
        // finer than a pixel, which is why an offline renderer does it and a
        // real-time one usually cannot afford to.
        int n = (method == 2) ? 2 : 4;
        float total = 0.0;
        for (int y = 0; y < 4; y++) {
            for (int x = 0; x < 4; x++) {
                if (x >= n || y >= n) continue;
                vec2 off = (vec2(float(x), float(y)) + 0.5) / float(n) - 0.5;
                total += hard(p + off * px);
            }
        }
        cov = total / float(n * n);
    }

    vec3 colour = mix(vec3(0.04, 0.05, 0.07), ink.rgb, cov);
    gl_FragColor = vec4(colour, 1.0);
}
