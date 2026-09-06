/*{
  "DESCRIPTION": "Three layers composited with selectable blend modes, showing the difference between premultiplied and straight alpha and why the order of the operators is not a detail.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The separable blend modes follow the PDF and CSS compositing definitions.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Filter"],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image", "LABEL": "Backdrop" },
    {
      "NAME": "blend",
      "TYPE": "long",
      "LABEL": "Blend mode",
      "VALUES": [0, 1, 2, 3, 4, 5, 6, 7, 8],
      "LABELS": ["normal", "add", "multiply", "screen", "overlay", "soft light", "difference", "colour dodge", "linear burn"],
      "DEFAULT": 3
    },
    { "NAME": "opacity",  "TYPE": "float",   "LABEL": "Opacity",   "DEFAULT": 0.85, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "origin",   "TYPE": "point2D", "LABEL": "Layer position", "DEFAULT": [0.42, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "size",     "TYPE": "float",   "LABEL": "Layer size", "DEFAULT": 0.26, "MIN": 0.05, "MAX": 0.7 },
    { "NAME": "softness", "TYPE": "float",   "LABEL": "Edge softness", "DEFAULT": 0.10, "MIN": 0.0, "MAX": 0.5 },
    { "NAME": "layerHue", "TYPE": "float",   "LABEL": "Layer hue", "DEFAULT": 0.10, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "linear",   "TYPE": "bool",    "LABEL": "Blend in linear light", "DEFAULT": true },
    { "NAME": "premul",   "TYPE": "bool",    "LABEL": "Premultiplied alpha", "DEFAULT": true },
    { "NAME": "showAlpha","TYPE": "bool",    "LABEL": "Show the layer's alpha", "DEFAULT": false }
  ]
}*/

const float TAU = 6.28318530718;

vec3 srgbToLinear(vec3 c) {
    return mix(c / 12.92, pow((c + 0.055) / 1.055, vec3(2.4)), step(vec3(0.04045), c));
}
vec3 linearToSrgb(vec3 c) {
    c = max(c, 0.0);
    return mix(c * 12.92, 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055, step(vec3(0.0031308), c));
}

vec3 palette(float t) {
    return 0.5 + 0.46 * cos(TAU * (vec3(t) + vec3(0.0, 0.30, 0.62)));
}

// The separable blend functions, each defined per channel on the backdrop b and
// the source s. These are the definitions from the PDF imaging model, which CSS
// and every compositing application inherited; they are worth knowing as
// formulas rather than as menu items, because most of them are two operations.
vec3 blendFn(vec3 b, vec3 s) {
    if (blend == 1) return b + s;                                  // add
    if (blend == 2) return b * s;                                  // multiply
    if (blend == 3) return b + s - b * s;                          // screen
    if (blend == 4) {                                              // overlay
        return mix(2.0 * b * s, 1.0 - 2.0 * (1.0 - b) * (1.0 - s), step(0.5, b));
    }
    if (blend == 5) {                                              // soft light
        vec3 d = mix(((16.0 * b - 12.0) * b + 4.0) * b, sqrt(max(b, 0.0)), step(0.25, b));
        return mix(b - (1.0 - 2.0 * s) * b * (1.0 - b),
                   b + (2.0 * s - 1.0) * (d - b), step(0.5, s));
    }
    if (blend == 6) return abs(b - s);                             // difference
    if (blend == 7) return b / max(1.0 - s, 1e-4);                 // colour dodge
    if (blend == 8) return b + s - 1.0;                            // linear burn
    return s;                                                      // normal
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * vec2(RENDERSIZE.x / RENDERSIZE.y, 1.0);
    vec2 c = (origin - 0.5) * vec2(RENDERSIZE.x / RENDERSIZE.y, 1.0);

    vec3 backdrop = IMG_NORM_PIXEL(inputImage, uv).rgb;

    // The layer: a soft disc with a palette across it, so the blend has both a
    // hard region and a gradient to work on.
    float d = length(p - c) - size;
    float alpha = smoothstep(softness + fwidth(d), -fwidth(d), d) * opacity;
    vec3 layer = palette(layerHue + 0.5 * (0.5 + 0.5 * (p.x - c.x) / max(size, 1e-3)));

    if (showAlpha) {
        gl_FragColor = vec4(vec3(alpha), 1.0);
        return;
    }

    // Blend in linear light or in code space. Almost every blend mode was
    // defined on codes, so the "correct" linear version genuinely looks
    // different from what a designer expects; this course's position is that
    // add and screen belong in linear light because they model light arriving,
    // and that multiply and overlay are stylistic and belong wherever they look
    // right. The switch is here so a reader can decide rather than be told.
    vec3 b = linear ? srgbToLinear(backdrop) : backdrop;
    vec3 s = linear ? srgbToLinear(layer) : layer;

    vec3 blended = blendFn(b, s);

    // The composite. Straight alpha interpolates between backdrop and blend;
    // premultiplied adds a source that has already been scaled by its own
    // alpha. They agree wherever alpha is 0 or 1 and differ across every soft
    // edge, which is why a badly composited image looks wrong only at its
    // boundaries.
    vec3 result;
    if (premul) {
        result = blended * alpha + b * (1.0 - alpha);
    } else {
        result = mix(b, blended, alpha);
    }

    vec3 outColour = linear ? linearToSrgb(result) : result;
    gl_FragColor = vec4(clamp(outColour, 0.0, 1.0), 1.0);
}
