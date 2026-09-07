/*{
  "DESCRIPTION": "Ambient occlusion, fog, per-object materials, and one bounce of reflection, each on a switch, with a step-count view so the price of each is visible.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The occlusion estimator, the distance functions and the smooth minimum are Inigo Quilez's; the cosine palette is his too.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "3D"],
  "INPUTS": [
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "Show",
      "VALUES": [0, 1, 2, 3],
      "LABELS": ["everything", "occlusion only", "material id", "steps taken"],
      "DEFAULT": 0
    },
    { "NAME": "orbit",     "TYPE": "point2D", "LABEL": "Camera",   "DEFAULT": [0.53, 0.58], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "sun",       "TYPE": "point2D", "LABEL": "Light",    "DEFAULT": [0.70, 0.62], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "occlusion", "TYPE": "float", "LABEL": "Occlusion",  "DEFAULT": 1.0,  "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "fog",       "TYPE": "float", "LABEL": "Fog",        "DEFAULT": 0.016, "MIN": 0.0, "MAX": 0.2 },
    { "NAME": "fogHeight", "TYPE": "float", "LABEL": "Fog settles","DEFAULT": 0.5,  "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "mirror",    "TYPE": "float", "LABEL": "Reflection", "DEFAULT": 0.35, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "roughness", "TYPE": "float", "LABEL": "Roughness",  "DEFAULT": 0.30, "MIN": 0.02, "MAX": 1.0 },
    { "NAME": "hue",       "TYPE": "float", "LABEL": "Hue",        "DEFAULT": 0.05, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spin",      "TYPE": "float", "LABEL": "Spin",       "DEFAULT": 0.05, "MIN": 0.0, "MAX": 0.4 }
  ]
}*/

const float TAU = 6.28318530718;
mat2 rot(float a) { return mat2(cos(a), -sin(a), sin(a), cos(a)); }

vec3 palette(float t) {
    return 0.5 + 0.44 * cos(TAU * (vec3(t) + vec3(0.0, 0.28, 0.58)));
}

float sdBox(vec3 p, vec3 b) {
    vec3 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);
}
float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

// The scene returns a distance *and* a material id. Carrying the id through the
// combination is what lets one march produce several materials, and the
// bookkeeping is the whole reason people use a vec2 rather than a float here.
vec2 opU(vec2 a, vec2 b) { return a.x < b.x ? a : b; }

vec2 mapM(vec3 p) {
    float t = TIME * spin * TAU;
    vec3 q = p;
    q.xz = rot(t) * q.xz;

    vec2 body = vec2(smin(sdBox(q, vec3(0.48)) - 0.10,
                          length(p - vec3(0.0, 0.80, 0.0)) - 0.34, 0.20), 1.0);
    vec2 ball = vec2(length(p - vec3(1.15 * cos(t * 0.8), -0.34, 1.15 * sin(t * 0.8))) - 0.36, 2.0);
    vec2 post = vec2(sdBox(p - vec3(-1.2, -0.1, -0.6), vec3(0.10, 0.75, 0.10)) - 0.03, 3.0);
    vec2 floorPlane = vec2(p.y + 0.72, 0.0);

    return opU(opU(body, ball), opU(post, floorPlane));
}

float map(vec3 p) { return mapM(p).x; }

vec3 normalAt(vec3 p) {
    vec2 e = vec2(0.0013, 0.0);
    return normalize(vec3(
        map(p + e.xyy) - map(p - e.xyy),
        map(p + e.yxy) - map(p - e.yxy),
        map(p + e.yyx) - map(p - e.yyx)));
}

// Ambient occlusion, the raymarcher's version: step a short way along the
// normal and ask the field how far the nearest surface is. If it is less than
// the distance stepped, something is nearby, and the shortfall is how occluded
// the point is. Five samples, no rays, and it is the single cheapest thing you
// can do to make a raymarched image look solid.
float ao(vec3 p, vec3 n) {
    float occ = 0.0, scale = 1.0;
    for (int i = 0; i < 5; i++) {
        float h = 0.02 + 0.14 * float(i);
        float d = map(p + n * h);
        occ += (h - d) * scale;
        scale *= 0.72;
    }
    return clamp(1.0 - occlusion * occ, 0.0, 1.0);
}

float shadow(vec3 ro, vec3 rd) {
    float res = 1.0, t = 0.06;
    for (int i = 0; i < 40; i++) {
        float d = map(ro + rd * t);
        if (d < 0.0015) return 0.0;
        res = min(res, 14.0 * d / t);
        t += clamp(d, 0.03, 0.45);
        if (t > 9.0) break;
    }
    return clamp(res, 0.0, 1.0);
}

vec3 materialOf(float id, vec3 p) {
    if (id < 0.5) {
        // A checker on the floor, filtered by the derivative so it fades to its
        // average with distance instead of shimmering. Unit 11's rule, in three
        // dimensions, where it matters far more.
        vec2 w = fwidth(p.xz) * 2.0;
        vec2 f = 1.0 - 2.0 * abs(fract(p.xz * 0.5) - 0.5);
        float check = mix(0.5, f.x * f.y, clamp(1.0 - max(w.x, w.y), 0.0, 1.0));
        return mix(vec3(0.07, 0.08, 0.11), vec3(0.42, 0.45, 0.50), check);
    }
    if (id < 1.5) return palette(hue);
    if (id < 2.5) return palette(hue + 0.32);
    return palette(hue + 0.62) * 0.7;
}

