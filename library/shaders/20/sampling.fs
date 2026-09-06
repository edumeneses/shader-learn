/*{
  "DESCRIPTION": "Reading a texture: normalised against pixel coordinates, what happens between texels, and the coordinate mistakes that produce a flipped, stretched, or crawling image.",
  "CREDIT": "Eduardo Meneses, Learn shader art",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Filter"],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image", "LABEL": "Source" },
    {
      "NAME": "mode",
      "TYPE": "long",
      "LABEL": "Mapping",
      "VALUES": [0, 1, 2, 3, 4],
      "LABELS": ["fit: correct", "stretch to fill", "fill and crop", "flipped Y", "pixel coordinates, unnormalised"],
      "DEFAULT": 0
    },
    { "NAME": "zoom",     "TYPE": "float",   "LABEL": "Zoom",     "DEFAULT": 1.0, "MIN": 0.2, "MAX": 24.0 },
    { "NAME": "centre",   "TYPE": "point2D", "LABEL": "Centre",   "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "quantise", "TYPE": "float",   "LABEL": "Sample grid", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 400.0 },
    { "NAME": "showGrid", "TYPE": "bool",    "LABEL": "Mark the edges", "DEFAULT": false }
  ]
}*/

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 size = IMG_SIZE(inputImage);
    float outAspect = RENDERSIZE.x / RENDERSIZE.y;
    float inAspect = size.x / size.y;

    // Zoom about a point, in normalised space. Unit 09's inverse rule again:
    // to make the image appear larger, the lookup coordinate contracts.
    vec2 p = (uv - centre) / zoom + centre;

    vec2 lookup;

    if (mode == 0) {
        // Fit: scale so the whole image is visible, letterboxing the rest. The
        // correction is applied to whichever axis has room to spare.
        vec2 scale = inAspect > outAspect
            ? vec2(1.0, outAspect / inAspect)
            : vec2(inAspect / outAspect, 1.0);
        lookup = (p - 0.5) / scale + 0.5;

    } else if (mode == 1) {
        // Stretch: ignore the aspect ratio entirely. This is what a shader that
        // uses isf_FragNormCoord directly does, and on a source and an output
        // with different shapes it distorts.
        lookup = p;

    } else if (mode == 2) {
        // Fill: scale so no gap remains and let the overflow crop. The same
        // arithmetic as fit with the comparison reversed.
        vec2 scale = inAspect > outAspect
            ? vec2(inAspect / outAspect, 1.0)
            : vec2(1.0, outAspect / inAspect);
        lookup = (p - 0.5) / scale + 0.5;

    } else if (mode == 3) {
        // The classic. One sign, and the image is upside down. It happens
        // because texture space and window space disagree about which way y
        // runs, and it is the reason ISF supplies isf_FragNormCoord rather than
        // letting a shader reach for gl_FragCoord.
        vec2 scale = inAspect > outAspect
            ? vec2(1.0, outAspect / inAspect)
            : vec2(inAspect / outAspect, 1.0);
        lookup = (p - 0.5) / scale + 0.5;
        lookup.y = 1.0 - lookup.y;

    } else {
        // Pixel coordinates, used without dividing by the texture size. The
        // whole image collapses into the first texel, because a coordinate of
        // 900 is far outside the 0..1 range a sampler expects. Recognisable
        // once, confusing forever otherwise.
        lookup = p * RENDERSIZE;
    }

    // Snapping the lookup to a coarse grid shows what a sampler does *between*
    // texels: with the grid on, each cell reads one point and holds it, which
    // is nearest-neighbour sampling drawn large.
    if (quantise >= 1.0) {
        lookup = (floor(lookup * quantise) + 0.5) / quantise;
    }

    vec3 colour = IMG_NORM_PIXEL(inputImage, lookup).rgb;

    // Outside the image there is nothing. Showing that as a colour rather than
    // as a clamped smear is the honest thing to do, and it is what makes the
    // fit and fill modes legible.
    vec2 outside = max(abs(lookup - 0.5) - 0.5, 0.0);
    float off = step(1e-6, max(outside.x, outside.y));
    colour = mix(colour, vec3(0.06, 0.07, 0.10), off);

    if (showGrid) {
        vec2 edge = min(lookup, 1.0 - lookup);
        float border = min(edge.x, edge.y);
        colour = mix(colour, vec3(1.0, 0.25, 0.35),
                     smoothstep(fwidth(border) * 1.5, 0.0, abs(border)) * 0.9);
    }

    gl_FragColor = vec4(colour, 1.0);
}
