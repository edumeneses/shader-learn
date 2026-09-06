/*{
  "DESCRIPTION": "Infinite repetition, limited repetition, and polar repetition, with a cell size that can be made smaller than the shape so the tiling trap is visible.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The limited-repetition clamp is Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Generator"],
  "INPUTS": [
    {
      "NAME": "kind",
      "TYPE": "long",
      "LABEL": "Repetition",
      "VALUES": [0, 1, 2, 3],
      "LABELS": ["none", "infinite grid", "limited grid", "polar"],
      "DEFAULT": 1
    },
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "View",
      "VALUES": [0, 1],
      "LABELS": ["the field", "the shape only"],
      "DEFAULT": 1
    },
    { "NAME": "origin", "TYPE": "point2D", "LABEL": "Origin",    "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "cell",   "TYPE": "float",   "LABEL": "Cell size", "DEFAULT": 0.34, "MIN": 0.05, "MAX": 1.20 },
    { "NAME": "size",   "TYPE": "float",   "LABEL": "Shape size","DEFAULT": 0.10, "MIN": 0.01, "MAX": 0.50 },
    { "NAME": "limit",  "TYPE": "float",   "LABEL": "Limit",     "DEFAULT": 2.0,  "MIN": 0.0,  "MAX": 6.0 },
    { "NAME": "arms",   "TYPE": "float",   "LABEL": "Arms",      "DEFAULT": 7.0,  "MIN": 2.0,  "MAX": 24.0 },
    { "NAME": "turn",   "TYPE": "float",   "LABEL": "Turn",      "DEFAULT": 0.0,  "MIN": -1.0, "MAX": 1.0 },
    { "NAME": "drift",  "TYPE": "float",   "LABEL": "Drift",     "DEFAULT": 0.0,  "MIN": -1.0, "MAX": 1.0 },
    { "NAME": "rings",  "TYPE": "float",   "LABEL": "Isolines",  "DEFAULT": 90.0, "MIN": 0.0,  "MAX": 300.0 },
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

float box(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

float shape(vec2 p) {
    return box(rot(turn + TIME * drift * 0.1) * p, vec2(size, size * 0.55)) - size * 0.18;
}

void main() {
    vec2 p = (isf_FragNormCoord - 0.5) * RENDERSIZE / RENDERSIZE.y;
    p -= (origin - 0.5) * RENDERSIZE / RENDERSIZE.y;

    float d;

    if (kind == 0) {
        d = shape(p);

    } else if (kind == 1) {
        // Infinite repetition: fold the whole plane into one cell. The shape is
        // written once and appears everywhere, at no cost, forever. This is the
        // cheapest thing in shader art and it is why fields are used at all.
        //
        // The catch, which the Cell size control lets you meet: the fold is
        // only correct while the shape fits inside its cell. Make the cell
        // smaller than the shape and each cell measures the distance to its own
        // copy while a neighbour's copy is nearer, so the field lies and the
        // shapes are cut off at the cell boundaries.
        vec2 q = mod(p + 0.5 * cell, cell) - 0.5 * cell;
        d = shape(q);

    } else if (kind == 2) {
        // Limited repetition: round to the nearest cell index, then clamp the
        // index. Rounding rather than mod is what makes the clamp possible, and
        // clamping the index rather than the position is what keeps the cells
        // the same size at the edge of the grid.
        vec2 id = clamp(round(p / cell), -vec2(floor(limit)), vec2(floor(limit)));
        d = shape(p - cell * id);

    } else {
        // Polar repetition: fold the angle instead of the position. Everything
        // that radiates is this. The sector width is TAU / arms, and the same
        // trap applies: a shape wider than its sector is cut by the fold.
        float r = length(p);
        float a = atan(p.y, p.x);
        float sector = TAU / max(arms, 1.0);
        a = mod(a + 0.5 * sector, sector) - 0.5 * sector;
        vec2 q = vec2(cos(a), sin(a)) * r - vec2(cell, 0.0);
        d = shape(q);
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
