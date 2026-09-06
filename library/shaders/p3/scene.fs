/*{
  "DESCRIPTION": "The Module G milestone: a raymarched scene assembled from Units 24 to 27, with a step budget, a cost view, and every quality control exposed so it can be tuned down to the machine it has to run on.",
  "CREDIT": "Eduardo Meneses, Learn shader art. Distance functions and estimators after Inigo Quilez.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "3D"],
  "INPUTS": [
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "View",
      "VALUES": [0, 1, 2],
      "LABELS": ["the scene", "steps taken", "cost against the budget"],
      "DEFAULT": 0
    },
    { "NAME": "orbit",     "TYPE": "point2D", "LABEL": "Camera",   "DEFAULT": [0.55, 0.56], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "sun",       "TYPE": "point2D", "LABEL": "Light",    "DEFAULT": [0.74, 0.55], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "budget",    "TYPE": "float", "LABEL": "Step budget",  "DEFAULT": 96.0, "MIN": 16.0, "MAX": 160.0 },
    { "NAME": "shadowSteps","TYPE": "float","LABEL": "Shadow steps", "DEFAULT": 32.0, "MIN": 4.0,  "MAX": 64.0 },
    { "NAME": "aoSamples", "TYPE": "float", "LABEL": "Occlusion samples", "DEFAULT": 5.0, "MIN": 0.0, "MAX": 8.0 },
    { "NAME": "stepScale", "TYPE": "float", "LABEL": "Step scale",   "DEFAULT": 0.85, "MIN": 0.3, "MAX": 1.2 },
    { "NAME": "detail",    "TYPE": "float", "LABEL": "Surface detail","DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "fog",       "TYPE": "float", "LABEL": "Fog",          "DEFAULT": 0.006, "MIN": 0.0, "MAX": 0.12 },
    { "NAME": "hue",       "TYPE": "float", "LABEL": "Hue",          "DEFAULT": 0.62, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spread",    "TYPE": "float", "LABEL": "Hue spread",   "DEFAULT": 0.30, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "drift",     "TYPE": "float", "LABEL": "Drift",        "DEFAULT": 0.06, "MIN": 0.0, "MAX": 0.4 }
  ]
}*/

const float TAU = 6.28318530718;
mat2 rot(float a) { return mat2(cos(a), -sin(a), sin(a), cos(a)); }

vec3 palette(float t) {
    vec3 d = vec3(hue) + spread * vec3(0.0, 0.30, 0.60);
    return 0.5 + 0.44 * cos(TAU * (vec3(t) + d));
}

float hash21(vec2 p) {
    vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    q += dot(q, q.yzx + 33.33);
    return fract((q.x + q.y) * q.z);
}

float sdBox(vec3 p, vec3 b) {
    vec3 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);
}
float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

vec2 opU(vec2 a, vec2 b) { return a.x < b.x ? a : b; }

// A ring of towers on a plain, in polar repetition from Unit 10, with each
// tower's height taken from a hash of its sector index so the ring is regular
// without being uniform.
vec2 mapM(vec3 p) {
    float t = TIME * drift;

    vec2 ground = vec2(p.y + 1.0, 0.0);

    // Polar repetition. The sector index is the value that gives each tower
    // its own identity, which is Unit 12 and Unit 10 doing the work together.
    //
    // Three sectors are evaluated, not one. Unit 10 said a fold assumes the
    // nearest copy is the one in your own cell, and that a shape near a
    // boundary breaks the assumption; here it breaks it in the way that matters
    // most, because the field then *over-estimates* and a raymarcher that
    // over-estimates steps through surfaces. The symptom was a wedge bitten out
    // of whichever tower sat nearest a sector boundary, and it looked like a
    // modelling bug rather than a field one. Three evaluations, one line each.
    vec3 q = p;
    q.xz = rot(t * 0.3) * q.xz;
    float sectors = 9.0;
    float sector = TAU / sectors;
    float ang = atan(q.z, q.x);
    float r = length(q.xz);
    float base = floor((ang + 0.5 * sector) / sector);

    float tower = 1e9;
    for (int k = -1; k <= 1; k++) {
        float id = base + float(k);
        float a = ang - id * sector;
        // A real position rebuilt from the folded angle, not the arc length
        // `a * r`: the arc is longer than the straight line, so an arc-length
        // fold over-estimates too, and for the same reason.
        vec2 folded = vec2(cos(a), sin(a)) * r;
        vec3 tp = vec3(folded.x - 2.6, q.y, folded.y);
        float h = 0.5 + 1.4 * hash21(vec2(mod(id, sectors), 3.0));
        tower = min(tower,
            sdBox(tp - vec3(0.0, h * 0.5 - 1.0, 0.0), vec3(0.30, h * 0.5, 0.30)) - 0.06);
    }
    vec2 towers = vec2(tower, 1.0);

    // The centrepiece: a smooth union that is expensive precisely where the
    // fillet is, which the step view will show.
    vec3 c = p - vec3(0.0, 0.15 + 0.12 * sin(t * 2.2), 0.0);
    c.xz = rot(t) * c.xz;
    c.xy = rot(t * 0.7) * c.xy;
    float core = smin(sdBox(c, vec3(0.42)) - 0.10,
                      length(p - vec3(0.0, 0.95, 0.0)) - 0.30, 0.30);
    vec2 centre = vec2(core, 2.0);

    vec2 scene = opU(opU(ground, towers), centre);

    // Surface detail, added to the distance. Any displacement breaks the metric
    // and the field over-estimates, so the marcher needs a smaller step; that
    // is what Step scale is for and why raising Surface detail costs steps.
    if (detail > 0.01) {
        float bump = sin(p.x * 9.0) * sin(p.y * 9.0) * sin(p.z * 9.0);
        scene.x += bump * 0.018 * detail;
    }

    return scene;
}

