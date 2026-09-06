/*{
  "DESCRIPTION": "Voronoi from the 3x3 neighbourhood, with F1, the cell id, the F2 minus F1 edge distance, and the true edge distance as separate views.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The true-edge second pass is Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Noise"],
  "INPUTS": [
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "View",
      "VALUES": [0, 1, 2, 3, 4],
      "LABELS": ["F1  distance to nearest", "cell id", "F2 - F1  approximate edges", "true edge distance", "id with F1 shading"],
      "DEFAULT": 4
    },
    { "NAME": "density",  "TYPE": "float", "LABEL": "Density",     "DEFAULT": 6.0,  "MIN": 1.0, "MAX": 30.0 },
    { "NAME": "jitter",   "TYPE": "float", "LABEL": "Jitter",      "DEFAULT": 1.0,  "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "drift",    "TYPE": "float", "LABEL": "Drift",       "DEFAULT": 0.25, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "lineWidth","TYPE": "float", "LABEL": "Edge width",  "DEFAULT": 0.05, "MIN": 0.005, "MAX": 0.30 },
    { "NAME": "hue",      "TYPE": "float", "LABEL": "Hue",         "DEFAULT": 0.55, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spread",   "TYPE": "float", "LABEL": "Hue spread",  "DEFAULT": 0.30, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "showSites","TYPE": "bool",  "LABEL": "Show the sites", "DEFAULT": true }
  ]
}*/

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 a = vec3(0.5), b = vec3(0.46), c = vec3(1.0);
    vec3 d = vec3(hue) + spread * vec3(0.0, 0.33, 0.67);
    return a + b * cos(TAU * (c * t + d));
}

float hash21(vec2 p) {
    vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    q += dot(q, q.yzx + 33.33);
    return fract((q.x + q.y) * q.z);
}

// One site per cell. The site moves inside its cell and never leaves it, which
// is the invariant the whole algorithm depends on: if a site could wander into
// a neighbouring cell, the 3x3 search below would miss it.
vec2 site(vec2 cell) {
    float a = hash21(cell) * TAU;
    float b = hash21(cell + 41.7);
    vec2 wobble = vec2(cos(a + TIME * drift), sin(a + TIME * drift * 1.3));
    return cell + 0.5 + 0.5 * jitter * wobble * (0.6 + 0.4 * b);
}

// F1 and F2, plus the id of the nearest site and the offset to it.
//
// Nine cells, not one. Unit 10's fold assumed the nearest copy was the one in
// your own cell, and here that assumption is simply false: a site near a cell
// boundary is closer to points in the neighbouring cell than that cell's own
// site is. This is the case Unit 10 said it would have to check neighbours for,
// and Voronoi is where it becomes unavoidable.
void voronoi(vec2 p, out float f1, out float f2, out vec2 id, out vec2 toNearest) {
    vec2 cell = floor(p);
    f1 = 1e9; f2 = 1e9; id = cell; toNearest = vec2(0.0);
    for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
            vec2 c = cell + vec2(float(i), float(j));
            vec2 r = site(c) - p;
            float d = dot(r, r);
            if (d < f1) {
                f2 = f1;
                f1 = d;
                id = c;
                toNearest = r;
            } else if (d < f2) {
                f2 = d;
            }
        }
    }
    f1 = sqrt(f1);
    f2 = sqrt(f2);
}

// The distance to the nearest *edge*, rather than the difference between the
// two nearest sites. F2 - F1 is an approximation that goes to zero at corners
// where three cells meet, so a border drawn from it thickens at every corner.
// The correct value needs a second pass: for each neighbouring site, measure
// the distance from p to the perpendicular bisector between it and the winner.
float edgeDistance(vec2 p, vec2 id, vec2 toNearest) {
    float d = 1e9;
    for (int j = -2; j <= 2; j++) {
        for (int i = -2; i <= 2; i++) {
            vec2 c = id + vec2(float(i), float(j));
            vec2 r = site(c) - p;
            vec2 diff = r - toNearest;
            float len = length(diff);
            if (len > 1e-5) {
                d = min(d, dot(0.5 * (toNearest + r), diff / len));
            }
        }
    }
    return d;
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y * density;

    float f1, f2;
    vec2 id, toNearest;
    voronoi(p, f1, f2, id, toNearest);

    vec3 colour;

    if (view == 0) {
        colour = palette(0.15 + f1 * 0.9) * clamp(1.0 - f1 * 0.8, 0.05, 1.0);

    } else if (view == 1) {
        colour = palette(hash21(id));

    } else if (view == 2) {
        float border = f2 - f1;
        float aa = fwidth(border);
        colour = mix(palette(hash21(id)) * 0.35, vec3(0.95),
                     smoothstep(lineWidth + aa, lineWidth - aa, border));

    } else if (view == 3) {
        float e = edgeDistance(p, id, toNearest);
        float aa = fwidth(e);
        colour = mix(palette(hash21(id)) * 0.35, vec3(0.95),
                     smoothstep(lineWidth * 0.5 + aa, lineWidth * 0.5 - aa, e));

    } else {
        // The combination that is actually useful: each cell its own colour,
        // shaded by how far the point is from that cell's site, with a true
        // edge. Cracked earth, scales, cobbles, cells.
        float e = edgeDistance(p, id, toNearest);
        float aa = fwidth(e);
        vec3 body = palette(hash21(id)) * (0.45 + 0.75 * (1.0 - f1));
        colour = mix(body, body * 0.15, smoothstep(lineWidth * 0.5 + aa, lineWidth * 0.5 - aa, e));
    }

    if (showSites) {
        float r = length(toNearest);
        float aa = fwidth(r);
        colour = mix(colour, vec3(1.0, 0.35, 0.45),
                     smoothstep(0.035 + aa, 0.035 - aa, r));
    }

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
