/*{
  "DESCRIPTION": "Eight three-dimensional distance primitives and the operators that combine them, rendered with a step-count view so the cost of each is visible alongside its shape.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The distance functions are Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "3D"],
  "INPUTS": [
    {
      "NAME": "shape",
      "TYPE": "long",
      "LABEL": "Primitive",
      "VALUES": [0, 1, 2, 3, 4, 5, 6, 7],
      "LABELS": ["sphere", "box", "rounded box", "torus", "capsule", "cylinder", "octahedron", "gyroid"],
      "DEFAULT": 3
    },
    {
      "NAME": "op",
      "TYPE": "long",
      "LABEL": "Combined with a sphere by",
      "VALUES": [0, 1, 2, 3, 4],
      "LABELS": ["nothing", "union", "smooth union", "subtraction", "intersection"],
      "DEFAULT": 2
    },
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "View",
      "VALUES": [0, 1],
      "LABELS": ["shaded", "steps taken"],
      "DEFAULT": 0
    },
    { "NAME": "orbit",    "TYPE": "point2D", "LABEL": "Camera",     "DEFAULT": [0.5, 0.55], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "size",     "TYPE": "float",   "LABEL": "Size",       "DEFAULT": 0.62, "MIN": 0.15, "MAX": 1.2 },
    { "NAME": "blend",    "TYPE": "float",   "LABEL": "Smoothing",  "DEFAULT": 0.22, "MIN": 0.01, "MAX": 0.6 },
    { "NAME": "cutter",   "TYPE": "float",   "LABEL": "Sphere size","DEFAULT": 0.55, "MIN": 0.05, "MAX": 1.2 },
    { "NAME": "offset",   "TYPE": "float",   "LABEL": "Sphere offset","DEFAULT": 0.55, "MIN": -1.5, "MAX": 1.5 },
    { "NAME": "repeat",   "TYPE": "float",   "LABEL": "Repeat",     "DEFAULT": 0.0,  "MIN": 0.0, "MAX": 4.0 },
    { "NAME": "stepScale","TYPE": "float",   "LABEL": "Step scale", "DEFAULT": 0.85, "MIN": 0.1, "MAX": 1.2 },
    { "NAME": "spin",     "TYPE": "float",   "LABEL": "Spin",       "DEFAULT": 0.08, "MIN": 0.0, "MAX": 0.6 }
  ]
}*/

const float TAU = 6.28318530718;
mat2 rot(float a) { return mat2(cos(a), -sin(a), sin(a), cos(a)); }

float sdSphere(vec3 p, float r) { return length(p) - r; }

float sdBox(vec3 p, vec3 b) {
    vec3 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);
}

// The torus is the clearest example of dimension reduction: measure the
// distance to the circle in xz, pair it with y, and you have a 2D problem.
float sdTorus(vec3 p, vec2 t) {
    vec2 q = vec2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}

float sdCapsule(vec3 p, vec3 a, vec3 b, float r) {
    vec3 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - r;
}

float sdCylinder(vec3 p, float h, float r) {
    vec2 d = abs(vec2(length(p.xz), p.y)) - vec2(r, h);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

float sdOctahedron(vec3 p, float s) {
    p = abs(p);
    return (p.x + p.y + p.z - s) * 0.57735027;
}

// A gyroid is not a distance field at all: it is an implicit surface, and the
// value it returns is not a distance to anything. It is included precisely
// because of that. Dividing by a bound on its gradient makes it *safe* to march
// rather than correct, and the step-count view shows what that costs.
float sdGyroid(vec3 p, float scale) {
    p *= scale;
    return (dot(sin(p), cos(p.zxy))) / scale * 0.55;
}

float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

float primitive(vec3 p) {
    if (shape == 0) return sdSphere(p, size);
    if (shape == 1) return sdBox(p, vec3(size * 0.8));
    if (shape == 2) return sdBox(p, vec3(size * 0.8) - 0.12) - 0.12;
    if (shape == 3) return sdTorus(p, vec2(size, size * 0.35));
    if (shape == 4) return sdCapsule(p, vec3(0.0, -size, 0.0), vec3(0.0, size, 0.0), size * 0.45);
    if (shape == 5) return sdCylinder(p, size * 0.8, size * 0.6);
    if (shape == 6) return sdOctahedron(p, size * 1.2);
    return sdGyroid(p, 5.0 / max(size, 0.2));
}

float map(vec3 p) {
    float t = TIME * spin * TAU;
    vec3 q = p;
    q.xz = rot(t) * q.xz;
    q.yz = rot(t * 0.6) * q.yz;

    // Domain repetition, unchanged from Unit 10 except for the extra component.
    if (repeat > 0.05) {
        float c = 3.4 / repeat;
        q = mod(q + 0.5 * c, c) - 0.5 * c;
    }

    float a = primitive(q);
    if (op == 0) return a;

    float b = sdSphere(q - vec3(offset, 0.0, 0.0), cutter);
    if (op == 1) return min(a, b);
    if (op == 2) return smin(a, b, blend);
    if (op == 3) return max(a, -b);
    return max(a, b);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y;

    // An orbit camera, built from a target and two angles. Every raymarched
    // scene in this course uses this, and it is worth having as a habit: a
    // camera that cannot be moved is a scene you cannot inspect.
    float yaw = (orbit.x - 0.5) * TAU;
    float pitch = (orbit.y - 0.5) * 2.4;
    vec3 ro = vec3(0.0, 0.0, 3.4);
    ro.yz = rot(pitch) * ro.yz;
    ro.xz = rot(yaw) * ro.xz;

    vec3 fw = normalize(-ro);
    vec3 rt = normalize(cross(vec3(0.0, 1.0, 0.0), fw));
    vec3 up = cross(fw, rt);
    vec3 rd = normalize(p.x * rt + p.y * up + 1.7 * fw);

    float travelled = 0.0;
    int used = 0;
    bool hit = false;
    for (int i = 0; i < 128; i++) {
        used = i;
        vec3 at = ro + rd * travelled;
        float d = map(at);
        if (d < 0.0012) { hit = true; break; }
        if (travelled > 14.0) break;
        travelled += d * stepScale;
    }

    vec3 colour;

    if (view == 1) {
        float t = float(used) / 128.0;
        colour = mix(vec3(0.05, 0.10, 0.25), vec3(1.0, 0.95, 0.55), sqrt(t));
        colour = mix(colour, vec3(1.0, 0.25, 0.25), step(0.97, t));
    } else if (hit) {
        vec3 at = ro + rd * travelled;
        vec2 e = vec2(0.0012, 0.0);
        vec3 n = normalize(vec3(
            map(at + e.xyy) - map(at - e.xyy),
            map(at + e.yxy) - map(at - e.yxy),
            map(at + e.yyx) - map(at - e.yyx)));
        float key = clamp(dot(n, normalize(vec3(0.7, 0.8, 0.5))), 0.0, 1.0);
        float fill = clamp(dot(n, normalize(vec3(-0.6, 0.2, 0.5))), 0.0, 1.0);
        colour = vec3(0.95, 0.62, 0.30) * key
               + vec3(0.20, 0.34, 0.60) * fill * 0.7
               + vec3(0.05, 0.06, 0.10);
        colour = mix(colour, vec3(0.04, 0.05, 0.08), 1.0 - exp(-0.02 * travelled * travelled));
    } else {
        colour = mix(vec3(0.05, 0.06, 0.10), vec3(0.09, 0.11, 0.17), uv.y);
    }

    gl_FragColor = vec4(colour, 1.0);
}
