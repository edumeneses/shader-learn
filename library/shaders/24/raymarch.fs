/*{
  "DESCRIPTION": "The sphere-traced loop, with a two-dimensional cross-section that draws every step's safety circle so the algorithm can be watched rather than imagined.",
  "CREDIT": "Eduardo Meneses, Learn shader art. Sphere tracing is John Hart's; the raymarching idiom and the distance functions are Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "3D"],
  "INPUTS": [
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "View",
      "VALUES": [0, 1, 2],
      "LABELS": ["the render", "steps taken", "cross-section: watch it march"],
      "DEFAULT": 2
    },
    { "NAME": "aim",      "TYPE": "point2D", "LABEL": "Ray direction", "DEFAULT": [0.72, 0.56], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "maxSteps", "TYPE": "float",   "LABEL": "Max steps",  "DEFAULT": 64.0, "MIN": 4.0,  "MAX": 128.0 },
    { "NAME": "epsilon",  "TYPE": "float",   "LABEL": "Hit epsilon","DEFAULT": 0.002, "MIN": 0.0002, "MAX": 0.08 },
    { "NAME": "stepScale","TYPE": "float",   "LABEL": "Step scale", "DEFAULT": 1.0,  "MIN": 0.15, "MAX": 1.6 },
    { "NAME": "far",      "TYPE": "float",   "LABEL": "Far",        "DEFAULT": 12.0, "MIN": 2.0,  "MAX": 40.0 },
    { "NAME": "spin",     "TYPE": "float",   "LABEL": "Spin",       "DEFAULT": 0.10, "MIN": 0.0,  "MAX": 1.0 },
    { "NAME": "showCircles","TYPE": "bool",  "LABEL": "Draw the safety circles", "DEFAULT": true }
  ]
}*/

const float TAU = 6.28318530718;

mat2 rot(float a) { return mat2(cos(a), -sin(a), sin(a), cos(a)); }

// --- the 3D scene -----------------------------------------------------------

float sdSphere(vec3 p, float r) { return length(p) - r; }

float sdBox(vec3 p, vec3 b) {
    vec3 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);
}

float map(vec3 p) {
    float t = TIME * spin;
    vec3 q = p;
    q.xz = rot(t) * q.xz;
    float box = sdBox(q - vec3(0.0, 0.0, 0.0), vec3(0.55)) - 0.08;
    float ball = sdSphere(p - vec3(0.9 * cos(t * 1.3), 0.35, 0.9 * sin(t * 1.3)), 0.32);
    float ground = p.y + 0.75;
    return min(min(box, ball), ground);
}

// --- the 2D scene, for the cross-section ------------------------------------
//
// The same algorithm in two dimensions, because in two dimensions it can be
// drawn. Everything about the loop is identical; only the number of components
// changes, which is the point.

