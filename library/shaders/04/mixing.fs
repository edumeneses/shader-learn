/*{
  "DESCRIPTION": "The same two colours blended in the space the numbers are stored in and in the space light actually adds in, above and below a split, so the muddy middle is visible rather than argued about.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The sRGB transfer functions are from the sRGB standard; the ordered dither is a Bayer matrix.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Color"],
  "INPUTS": [
    { "NAME": "left",   "TYPE": "color", "LABEL": "Left colour",  "DEFAULT": [0.10, 0.35, 0.95, 1.0] },
    { "NAME": "right",  "TYPE": "color", "LABEL": "Right colour", "DEFAULT": [1.00, 0.85, 0.10, 1.0] },
    { "NAME": "split",  "TYPE": "float", "LABEL": "Split",        "DEFAULT": 0.5,  "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "steps",  "TYPE": "float", "LABEL": "Quantise",     "DEFAULT": 0.0,  "MIN": 0.0, "MAX": 64.0 },
    { "NAME": "dither", "TYPE": "bool",  "LABEL": "Dither",       "DEFAULT": false },
    { "NAME": "swatch", "TYPE": "bool",  "LABEL": "Show midpoint","DEFAULT": true }
  ]
}*/

// sRGB is not a brightness. The number 0.5 in an image file is about 21 percent
// of the light of the number 1.0, because the file stores a value that has been
// bent by a transfer function so that the codes are spaced the way an eye
// notices differences rather than the way a photon counter would.
//
// These two functions are the sRGB transfer function and its inverse, in the
// exact piecewise form the standard defines. The popular pow(x, 2.2) is an
// approximation that is wrong near black, which is precisely where banding
// lives, so the course uses the real one.
vec3 srgbToLinear(vec3 c) {
    vec3 lo = c / 12.92;
    vec3 hi = pow((c + 0.055) / 1.055, vec3(2.4));
    return mix(lo, hi, step(vec3(0.04045), c));
}

vec3 linearToSrgb(vec3 c) {
    c = max(c, 0.0);
    vec3 lo = c * 12.92;
    vec3 hi = 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055;
    return mix(lo, hi, step(vec3(0.0031308), c));
}

// An ordered dither, one value per pixel from a 4x4 Bayer matrix, computed
// rather than looked up. Adding well under one code's worth of noise before
// quantising turns a hard band edge into a stipple the eye reads as a gradient.
float bayer(vec2 pixel) {
    vec2 p = floor(mod(pixel, 4.0));
    int i = int(p.x + p.y * 4.0);
    float m[16] = float[16](
         0.0,  8.0,  2.0, 10.0,
        12.0,  4.0, 14.0,  6.0,
         3.0, 11.0,  1.0,  9.0,
        15.0,  7.0, 13.0,  5.0);
    return (m[i] + 0.5) / 16.0;
}

vec3 quantise(vec3 c, vec2 pixel) {
    if (steps < 2.0) return c;
    float n = floor(steps);
    float d = dither ? (bayer(pixel) - 0.5) / n : 0.0;
    return floor(c * n + 0.5 + d * n) / n;
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 pixel = uv * RENDERSIZE;

    float t = uv.x;

    // The naive blend: interpolate the stored numbers. Every graphics tool that
    // does not say otherwise does this, and it is why a blue-to-yellow gradient
    // passes through a grey that neither colour suggests.
    vec3 naive = mix(left.rgb, right.rgb, t);

    // The physical blend: undo the transfer function, add the light, put the
    // transfer function back. Twice as many instructions and it is the one that
    // matches what happens when you point two lamps at the same wall.
    vec3 correct = linearToSrgb(mix(srgbToLinear(left.rgb), srgbToLinear(right.rgb), t));

    vec3 colour = uv.y > split ? naive : correct;
    colour = quantise(colour, pixel);

    // A hairline at the split, and two swatches of the midpoint so the
    // difference can be read as a colour rather than as a gradient.
    float line = smoothstep(1.5 / RENDERSIZE.y, 0.0, abs(uv.y - split));
    colour = mix(colour, vec3(0.0), line * 0.8);

    if (swatch) {
        vec2 c = abs(uv - vec2(0.5, split)) - vec2(0.075, 0.16);
        float box = max(c.x, c.y);
        float inBox = step(box, 0.0);
        float upper = step(split, uv.y);
        vec3 mid = upper > 0.5
            ? mix(left.rgb, right.rgb, 0.5)
            : linearToSrgb(mix(srgbToLinear(left.rgb), srgbToLinear(right.rgb), 0.5));
        colour = mix(colour, mid, inBox);
        colour = mix(colour, vec3(1.0), inBox * smoothstep(2.0 / RENDERSIZE.y, 0.0, abs(box)) * 0.9);
    }

    gl_FragColor = vec4(colour, 1.0);
}