vec3 sky(vec3 rd) {
    return mix(vec3(0.06, 0.08, 0.13), vec3(0.16, 0.21, 0.32), clamp(rd.y * 0.5 + 0.5, 0.0, 1.0));
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y;

    float yaw = (orbit.x - 0.5) * TAU;
    float pitch = clamp((orbit.y - 0.5) * 2.0, -1.0, 1.0);
    vec3 ro = vec3(0.0, 0.0, 4.4);
    ro.yz = rot(pitch) * ro.yz;
    ro.xz = rot(yaw) * ro.xz;
    ro.y += 0.5;

    vec3 fw = normalize(vec3(0.0, 0.05, 0.0) - ro);
    vec3 rt = normalize(cross(vec3(0.0, 1.0, 0.0), fw));
    vec3 upv = cross(fw, rt);
    vec3 rd = normalize(p.x * rt + p.y * upv + 1.8 * fw);

    float yawL = (sun.x - 0.5) * TAU;
    float elev = sun.y * 1.3;
    vec3 L = normalize(vec3(sin(yawL) * cos(elev), sin(elev) + 0.2, cos(yawL) * cos(elev)));

    float travelled = 0.0;
    int used = 0;
    vec2 hitInfo = vec2(-1.0);
    for (int i = 0; i < 140; i++) {
        used = i;
        vec3 at = ro + rd * travelled;
        vec2 m = mapM(at);
        if (m.x < 0.0012) { hitInfo = vec2(travelled, m.y); break; }
        if (travelled > 26.0) break;
        travelled += m.x * 0.9;
    }

    if (view == 3) {
        float t = float(used) / 140.0;
        vec3 c = mix(vec3(0.05, 0.10, 0.25), vec3(1.0, 0.95, 0.55), sqrt(t));
        gl_FragColor = vec4(mix(c, vec3(1.0, 0.25, 0.25), step(0.96, t)), 1.0);
        return;
    }

    vec3 colour;

    if (hitInfo.x < 0.0) {
        colour = sky(rd);
    } else {
        vec3 at = ro + rd * hitInfo.x;
        vec3 n = normalAt(at);
        float occ = ao(at, n);
        float sh = shadow(at + n * 0.012, L);
        vec3 base = materialOf(hitInfo.y, at);

        if (view == 1) {
            colour = vec3(occ);
        } else if (view == 2) {
            colour = palette(hitInfo.y * 0.27);
        } else {
            float diff = clamp(dot(n, L), 0.0, 1.0);
            vec3 h = normalize(L - rd);
            float spec = pow(clamp(dot(n, h), 0.0, 1.0), mix(128.0, 6.0, roughness));

            // Ambient light arrives from everywhere, so occlusion belongs on it
            // and not on the key light, which already has a shadow. Multiplying
            // the whole result by AO is the usual mistake and it makes crevices
            // black rather than merely darker.
            vec3 ambientLight = sky(n) * 1.2 * occ;

            colour = base * (ambientLight + vec3(1.0, 0.92, 0.80) * diff * sh)
                   + vec3(1.0) * spec * sh * (1.0 - roughness) * 0.6;

            // One bounce of reflection, marched again. This is where a
            // raymarcher's cost doubles, and the step view shows it: the
            // reflection ray has no shortcut and pays the full price.
            if (mirror > 0.01) {
                vec3 r = reflect(rd, n);
                float rt2 = 0.05;
                vec3 rc = sky(r);
                for (int i = 0; i < 48; i++) {
                    vec3 rp = at + r * rt2;
                    vec2 m = mapM(rp);
                    if (m.x < 0.002) {
                        vec3 rn = normalAt(rp);
                        float rd2 = clamp(dot(rn, L), 0.0, 1.0);
                        rc = materialOf(m.y, rp) * (0.15 + 0.85 * rd2);
                        break;
                    }
                    rt2 += m.x;
                    if (rt2 > 12.0) break;
                }
                float fresnel = pow(1.0 - clamp(dot(-rd, n), 0.0, 1.0), 5.0);
                colour = mix(colour, rc, mirror * mix(0.06, 1.0, fresnel) * (1.0 - roughness));
            }
        }

        // Height fog: denser low down, so the scene sits in something rather
        // than fading uniformly. Applied last, on the finished colour, because
        // fog is light scattered on the way to the eye and not a property of
        // the surface.
        if (view == 0) {
            float density = fog * mix(1.0, exp(-max(at.y + 0.7, 0.0) * 1.6), fogHeight);
            float amount = 1.0 - exp(-density * hitInfo.x * hitInfo.x);
            colour = mix(colour, sky(rd) * 1.15, clamp(amount, 0.0, 1.0));
        }
    }

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