float map2(vec2 p) {
    float t = TIME * spin;
    vec2 q = rot(t) * (p - vec2(0.35, 0.05));
    vec2 d = abs(q) - vec2(0.30, 0.20);
    float box = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - 0.06;
    float ball = length(p - vec2(-0.45, 0.30 + 0.18 * sin(t * 2.0))) - 0.22;
    float bar = abs(p.y + 0.52) - 0.03;
    return min(min(box, ball), bar);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y;

    int limit = int(floor(maxSteps + 0.5));

    if (view == 2) {
        // The cross-section. A ray leaves the origin, and at each step the
        // field is asked how far the nearest surface is. That distance is a
        // radius the ray may advance through without hitting anything, which is
        // why the circles never overlap a surface and why the method is called
        // sphere tracing.
        vec2 ro = vec2(-1.05, -0.15);
        vec2 rd = normalize((aim - vec2(0.5)) * vec2(2.2, 1.4) - (ro - vec2(0.0)));

        vec3 colour = vec3(0.05, 0.06, 0.09);

        // The scene, as a field, so the reader can see what the ray is asking.
        float d0 = map2(p);
        colour = mix(colour, vec3(0.14, 0.17, 0.23), smoothstep(0.0, 0.004, -d0));
        colour = mix(colour, vec3(0.35, 0.42, 0.55),
                     smoothstep(fwidth(d0) * 1.5, 0.0, abs(d0)));

        // March, drawing as we go.
        float travelled = 0.0;
        int used = 0;
        for (int i = 0; i < 128; i++) {
            if (i >= limit) break;
            vec2 at = ro + rd * travelled;
            float d = map2(at);
            used = i;

            if (showCircles) {
                float ring = abs(length(p - at) - abs(d) * stepScale);
                float fade = 1.0 - float(i) / float(limit);
                colour = mix(colour, vec3(0.30, 0.85, 0.80),
                             smoothstep(fwidth(ring) * 1.6, 0.0, ring) * (0.20 + 0.55 * fade));
            }
            // The sample point itself.
            colour = mix(colour, vec3(1.0, 0.72, 0.25),
                         smoothstep(0.012, 0.006, length(p - at)));

            if (d < epsilon || travelled > far) break;
            travelled += d * stepScale;
        }

        // The ray, drawn under everything as a thin line to its stopping point.
        vec2 pa = p - ro, ba = rd * travelled;
        float h = clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0);
        float line = length(pa - ba * h);
        colour = mix(colour, vec3(0.95, 0.55, 0.20),
                     smoothstep(0.004, 0.002, line) * 0.7);

        gl_FragColor = vec4(colour, 1.0);
        return;
    }

    // --- the ordinary raymarcher ------------------------------------------

    vec3 ro = vec3(0.0, 0.55, 3.0);
    vec3 rd = normalize(vec3(p, -1.6));

    float travelled = 0.0;
    int used = 0;
    bool hit = false;

    // The loop bound is a compile-time constant and the real limit is a
    // `break`. Some WebGL drivers reject a loop whose bound is a uniform, and a
    // raymarcher written the natural way fails in a browser and nowhere else.
    for (int i = 0; i < 128; i++) {
        if (i >= limit) break;
        used = i;
        vec3 at = ro + rd * travelled;
        float d = map(at);
        if (d < epsilon) { hit = true; break; }
        if (travelled > far) break;
        // Advance by the distance to the nearest surface, which is guaranteed
        // safe. Scaling it below 1 is the fix for a field that over-estimates;
        // scaling above 1 is faster and will punch through thin geometry.
        travelled += d * stepScale;
    }

    vec3 colour;

    if (view == 1) {
        // Step count as a heat map. This is the single most useful debugging
        // view in three-dimensional shader work: it shows exactly where the
        // marcher is spending its time, and it is almost always at grazing
        // angles and near silhouettes.
        float t = float(used) / float(limit);
        colour = mix(vec3(0.05, 0.10, 0.25), vec3(1.0, 0.95, 0.55), sqrt(t));
        colour = mix(colour, vec3(1.0, 0.25, 0.25), step(0.985, t));

    } else {
        if (hit) {
            // Normals by central differences, which Unit 26 explains properly.
            vec3 at = ro + rd * travelled;
            vec2 e = vec2(0.0015, 0.0);
            vec3 n = normalize(vec3(
                map(at + e.xyy) - map(at - e.xyy),
                map(at + e.yxy) - map(at - e.yxy),
                map(at + e.yyx) - map(at - e.yyx)));
            float lam = clamp(dot(n, normalize(vec3(0.6, 0.8, 0.4))), 0.0, 1.0);
            colour = vec3(0.30, 0.55, 0.85) * (0.15 + 0.85 * lam);
            colour = mix(colour, vec3(0.04, 0.05, 0.08),
                         1.0 - exp(-0.06 * travelled * travelled));
        } else {
            colour = mix(vec3(0.05, 0.07, 0.12), vec3(0.10, 0.13, 0.20), uv.y);
        }
    }

    gl_FragColor = vec4(colour, 1.0);
}
