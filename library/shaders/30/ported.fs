/*{
  "DESCRIPTION": "A Shadertoy-shaped shader translated into ISF, with the original's uniform names kept as defines so the two versions can be compared line by line.",
  "CREDIT": "Eduardo Meneses, Learn shader art. Written for this course as a generic Shadertoy-shaped plasma so that no individual author's work is republished; the cosine palette is Inigo Quilez's.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Generator"],
  "INPUTS": [
    { "NAME": "pointer",  "TYPE": "point2D", "LABEL": "Pointer",  "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "rate",     "TYPE": "float",   "LABEL": "Rate",     "DEFAULT": 0.4, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "scale",    "TYPE": "float",   "LABEL": "Scale",    "DEFAULT": 4.0, "MIN": 1.0, "MAX": 16.0 },
    { "NAME": "layers",   "TYPE": "float",   "LABEL": "Layers",   "DEFAULT": 5.0, "MIN": 1.0, "MAX": 10.0 },
    { "NAME": "hue",      "TYPE": "float",   "LABEL": "Hue",      "DEFAULT": 0.55, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spread",   "TYPE": "float",   "LABEL": "Hue spread","DEFAULT": 0.28, "MIN": 0.0, "MAX": 1.0 }
  ]
}*/

// ---------------------------------------------------------------------------
// The compatibility shim. Five lines, and a Shadertoy body compiles unchanged.
//
// iTime         -> TIME
// iResolution   -> vec3(RENDERSIZE, 1.0)
// iMouse        -> a named point2D input, in pixels
// fragCoord     -> isf_FragNormCoord * RENDERSIZE
// mainImage()   -> called from main()
//
// The one that is not a rename is iMouse. Shadertoy hands you a device; ISF
// hands you a parameter. This course never names an input after a device, so
// the shim maps `pointer` into iMouse's shape rather than the other way round,
// and the shader can then be driven by an automation curve or an OSC message
// with nothing renamed. That is the whole argument of Unit 30 in five lines.
// ---------------------------------------------------------------------------

#define iTime TIME
#define iResolution vec3(RENDERSIZE, 1.0)
#define iMouse vec4(pointer * RENDERSIZE, 0.0, 0.0)

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 d = vec3(hue) + spread * vec3(0.0, 0.30, 0.60);
    return 0.5 + 0.46 * cos(TAU * (vec3(t) + d));
}

// Below this line is written exactly as it would be on Shadertoy: same entry
// point, same signature, same uniform names, same pixel-space fragCoord.
void mainImage(out vec4 fragColor, in vec2 fragCoord) {
    vec2 uv = (fragCoord - 0.5 * iResolution.xy) / iResolution.y;
    vec2 m = (iMouse.xy - 0.5 * iResolution.xy) / iResolution.y;

    float t = iTime * rate;
    vec2 p = uv * scale;

    // A stack of rotating sine layers: the plasma every shader site has a
    // hundred versions of, and a fair test for a port because it touches time,
    // resolution, and the pointer.
    float acc = 0.0;
    int n = int(floor(layers + 0.5));
    for (int i = 0; i < 10; i++) {
        if (i >= n) break;
        float fi = float(i);
        float a = fi * 2.399 + t * (0.2 + 0.1 * fi);
        vec2 dir = vec2(cos(a), sin(a));
        acc += sin(dot(p, dir) + t * (1.0 + 0.3 * fi) + length(p - m * scale) * 0.6);
    }
    acc /= float(n);

    vec3 col = palette(0.5 + acc * 0.45);
    col *= 0.75 + 0.35 * cos(acc * TAU);

    fragColor = vec4(col, 1.0);
}

void main() {
    mainImage(gl_FragColor, isf_FragNormCoord * RENDERSIZE);
}
