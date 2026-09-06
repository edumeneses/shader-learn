/*{
  "DESCRIPTION": "Two fields combined six ways, with the smoothing radius on a control, shown as a field so that what the operators do to the space between the shapes is visible.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The smooth minimum is Inigo Quilez's polynomial form.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Generator"],
  "INPUTS": [
    {
      "NAME": "op",
      "TYPE": "long",
      "LABEL": "Operator",
      "VALUES": [0, 1, 2, 3, 4, 5],
      "LABELS": ["union  min", "intersection  max", "subtraction  max(a,-b)", "smooth union", "smooth intersection", "smooth subtraction"],
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
    { "NAME": "left",   "TYPE": "point2D", "LABEL": "Left shape",  "DEFAULT": [0.40, 0.50], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "right",  "TYPE": "point2D", "LABEL": "Right shape", "DEFAULT": [0.60, 0.50], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "sizeA",  "TYPE": "float",   "LABEL": "Left size",   "DEFAULT": 0.20, "MIN": 0.02, "MAX": 0.50 },
    { "NAME": "sizeB",  "TYPE": "float",   "LABEL": "Right size",  "DEFAULT": 0.16, "MIN": 0.02, "MAX": 0.50 },
    { "NAME": "k",      "TYPE": "float",   "LABEL": "Smoothing",   "DEFAULT": 0.12, "MIN": 0.001, "MAX": 0.40 },
    { "NAME": "rings",  "TYPE": "float",   "LABEL": "Isolines",    "DEFAULT": 90.0, "MIN": 0.0, "MAX": 300.0 },
    { "NAME": "warm",   "TYPE": "color",   "LABEL": "Outside",     "DEFAULT": [0.95, 0.62, 0.28, 1.0] },
    { "NAME": "cool",   "TYPE": "color",   "LABEL": "Inside",      "DEFAULT": [0.30, 0.72, 0.95, 1.0] }
  ]
}*/

// The polynomial smooth minimum. It blends the two distances over a band of
// width k and, unlike min, is differentiable everywhere, so the seam has no
// crease. The h term is the fraction of the way from one field to the other;
// the last term is the correction that keeps the result close to a true
// distance rather than merely close to the right sign.
float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

// Every other smooth operator is this one with signs flipped, which is worth
// knowing because it means there is one function to get right rather than three.
float smax(float a, float b, float k) {
    return -smin(-a, -b, k);
}

float combine(float a, float b) {
    if (op == 0) return min(a, b);          // union: nearest surface wins
    if (op == 1) return max(a, b);          // intersection: both must be inside
    if (op == 2) return max(a, -b);         // subtraction: inside a, outside b
    if (op == 3) return smin(a, b, k);
    if (op == 4) return smax(a, b, k);
    return smax(a, -b, k);
}

void main() {
    vec2 p = (isf_FragNormCoord - 0.5) * RENDERSIZE / RENDERSIZE.y;
    vec2 ca = (left  - 0.5) * RENDERSIZE / RENDERSIZE.y;
    vec2 cb = (right - 0.5) * RENDERSIZE / RENDERSIZE.y;

    float a = length(p - ca) - sizeA;
    float b = length(p - cb) - sizeB;

    float d = combine(a, b);
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