float map(vec3 p) { return mapM(p).x; }

vec3 normalAt(vec3 p) {
    vec2 e = vec2(0.0015, 0.0);
    return normalize(vec3(
        map(p + e.xyy) - map(p - e.xyy),
        map(p + e.yxy) - map(p - e.yxy),
        map(p + e.yyx) - map(p - e.yyx)));
}

float shadow(vec3 ro, vec3 rd) {
    int n = int(floor(shadowSteps + 0.5));
    float res = 1.0, t = 0.06;
    for (int i = 0; i < 64; i++) {
        if (i >= n) break;
        float d = map(ro + rd * t);
        if (d < 0.002) return 0.0;
        res = min(res, 12.0 * d / t);
        t += clamp(d, 0.04, 0.7);
        if (t > 12.0) break;
    }
    return clamp(res, 0.0, 1.0);
}

float ao(vec3 p, vec3 n) {
    int n5 = int(floor(aoSamples + 0.5));
    if (n5 <= 0) return 1.0;
    float occ = 0.0, scale = 1.0;
    for (int i = 0; i < 8; i++) {
        if (i >= n5) break;
        float h = 0.02 + 0.16 * float(i);
        occ += (h - map(p + n * h)) * scale;
        scale *= 0.70;
    }
    return clamp(1.0 - occ, 0.0, 1.0);
}

vec3 sky(vec3 rd) {
    vec3 low = palette(0.02) * 0.10;
    vec3 high = palette(0.58) * 0.34;
    return mix(low, high, clamp(rd.y * 0.7 + 0.35, 0.0, 1.0));
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y;

    float yaw = (orbit.x - 0.5) * TAU;
    float pitch = clamp((orbit.y - 0.5) * 1.6, -0.55, 0.9);
    vec3 ro = vec3(0.0, 0.0, 8.2);
    ro.yz = rot(pitch) * ro.yz;
    ro.xz = rot(yaw) * ro.xz;
    ro.y += 0.9;

    vec3 fw = normalize(vec3(0.0, 0.1, 0.0) - ro);
    vec3 rt = normalize(cross(vec3(0.0, 1.0, 0.0), fw));
    vec3 upv = cross(fw, rt);
    vec3 rd = normalize(p.x * rt + p.y * upv + 1.75 * fw);

    float yawL = (sun.x - 0.5) * TAU;
    float elev = sun.y * 1.2;
    vec3 L = normalize(vec3(sin(yawL) * cos(elev), sin(elev) + 0.22, cos(yawL) * cos(elev)));

    int limit = int(floor(budget + 0.5));
    float travelled = 0.0;
    int used = 0;
    vec2 hitInfo = vec2(-1.0);
    for (int i = 0; i < 160; i++) {
        if (i >= limit) break;
        used = i;
        vec2 m = mapM(ro + rd * travelled);
        if (m.x < 0.0015) { hitInfo = vec2(travelled, m.y); break; }
        if (travelled > 30.0) break;
        travelled += m.x * stepScale;
    }

    if (view >= 1) {
        float t = float(used) / float(limit);
        vec3 c;
        if (view == 1) {
            c = mix(vec3(0.05, 0.10, 0.25), vec3(1.0, 0.95, 0.55), sqrt(t));
        } else {
            // Cost against the budget: green where the march finished early,
            // red where it ran out. Red pixels are not slow, they are *wrong*:
            // the ray gave up before reaching a surface, which is what produces
            // the soft haloes around silhouettes in an under-budgeted scene.
            c = mix(vec3(0.10, 0.55, 0.35), vec3(1.0, 0.85, 0.25), t);
            if (hitInfo.x < 0.0 && travelled < 30.0) c = vec3(1.0, 0.20, 0.25);
        }
        gl_FragColor = vec4(c, 1.0);
        return;
    }

    vec3 colour;

    if (hitInfo.x < 0.0) {
        colour = sky(rd);
    } else {
        vec3 at = ro + rd * hitInfo.x;
        vec3 n = normalAt(at);
        float occ = ao(at, n);
        float sh = shadow(at + n * 0.015, L);

        vec3 base;
        if (hitInfo.y < 0.5) {
            vec2 w = fwidth(at.xz) * 2.0;
            vec2 f = 1.0 - 2.0 * abs(fract(at.xz * 0.4) - 0.5);
            float check = mix(0.5, f.x * f.y, clamp(1.0 - max(w.x, w.y), 0.0, 1.0));
            base = mix(palette(0.15) * 0.20, palette(0.15) * 0.55, check);
        } else if (hitInfo.y < 1.5) {
            base = palette(0.42);
        } else {
            base = palette(0.80);
        }

        float diff = clamp(dot(n, L), 0.0, 1.0);
        vec3 h = normalize(L - rd);
        float spec = pow(clamp(dot(n, h), 0.0, 1.0), 36.0);

        colour = base * (sky(n) * 1.6 * occ + vec3(1.0, 0.92, 0.82) * diff * sh)
               + vec3(1.0) * spec * sh * 0.35;

        float density = fog * exp(-max(at.y + 1.0, 0.0) * 0.9);
        colour = mix(colour, sky(rd) * 1.1,
                     clamp(1.0 - exp(-density * hitInfo.x * hitInfo.x), 0.0, 1.0));
    }

    // Tone curve and a light grain, from Units 22 and 04. A raymarched scene
    // without a tone curve clips its highlights exactly where the specular is.
    colour = colour / (1.0 + colour);
    colour = pow(max(colour, 0.0), vec3(1.0 / 1.25));
    colour += (hash21(uv * RENDERSIZE) - 0.5) * 0.02;

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
