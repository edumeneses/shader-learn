/*{
  "DESCRIPTION": "A colour grade in the order a colourist works: exposure, white balance, contrast, lift/gamma/gain, saturation, then a tone curve, each on its own control and each applied in the space it belongs in.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The filmic curve is Krzysztof Narkowicz's ACES approximation; Reinhard tone mapping is Erik Reinhard's; lift/gamma/gain follows the ASC CDL.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Color"],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image", "LABEL": "Source" },
    { "NAME": "exposure",   "TYPE": "float", "LABEL": "Exposure  stops", "DEFAULT": 0.0,  "MIN": -3.0, "MAX": 3.0 },
    { "NAME": "temperature","TYPE": "float", "LABEL": "Temperature",     "DEFAULT": 0.0,  "MIN": -1.0, "MAX": 1.0 },
    { "NAME": "tintGM",     "TYPE": "float", "LABEL": "Tint  green-magenta", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0 },
    { "NAME": "contrast",   "TYPE": "float", "LABEL": "Contrast",        "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 2.5 },
    { "NAME": "pivot",      "TYPE": "float", "LABEL": "Contrast pivot",  "DEFAULT": 0.18, "MIN": 0.05, "MAX": 0.60 },
    { "NAME": "lift",       "TYPE": "color", "LABEL": "Lift  shadows",   "DEFAULT": [0.5, 0.5, 0.5, 1.0] },
    { "NAME": "gammaC",     "TYPE": "color", "LABEL": "Gamma  midtones", "DEFAULT": [0.5, 0.5, 0.5, 1.0] },
    { "NAME": "gainC",      "TYPE": "color", "LABEL": "Gain  highlights","DEFAULT": [0.5, 0.5, 0.5, 1.0] },
    { "NAME": "saturation", "TYPE": "float", "LABEL": "Saturation",      "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 2.5 },
    {
      "NAME": "curve",
      "TYPE": "long",
      "LABEL": "Tone curve",
      "VALUES": [0, 1, 2],
      "LABELS": ["none  clip", "Reinhard", "filmic  ACES fit"],
      "DEFAULT": 0
    },
    { "NAME": "split",      "TYPE": "float", "LABEL": "Compare", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "scope",      "TYPE": "bool",  "LABEL": "Show the histogram", "DEFAULT": true }
  ]
}*/

vec3 srgbToLinear(vec3 c) {
    return mix(c / 12.92, pow((c + 0.055) / 1.055, vec3(2.4)), step(vec3(0.04045), c));
}
vec3 linearToSrgb(vec3 c) {
    c = max(c, 0.0);
    return mix(c * 12.92, 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055, step(vec3(0.0031308), c));
}

const vec3 LUMA = vec3(0.2126, 0.7152, 0.0722);

// Narkowicz's fit to the ACES filmic tone curve: one line, no matrices, and it
// is what most real-time work actually ships. It rolls the highlights off
// instead of clipping them, which is the difference between a bright area that
// has shape and one that is a white hole.
vec3 acesFilm(vec3 x) {
    const float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

vec3 grade(vec3 srgb) {
    // Every operation below is on light, not on codes. Unit 04's rule: convert
    // when combining or scaling, and a grade is nothing but scaling.
    vec3 c = srgbToLinear(srgb);

    // Exposure, in stops, because that is the unit a photographer thinks in and
    // one stop is exactly a doubling.
    c *= exp2(exposure);

    // White balance, as a cheap channel scale. A correct one would go through a
    // chromatic adaptation matrix; this is the version that fits on one line
    // and is what most live tools do.
    c *= vec3(1.0 + temperature * 0.30, 1.0 + tintGM * 0.18, 1.0 - temperature * 0.30);

    // Contrast about a pivot. The pivot matters: scaling about zero darkens
    // everything as contrast rises, and 0.18 is middle grey, which is why it is
    // the default in every grading tool.
    c = max(pivot + (c - pivot) * contrast, 0.0);

    // Lift, gamma, gain, the three-way colour balance. The pickers default to
    // mid grey and are read as offsets from it, so an untouched picker changes
    // nothing. Lift moves the shadows, gain scales the highlights, and gamma
    // bends the middle without moving either end.
    vec3 L = (lift.rgb - 0.5) * 0.5;
    vec3 G = (gainC.rgb - 0.5) * 2.0 + 1.0;
    vec3 M = pow(vec3(2.0), -(gammaC.rgb - 0.5) * 2.0);
    c = pow(max(c * G + L, 0.0), M);

    // Saturation, against luminance rather than against the channel average,
    // for the reason Unit 04 gave.
    float y = dot(c, LUMA);
    c = mix(vec3(y), c, saturation);

    // The tone curve, last, and this is the operation that decides whether the
    // grade above it had room to work. Without one, anything the grade pushed
    // above 1 is simply gone.
    if (curve == 1) c = c / (1.0 + c);
    else if (curve == 2) c = acesFilm(c);

    return linearToSrgb(clamp(c, 0.0, 1.0));
}

void main() {
    vec2 uv = isf_FragNormCoord;

    vec3 original = IMG_NORM_PIXEL(inputImage, uv).rgb;
    vec3 result = grade(original);

    vec3 colour = uv.x < split ? result : original;

    if (split > 0.001 && split < 0.999) {
        colour = mix(colour, vec3(1.0),
                     smoothstep(1.5 / RENDERSIZE.x, 0.0, abs(uv.x - split)) * 0.8);
    }

    if (scope && uv.y < 0.18) {
        // A luminance histogram of the graded image, sampled along a row rather
        // than over the whole frame: a fragment shader cannot accumulate across
        // pixels, so a real histogram needs a compute pass. This is an honest
        // approximation and the unit says so.
        float bin = uv.x;
        float count = 0.0;
        const int SAMPLES = 96;
        for (int i = 0; i < SAMPLES; i++) {
            float t = (float(i) + 0.5) / float(SAMPLES);
            vec3 s = grade(IMG_NORM_PIXEL(inputImage, vec2(t, fract(t * 7.3))).rgb);
            float y = dot(s, LUMA);
            count += smoothstep(0.03, 0.0, abs(y - bin));
        }
        float h = clamp(count / 16.0, 0.0, 1.0) * 0.16;
        vec3 bars = mix(vec3(0.05, 0.06, 0.09), vec3(0.85, 0.90, 0.98),
                        step(uv.y, h));
        colour = mix(colour, bars, 0.86);
        colour = mix(colour, vec3(0.3), smoothstep(2.0 / RENDERSIZE.y, 0.0, abs(uv.y - 0.18)));
    }

    gl_FragColor = vec4(colour, 1.0);
}
