/*{
  "DESCRIPTION": "The same circle drawn in three coordinate spaces, so the aspect-ratio mistake and its two fixes are visible side by side.",
  "CREDIT": "Eduardo Meneses, Learn shader art.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course"],
  "INPUTS": [
    {
      "NAME": "space",
      "TYPE": "long",
      "LABEL": "Coordinate space",
      "VALUES": [0, 1, 2],
      "LABELS": ["0..1, unfixed", "centred, divided by height", "centred, aspect applied"],
      "DEFAULT": 0
    },
    { "NAME": "focus",  "TYPE": "point2D", "LABEL": "Focus",  "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "radius", "TYPE": "float",   "LABEL": "Radius", "DEFAULT": 0.25, "MIN": 0.02, "MAX": 0.9 },
    { "NAME": "grid",   "TYPE": "float",   "LABEL": "Grid",   "DEFAULT": 8.0,  "MIN": 0.0,  "MAX": 32.0 },
    { "NAME": "tint",   "TYPE": "color",   "LABEL": "Tint",   "DEFAULT": [0.27, 0.88, 0.83, 1.0] }
  ]
}*/

void main() {
    vec2 uv = isf_FragNormCoord;
    float aspect = RENDERSIZE.x / RENDERSIZE.y;

    vec2 p;      // the coordinate the shape is measured in
    vec2 centre; // the focus, brought into the same space
    float cell;  // the grid spacing, so the distortion is measurable

    if (space == 0) {
        // The mistake. 0..1 in both directions means one unit of x is wider
        // than one unit of y on any screen that is not square, so a set of
        // points at equal distance is an ellipse.
        p = uv;
        centre = focus;
        cell = 1.0 / grid;

    } else if (space == 1) {
        // Fix one, and the one this course uses. Centre on zero and divide both
        // axes by the same number, the height. x now runs past 1 on a wide
        // screen, which is correct: the screen really is wider than it is tall.
        p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y;
        centre = (focus - 0.5) * RENDERSIZE / RENDERSIZE.y;
        cell = 1.0 / grid;

    } else {
        // Fix two. Keep the 0..1 range and multiply x by the aspect ratio only
        // where a distance is measured. It works, and it is easy to forget one
        // of the places, which is why fix one is the one to keep.
        p = (uv - 0.5) * vec2(aspect, 1.0);
        centre = (focus - 0.5) * vec2(aspect, 1.0);
        cell = 1.0 / grid;
    }

    float d = length(p - centre) - radius;

    // A grid in the same space as the shape. Squares mean the space is square.
    vec2 g = abs(fract(p / cell) - 0.5) / fwidth(p / cell);
    float lines = 1.0 - min(min(g.x, g.y), 1.0);

    float aa = fwidth(d);
    float disc = smoothstep(aa, -aa, d);

    vec3 colour = vec3(0.05, 0.06, 0.09);
    colour = mix(colour, vec3(0.16, 0.18, 0.24), lines * step(0.5, grid));
    colour = mix(colour, tint.rgb, disc);
    colour = mix(colour, vec3(1.0), smoothstep(aa * 2.0, 0.0, abs(d)) * 0.6);

    gl_FragColor = vec4(colour, 1.0);
}
