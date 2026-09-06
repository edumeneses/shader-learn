/*{
  "DESCRIPTION": "A procedural test card: colour bars, a greyscale ramp, frequency wedges, and fine detail. Rendered once and committed as a PNG, so every shader in Module F has something to read.",
  "CREDIT": "Eduardo Meneses, Learn shader art",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Utility"],
  "INPUTS": [
    { "NAME": "labels", "TYPE": "bool",  "LABEL": "Registration marks", "DEFAULT": true },
    { "NAME": "seed",   "TYPE": "float", "LABEL": "Detail seed", "DEFAULT": 3.0, "MIN": 0.0, "MAX": 20.0 }
  ]
}*/

const float TAU = 6.28318530718;

float hash21(vec2 p) {
    vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    q += dot(q, q.yzx + 33.33);
    return fract((q.x + q.y) * q.z);
}

vec2 hash22(vec2 p) {
    float a = hash21(p) * TAU;
    return vec2(cos(a), sin(a));
}

vec2 quintic(vec2 t) { return t * t * t * (t * (t * 6.0 - 15.0) + 10.0); }

float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    vec2 u = quintic(f);
    float a = dot(hash22(i), f);
    float b = dot(hash22(i + vec2(1.0, 0.0)), f - vec2(1.0, 0.0));
    float c = dot(hash22(i + vec2(0.0, 1.0)), f - vec2(0.0, 1.0));
    float d = dot(hash22(i + vec2(1.0, 1.0)), f - vec2(1.0, 1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y) * 1.4;
}

float fbm(vec2 p) {
    float s = 0.0, a = 0.5, n = 0.0;
    for (int i = 0; i < 5; i++) { s += a * noise(p); n += a; p *= 2.02; a *= 0.5; }
    return s / n;
}

float box(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec3 c;

    // Row 1, top third: colour bars. Saturated primaries and secondaries, which
    // is what makes a colour-space mistake in a filter obvious at a glance.
    if (uv.y > 0.667) {
        float i = floor(uv.x * 8.0);
        vec3 bars[8] = vec3[8](
            vec3(1.0), vec3(1.0, 1.0, 0.0), vec3(0.0, 1.0, 1.0), vec3(0.0, 1.0, 0.0),
            vec3(1.0, 0.0, 1.0), vec3(1.0, 0.0, 0.0), vec3(0.0, 0.0, 1.0), vec3(0.05));
        c = bars[int(i)];

    // Row 2, middle third: a greyscale ramp on the left, so banding and gamma
    // problems show, and frequency wedges on the right, so a blur's or a
    // resample's loss of detail is measurable rather than a matter of opinion.
    } else if (uv.y > 0.333) {
        vec2 p = vec2(uv.x, (uv.y - 0.333) / 0.334);
        if (p.x < 0.5) {
            float t = p.x * 2.0;
            c = vec3(p.y > 0.5 ? t : floor(t * 12.0) / 11.0);
        } else {
            float t = (p.x - 0.5) * 2.0;
            // Frequency rises across the wedge; the point at which it turns
            // grey is where the process being tested stopped resolving it.
            float freq = mix(6.0, 220.0, t * t);
            float bars = 0.5 + 0.5 * sin(p.y * freq * 3.0);
            c = vec3(bars);
        }

    // Row 3, bottom third: something photographic. A warped fbm with relief,
    // because a filter tested only on flat colour and hard edges will look fine
    // and fall apart on continuous tone.
    } else {
        vec2 p = vec2(uv.x, uv.y / 0.334) * vec2(3.2, 1.6) + seed;
        vec2 q = vec2(fbm(p), fbm(p + vec2(5.2, 1.3)));
        float n = fbm(p + 2.2 * q);
        // Deliberately low saturation. A test card's continuous-tone region
        // is there to show what a filter does to gentle gradients, and a
        // hyper-saturated one hides exactly that.
        vec3 base = 0.5 + 0.30 * cos(TAU * (vec3(n * 0.45 + 0.08) + vec3(0.0, 0.12, 0.26)));
        vec2 g = vec2(dFdx(n), dFdy(n)) * 26.0;
        float lam = clamp(dot(normalize(vec3(-g, 0.25)), normalize(vec3(-0.5, 0.7, 0.5))), 0.0, 1.0);
        c = base * (0.55 + 0.85 * lam);
    }

    if (labels) {
        // A centre cross and corner brackets, so a reader can see immediately
        // whether a filter has shifted, flipped, or cropped the image. A Y-flip
        // is the single most common mistake in this module and it is invisible
        // on a symmetric test image.
        vec2 p = (uv - 0.5) * vec2(RENDERSIZE.x / RENDERSIZE.y, 1.0);
        // Drawn with a dark outline under a light core, so it is visible over
        // white, over black, and over the wedges alike. A single-colour
        // registration mark disappears over half a test card.
        float cross = min(box(p, vec2(0.16, 0.004)), box(p, vec2(0.004, 0.16)));
        c = mix(c, vec3(0.0), smoothstep(fwidth(cross), 0.0, cross - 0.004));
        c = mix(c, vec3(1.0), smoothstep(fwidth(cross), 0.0, cross));

        // One bracket only in the top left, deliberately asymmetric, so
        // orientation is unambiguous.
        vec2 tl = p - vec2(-0.78, 0.42);
        float bracket = min(box(tl, vec2(0.07, 0.005)), box(tl, vec2(0.005, 0.07)));
        c = mix(c, vec3(0.0), smoothstep(fwidth(bracket), 0.0, bracket - 0.004));
        c = mix(c, vec3(1.0, 0.25, 0.35), smoothstep(fwidth(bracket), 0.0, bracket));

        float seam = min(abs(uv.y - 0.333), abs(uv.y - 0.667));
        c = mix(c, vec3(0.0), smoothstep(1.5 / RENDERSIZE.y, 0.0, seam) * 0.7);
    }

    gl_FragColor = vec4(clamp(c, 0.0, 1.0), 1.0);
}
