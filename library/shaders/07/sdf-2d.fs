/*{
  "DESCRIPTION": "Six two-dimensional signed distance functions, shown either as a shape or as the whole field, so that what a field is stops being an abstraction.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The distance functions and the field visualisation are Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Generator"],
  "INPUTS": [
    {
      "NAME": "shape",
      "TYPE": "long",
      "LABEL": "Shape",
      "VALUES": [0, 1, 2, 3, 4, 5],
      "LABELS": ["circle", "box", "rounded box", "segment", "triangle", "hexagon"],
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
    { "NAME": "origin", "TYPE": "point2D", "LABEL": "Origin", "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "handle", "TYPE": "point2D", "LABEL": "Handle", "DEFAULT": [0.72, 0.66], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "size",   "TYPE": "float",   "LABEL": "Size",   "DEFAULT": 0.26, "MIN": 0.02, "MAX": 0.60 },
    { "NAME": "round",  "TYPE": "float",   "LABEL": "Round",  "DEFAULT": 0.06, "MIN": 0.00, "MAX": 0.30 },
    { "NAME": "rings",  "TYPE": "float",   "LABEL": "Isolines", "DEFAULT": 90.0, "MIN": 0.0, "MAX": 300.0 },
    { "NAME": "warm",   "TYPE": "color",   "LABEL": "Outside", "DEFAULT": [0.95, 0.62, 0.28, 1.0] },
    { "NAME": "cool",   "TYPE": "color",   "LABEL": "Inside",  "DEFAULT": [0.30, 0.72, 0.95, 1.0] }
  ]
}*/

// --- the distance functions -------------------------------------------------
//
// Each returns the signed distance from p to the shape's boundary: negative
// inside, zero on it, positive outside. They are exact, which matters more than
// it sounds: an exact field can be offset, combined, and marched along, and an
// approximate one can only be thresholded.

float sdCircle(vec2 p, float r) {
    return length(p) - r;
}

// The pattern to learn. abs(p) folds the plane into one quadrant, so only the
// top right corner has to be reasoned about. max(d, 0.0) handles the region
// diagonally outside the corner; min(max(d.x, d.y), 0.0) handles the inside,
// where the distance is to the nearest face.
float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

// Rounding is subtraction. Any field minus a constant is that shape grown by
// the constant, with every corner rounded to that radius for free.
float sdRoundedBox(vec2 p, vec2 b, float r) {
    return sdBox(p, b - r) - r;
}

// Project p onto the segment, clamp the projection to the segment's extent,
// then measure. The clamp is what makes the ends caps rather than an infinite
// line.
float sdSegment(vec2 p, vec2 a, vec2 b, float r) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - r;
}

float sdEquilateralTriangle(vec2 p, float r) {
    const float k = 1.73205081;               // sqrt(3)
    p.x = abs(p.x) - r;
    p.y = p.y + r / k;
    if (p.x + k * p.y > 0.0) {
        p = vec2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
    }
    p.x -= clamp(p.x, -2.0 * r, 0.0);
    return -length(p) * sign(p.y);
}

float sdHexagon(vec2 p, float r) {
    const vec3 k = vec3(-0.866025404, 0.5, 0.577350269);
    p = abs(p);
    p -= 2.0 * min(dot(k.xy, p), 0.0) * k.xy;
    p -= vec2(clamp(p.x, -k.z * r, k.z * r), r);
    return length(p) * sign(p.y);
}

float scene(vec2 p, vec2 a, vec2 b) {
    if (shape == 0) return sdCircle(p - a, size);
    if (shape == 1) return sdBox(p - a, vec2(size, size * 0.66));
    if (shape == 2) return sdRoundedBox(p - a, vec2(size, size * 0.66), min(round, size * 0.65));
    if (shape == 3) return sdSegment(p, a, b, max(round, 0.01));
    if (shape == 4) return sdEquilateralTriangle(p - a, size);
    return sdHexagon(p - a, size);
}

void main() {
    vec2 p = (isf_FragNormCoord - 0.5) * RENDERSIZE / RENDERSIZE.y;
    vec2 a = (origin - 0.5) * RENDERSIZE / RENDERSIZE.y;
    vec2 b = (handle - 0.5) * RENDERSIZE / RENDERSIZE.y;

    float d = scene(p, a, b);
    float aa = fwidth(d);

    vec3 colour;

    if (view == 1) {
        // The shape alone. This is what a reader thinks a distance field is
        // for, and it uses about a tenth of the information the field carries.
        colour = mix(vec3(0.05, 0.06, 0.09), cool.rgb, smoothstep(aa, -aa, d));

    } else {
        // The field. Sign chooses the hue, magnitude fades it out, and the
        // isolines are curves of equal distance: read them as a contour map,
        // because that is exactly what they are.
        colour = d > 0.0 ? warm.rgb : cool.rgb;
        colour *= 1.0 - exp(-7.0 * abs(d));
        colour *= 0.85 + 0.15 * cos(rings * d);
        colour = mix(colour, vec3(1.0), 1.0 - smoothstep(0.0, aa * 2.0, abs(d)));
    }

    gl_FragColor = vec4(colour, 1.0);
}
