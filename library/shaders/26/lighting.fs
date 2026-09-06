/*{
  "DESCRIPTION": "Normals from the field's own gradient, then diffuse, specular, and shadows built one at a time so each term's contribution is separable.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The soft-shadow estimator is Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "3D"],
  "INPUTS": [
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "Show",
      "VALUES": [0, 1, 2, 3, 4, 5],
      "LABELS": ["everything", "normals as colour", "diffuse only", "specular only", "shadow only", "flat, no lighting"],
      "DEFAULT": 0
    },
    { "NAME": "sun",       "TYPE": "point2D", "LABEL": "Light direction", "DEFAULT": [0.68, 0.72], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "orbit",     "TYPE": "point2D", "LABEL": "Camera",     "DEFAULT": [0.52, 0.60], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "normalEps", "TYPE": "float",   "LABEL": "Normal epsilon", "DEFAULT": 0.0015, "MIN": 0.0002, "MAX": 0.08 },
    { "NAME": "shininess", "TYPE": "float",   "LABEL": "Shininess",  "DEFAULT": 48.0, "MIN": 2.0, "MAX": 256.0 },
    { "NAME": "specular",  "TYPE": "float",   "LABEL": "Specular",   "DEFAULT": 0.5,  "MIN": 0.0, "MAX": 1.5 },
    {
      "NAME": "shadowMode",
      "TYPE": "long",
      "LABEL": "Shadows",
      "VALUES": [0, 1, 2],
      "LABELS": ["none", "hard", "soft"],
      "DEFAULT": 2
    },
    { "NAME": "softness",  "TYPE": "float", "LABEL": "Shadow softness", "DEFAULT": 12.0, "MIN": 1.0, "MAX": 64.0 },
    { "NAME": "ambient",   "TYPE": "float", "LABEL": "Ambient",    "DEFAULT": 0.12, "MIN": 0.0, "MAX": 0.6 },
    { "NAME": "spin",      "TYPE": "float", "LABEL": "Spin",       "DEFAULT": 0.07, "MIN": 0.0, "MAX": 0.5 }
  ]
}*/

const float TAU = 6.28318530718;
mat2 rot(float a) { return mat2(cos(a), -sin(a), sin(a), cos(a)); }

float sdBox(vec3 p, vec3 b) {
    vec3 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);
}
float sdTorus(vec3 p, vec2 t) {
    return length(vec2(length(p.xz) - t.x, p.y)) - t.y;
}
float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

float map(vec3 p) {
    float t = TIME * spin * TAU;
    vec3 q = p;
    q.xz = rot(t) * q.xz;
    float body = smin(sdBox(q, vec3(0.5, 0.5, 0.5)) - 0.1,
                      length(p - vec3(0.0, 0.85, 0.0)) - 0.38, 0.22);
    float ring = sdTorus(vec3(q.x, q.y - 0.1, q.z), vec2(1.05, 0.10));
    float ground = p.y + 0.85;
    return min(min(body, ring), ground);
}

// The normal is the gradient of the field, and the gradient is what direction
// the distance increases fastest in, which is exactly away from the surface.
// Six extra evaluations of the whole scene, which is why the normal is usually
// the second most expensive thing in a raymarcher after the march itself.
vec3 normalAt(vec3 p) {
    vec2 e = vec2(normalEps, 0.0);
    return normalize(vec3(
        map(p + e.xyy) - map(p - e.xyy),
        map(p + e.yxy) - map(p - e.yxy),
        map(p + e.yyx) - map(p - e.yyx)));
}

// A shadow is a second march, from the surface towards the light. If anything
// is hit before reaching it, the point is in shadow.
//
// The soft version is the clever part and it is free: while marching, the ratio
// of the nearest distance to how far we have travelled is a measure of how
// close the ray passed to an occluder, which is an estimate of the penumbra.
// Nothing extra is sampled; the information was already there.
float shadow(vec3 ro, vec3 rd) {
    if (shadowMode == 0) return 1.0;
    float res = 1.0;
    float t = 0.05;
    for (int i = 0; i < 48; i++) {
        vec3 at = ro + rd * t;
        float d = map(at);
        if (d < 0.0015) return 0.0;
        if (shadowMode == 2) res = min(res, softness * d / t);
        t += clamp(d, 0.02, 0.4);
        if (t > 8.0) break;
    }
    return clamp(res, 0.0, 1.0);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y;

    float yaw = (orbit.x - 0.5) * TAU;
    float pitch = clamp((orbit.y - 0.5) * 2.2, -1.2, 1.2);
    vec3 ro = vec3(0.0, 0.0, 4.0);
    ro.yz = rot(pitch) * ro.yz;
    ro.xz = rot(yaw) * ro.xz;
    ro.y += 0.4;

    vec3 fw = normalize(vec3(0.0, 0.1, 0.0) - ro);
    vec3 rt = normalize(cross(vec3(0.0, 1.0, 0.0), fw));
    vec3 up = cross(fw, rt);
    vec3 rd = normalize(p.x * rt + p.y * up + 1.8 * fw);

    float yawL = (sun.x - 0.5) * TAU;
    float elev = sun.y * 1.4;
    vec3 L = normalize(vec3(sin(yawL) * cos(elev), sin(elev) + 0.15, cos(yawL) * cos(elev)));

    float travelled = 0.0;
    bool hit = false;
    for (int i = 0; i < 128; i++) {
        vec3 at = ro + rd * travelled;
        float d = map(at);
        if (d < 0.0012) { hit = true; break; }
        if (travelled > 20.0) break;
        travelled += d * 0.9;
    }

    vec3 colour;

    if (!hit) {
        colour = mix(vec3(0.05, 0.06, 0.10), vec3(0.10, 0.13, 0.20), uv.y);
    } else {
        vec3 at = ro + rd * travelled;
        vec3 n = normalAt(at);

        // Diffuse: Lambert. How much a surface faces the light, and nothing
        // else. It is the term that gives an object its form.
        float diff = clamp(dot(n, L), 0.0, 1.0);

        // Specular: Blinn-Phong, using the halfway vector rather than a
        // reflection. Cheaper, better behaved at grazing angles, and the term
        // that tells the eye what a surface is made of.
        vec3 h = normalize(L - rd);
        float spec = pow(clamp(dot(n, h), 0.0, 1.0), shininess);

        float sh = shadow(at + n * 0.01, L);

        vec3 base = vec3(0.72, 0.55, 0.42);

        if (view == 1) {
            // Normals as colour. The single most useful debugging view in
            // three dimensions: a normal that is noisy, banded, or facetted is
            // visible here and nowhere else.
            colour = n * 0.5 + 0.5;
        } else if (view == 2) {
            colour = vec3(diff);
        } else if (view == 3) {
            colour = vec3(spec * specular);
        } else if (view == 4) {
            colour = vec3(sh);
        } else if (view == 5) {
            colour = base;
        } else {
            colour = base * (ambient + diff * sh)
                   + vec3(1.0, 0.95, 0.85) * spec * specular * sh;
            colour = mix(colour, vec3(0.05, 0.06, 0.10),
                         1.0 - exp(-0.008 * travelled * travelled));
        }
    }

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
