/*{
  "DESCRIPTION": "The Module D milestone: an animated surface that repeats exactly, built by moving through noise on a closed path rather than by sliding through it forever.",
  "CREDIT": "Eduardo Meneses, Learn shader art. Domain warping and the cosine palette are Inigo Quilez's; the quintic curve is Ken Perlin's; the hash is Dave Hoskins's; cellular noise is Steven Worley's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Generator"],
  "INPUTS": [
    { "NAME": "period",   "TYPE": "float", "LABEL": "Loop length s", "DEFAULT": 8.0,  "MIN": 1.0, "MAX": 30.0 },
    { "NAME": "density",  "TYPE": "float", "LABEL": "Scale",         "DEFAULT": 2.6,  "MIN": 0.5, "MAX": 10.0 },
    { "NAME": "octaves",  "TYPE": "float", "LABEL": "Octaves",       "DEFAULT": 5.0,  "MIN": 1.0, "MAX": 7.0 },
    { "NAME": "swirl",    "TYPE": "float", "LABEL": "Swirl",         "DEFAULT": 3.2,  "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "travel",   "TYPE": "float", "LABEL": "Travel",        "DEFAULT": 0.55, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "cellular", "TYPE": "float", "LABEL": "Cellular",      "DEFAULT": 0.35, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "cellSize", "TYPE": "float", "LABEL": "Cell size",     "DEFAULT": 5.0,  "MIN": 1.0, "MAX": 20.0 },
    { "NAME": "contrast", "TYPE": "float", "LABEL": "Contrast",      "DEFAULT": 0.30, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "shade",    "TYPE": "float", "LABEL": "Relief",        "DEFAULT": 0.55, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "hue",      "TYPE": "float", "LABEL": "Hue",           "DEFAULT": 0.72, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spread",   "TYPE": "float", "LABEL": "Hue spread",    "DEFAULT": 0.26, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "grain",    "TYPE": "float", "LABEL": "Grain",         "DEFAULT": 0.03, "MIN": 0.0, "MAX": 0.12 }
  ]
}*/

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 a = vec3(0.48, 0.46, 0.50), b = vec3(0.45, 0.44, 0.46), c = vec3(1.0);
    vec3 d = vec3(hue) + spread * vec3(0.0, 0.30, 0.62);
    return a + b * cos(TAU * (c * t + d));
}

float hash21(vec2 p) {
    vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    q += dot(q, q.yzx + 33.33);
    return fract((q.x + q.y) * q.z);
}

vec2 hash22(vec2 p) {
    float a = hash21(p) * TAU;
    float b = hash21(p + 19.19);
    return vec2(cos(a), sin(a)) * (0.7 + 0.3 * b);
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
    float sum = 0.0, amp = 0.5, norm = 0.0;
    int count = int(floor(octaves + 0.5));
    for (int i = 0; i < 7; i++) {
        if (i >= count) break;
        sum += amp * noise(p);
        norm += amp;
        p *= 2.02;      // not exactly 2, so octave lattices do not align
        amp *= 0.5;
    }
    return sum / max(norm, 1e-5);
}

float cells(vec2 p) {
    vec2 cell = floor(p);
    float f1 = 1e9;
    for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
            vec2 c = cell + vec2(float(i), float(j));
            vec2 s = c + 0.5 + 0.5 * vec2(cos(hash21(c) * TAU), sin(hash21(c + 7.3) * TAU));
            f1 = min(f1, dot(s - p, s - p));
        }
    }
    return sqrt(f1);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y * density;

    // The whole milestone is this pair of lines.
    //
    // Sliding through noise, p + vec2(TIME * speed, 0), never repeats: the field
    // is infinite and you are travelling along it. To repeat exactly, travel on
    // a *closed path* instead. A circle in the noise's own domain returns to
    // where it started after one period, so frame N and frame N + period are
    // asking the noise the same question and must give the same answer.
    //
    // Nothing is stored, nothing is faded, and there is no crossfade at the
    // seam. The loop is exact because the input is periodic.
    float phase = TAU * TIME / max(period, 0.001);
    vec2 loop = travel * vec2(cos(phase), sin(phase));

    // Domain warping from Unit 15, with the loop offset applied at the warp
    // stage rather than to p. Warping the warp is what makes the motion look
    // like flow rather than like a texture being dragged.
    vec2 q = vec2(fbm(p + loop), fbm(p + loop + vec2(5.2, 1.3)));
    vec2 r = vec2(fbm(p + swirl * 0.25 * q + vec2(1.7, 9.2)),
                  fbm(p + swirl * 0.25 * q + vec2(8.3, 2.8)));
    float n = fbm(p + swirl * 0.25 * r);

    // Voronoi from Unit 16, on the same closed path, mixed in by a control.
    // The two structures are very different and blending between them is the
    // fastest way to find a surface that is neither cloud nor scale.
    float c = cells(p * (cellSize / max(density, 0.001)) + loop * 1.6);
    n = mix(n, (c - 0.45) * 1.6, cellular);

    n = mix(n, smoothstep(-0.35, 0.35, n) * 2.0 - 1.0, contrast);

    vec3 colour = palette(0.5 + n * 0.55);

    // Relief from the field's own screen-space gradient, as in Unit 15.
    vec2 g = vec2(dFdx(n), dFdy(n)) * 22.0;
    vec3 normal = normalize(vec3(-g, 0.25));
    float lambert = clamp(dot(normal, normalize(vec3(-0.5, 0.7, 0.55))), 0.0, 1.0);
    colour *= mix(1.0, 0.55 + 0.85 * lambert, shade);

    colour += (hash21(uv * RENDERSIZE) - 0.5) * grain;

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
