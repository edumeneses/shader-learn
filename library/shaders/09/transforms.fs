/*{
  "DESCRIPTION": "Translation, rotation, and scale applied to the space rather than to the shape, with a mode that shows what happens when you forget to compensate the scale.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The cross distance function is Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Generator"],
  "INPUTS": [
    {
      "NAME": "mode",
      "TYPE": "long",
      "LABEL": "Scale handling",
      "VALUES": [0, 1, 2],
      "LABELS": ["correct: divide p, multiply d", "wrong: divide p only", "wrong: non-uniform, uncompensated"],
      "DEFAULT": 0
    },
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "View",
      "VALUES": [0, 1],
      "LABELS": ["the field", "the shape only"],
      "DEFAULT": 0
    },
    { "NAME": "origin", "TYPE": "point2D", "LABEL": "Translate", "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "turn",   "TYPE": "float",   "LABEL": "Rotate",    "DEFAULT": 0.08, "MIN": -0.5, "MAX": 0.5 },
    { "NAME": "scale",  "TYPE": "float",   "LABEL": "Scale",     "DEFAULT": 1.00, "MIN": 0.20, "MAX": 3.00 },
    { "NAME": "stretch","TYPE": "float",   "LABEL": "Stretch x", "DEFAULT": 1.00, "MIN": 0.30, "MAX": 3.00 },
    { "NAME": "size",   "TYPE": "float",   "LABEL": "Size",      "DEFAULT": 0.22, "MIN": 0.04, "MAX": 0.50 },
    { "NAME": "rings",  "TYPE": "float",   "LABEL": "Isolines",  "DEFAULT": 90.0, "MIN": 0.0, "MAX": 300.0 },
    { "NAME": "warm",   "TYPE": "color",   "LABEL": "Outside",   "DEFAULT": [0.95, 0.62, 0.28, 1.0] },
    { "NAME": "cool",   "TYPE": "color",   "LABEL": "Inside",    "DEFAULT": [0.30, 0.72, 0.95, 1.0] }
  ]
}*/

const float TAU = 6.28318530718;

mat2 rot(float turns) {
    float a = turns * TAU;
    float c = cos(a), s = sin(a);
    return mat2(c, -s, s, c);
}

// A cross, so that rotation is unmistakable and non-uniform scale is obvious.
float sdCross(vec2 p, vec2 b, float r) {
    p = abs(p);
    if (p.y > p.x) p = p.yx;
    vec2 q = p - b;
    float k = max(q.y, q.x);
    vec2 w = k > 0.0 ? q : vec2(b.y - p.x, -k);
    return sign(k) * length(max(w, 0.0)) + r;
}

float shape(vec2 p) {
    return sdCross(p, vec2(size, size * 0.30), -0.02);
}

void main() {
    vec2 p = (isf_FragNormCoord - 0.5) * RENDERSIZE / RENDERSIZE.y;
    vec2 t = (origin - 0.5) * RENDERSIZE / RENDERSIZE.y;

    float d;

    // Every one of these transforms the *coordinate*, not the shape. To move a
    // shape to the right you subtract from p, because you are asking "where
    // would this pixel be if the shape were at the origin". The transform you
    // write is the inverse of the transform you see, always.
    if (mode == 0) {
        vec2 q = rot(-turn) * (p - t) / scale;
        // Dividing the coordinate by s shrinks the space, so distances measured
        // in it are s times too small. Multiplying the result back is what
        // keeps the field a true distance, which everything downstream needs.
        d = shape(q) * scale;

    } else if (mode == 1) {
        vec2 q = rot(-turn) * (p - t) / scale;
        // The same shape, drawn correctly, with a field that lies about
        // distance by a factor of `scale`. The outline is right and the
        // isolines are wrong, which is why this bug survives until the field is
        // used for something other than a threshold.
        d = shape(q);

    } else {
        vec2 q = rot(-turn) * (p - t) / vec2(scale * stretch, scale);
        // Non-uniform scale cannot be undone by one multiplication, because the
        // space is now stretched differently along different directions. The
        // conservative fix is to multiply by the smallest scale factor, which
        // never over-estimates the distance and therefore never breaks a
        // raymarcher; it is what Unit 25 has to do in three dimensions.
        d = shape(q) * min(scale * stretch, scale);
    }

    float aa = fwidth(d);

    vec3 colour;
    if (view == 1) {
        colour = mix(vec3(0.05, 0.06, 0.09), cool.rgb, smoothstep(aa, -aa, d));
    } else {
        colour = d > 0.0 ? warm.rgb : cool.rgb;
        colour *= 1.0 - exp(-7.0 * abs(d));
        colour *= 0.85 + 0.15 * cos(rings * d);
        colour = mix(colour, vec3(1.0), 1.0 - smoothstep(0.0, aa * 2.0, abs(d)));
    }

    gl_FragColor = vec4(colour, 1.0);
}
