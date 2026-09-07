/*{
  "DESCRIPTION": "One circle's edge drawn four ways, magnified, so the difference between a hard step, a fixed-width blend, and a pixel-width blend is visible rather than described.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The rounded-box distance function is Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course"],
  "INPUTS": [
    {
      "NAME": "method",
      "TYPE": "long",
      "LABEL": "Edge",
      "VALUES": [0, 1, 2, 3],
      "LABELS": ["step: hard", "linear ramp", "smoothstep: fixed width", "smoothstep: one pixel"],
      "DEFAULT": 0
    },
    { "NAME": "focus",  "TYPE": "point2D", "LABEL": "Magnifier", "DEFAULT": [0.66, 0.62], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "snap",   "TYPE": "bool",    "LABEL": "Snap to edge", "DEFAULT": true },
    { "NAME": "zoom",   "TYPE": "float",   "LABEL": "Zoom",      "DEFAULT": 14.0, "MIN": 1.0, "MAX": 48.0 },
    { "NAME": "width",  "TYPE": "float",   "LABEL": "Width",     "DEFAULT": 0.012, "MIN": 0.0, "MAX": 0.20 },
    { "NAME": "radius", "TYPE": "float",   "LABEL": "Radius",    "DEFAULT": 0.30, "MIN": 0.05, "MAX": 0.45 },
    { "NAME": "spin",   "TYPE": "float",   "LABEL": "Spin",      "DEFAULT": 0.10, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "ink",    "TYPE": "color",   "LABEL": "Ink",       "DEFAULT": [0.27, 0.88, 0.83, 1.0] }
  ]
}*/

const float TAU = 6.28318530718;

// The field, in the centred, height-divided space of Unit 03. A slow rotation
// so the edge is never axis-aligned: an aliased edge on a perfect horizontal
// looks fine, and it is the diagonal that gives the game away.
float field(vec2 p) {
    // A standing tilt plus the drift: an aliased edge on a perfect horizontal or
    // vertical looks fine, so the shape is never allowed to be axis-aligned.
    float a = 0.32 + TIME * spin * TAU * 0.1;
    mat2 rot = mat2(cos(a), -sin(a), sin(a), cos(a));
    vec2 q = rot * p;
    // A rounded square rather than a circle, so there are both curves and
    // near-straight runs in one shape.
    vec2 d = abs(q) - vec2(radius, radius * 0.72);
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - 0.06;
}

float coverage(float d, float pixel) {
    if (method == 0) {
        // No blend at all: every pixel is fully in or fully out, so the edge
        // can only lie on pixel boundaries and it staircases.
        return step(d, 0.0);
    } else if (method == 1) {
        // A linear ramp of a chosen width. Better than nothing and visibly
        // linear: the ramp has corners where it meets 0 and 1.
        return clamp(0.5 - d / max(width, 1e-5), 0.0, 1.0);
    } else if (method == 2) {
        // smoothstep over the same width. The cubic has zero slope at both
        // ends, so the transition has no corners, but the width is in field
        // units and therefore changes with zoom and resolution.
        return smoothstep(width, -width, d);
    } else {
        // The width the edge actually wants: how much the field changes across
        // one pixel. Unit 11 is this line. It is one pixel wide at any zoom.
        float aa = fwidth(d) * 0.75;
        return smoothstep(aa, -aa, d);
    }
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y;

    // The magnifier: a disc that shows the same field sampled from a small
    // region, at the same screen resolution, which is what makes an aliasing
    // artefact large enough to see on a page.
    vec2 centre = (focus - 0.5) * RENDERSIZE / RENDERSIZE.y;
    float lensR = 0.28;
    float inLens = step(length(p - centre), lensR);

    // Snap the magnified region onto the nearest edge. At fourteen times, the
    // lens sees about two hundredths of a unit, so a magnifier that showed
    // wherever the reader dropped it would show flat colour almost always and
    // the figure would look broken rather than instructive. A distance field
    // makes the correction one step: its value is the distance to the edge and
    // its gradient is the direction, so stepping by one against the other lands
    // on the edge from anywhere. Unit 24 marches a ray with the same fact.
    vec2 origin = centre;
    if (snap) {
        float e = 0.002;
        float d0 = field(centre);
        vec2 grad = vec2(
            field(centre + vec2(e, 0.0)) - field(centre - vec2(e, 0.0)),
            field(centre + vec2(0.0, e)) - field(centre - vec2(0.0, e)));
        float len = max(length(grad), 1e-6);
        origin = centre - (grad / len) * d0;
    }

    vec2 probe = mix(p, origin + (p - centre) / zoom, inLens);

    float d = field(probe);
    // fwidth must be taken on the coordinate actually being shown, or the
    // magnified region would be antialiased for the wrong scale. Because
    // `probe` is a real varying-derived value, fwidth inside coverage()
    // measures the magnified rate of change, which is the honest thing to show.
    float cov = coverage(d, 1.0);

    vec3 background = vec3(0.05, 0.06, 0.09);
    vec3 colour = mix(background, ink.rgb, cov);

    // The lens rim, and a dimming of everything outside it, so the eye goes to
    // the magnified edge rather than to the whole shape.
    float rim = abs(length(p - centre) - lensR);
    colour *= mix(0.55, 1.0, inLens);
    colour = mix(colour, vec3(0.85, 0.88, 0.95), smoothstep(3.0 / RENDERSIZE.y, 0.0, rim));

    gl_FragColor = vec4(colour, 1.0);
}
