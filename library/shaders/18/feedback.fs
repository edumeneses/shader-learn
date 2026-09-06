/*{
  "DESCRIPTION": "A persistent buffer that reads its own previous frame through a transform, which is the whole of visual feedback: trails, tunnels, spirals, and decay.",
  "CREDIT": "Eduardo Meneses, Learn shader art",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Feedback"],
  "INPUTS": [
    { "NAME": "source",   "TYPE": "point2D", "LABEL": "Source",   "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "sourceSize","TYPE": "float",  "LABEL": "Source size", "DEFAULT": 0.045, "MIN": 0.0, "MAX": 0.25 },
    { "NAME": "orbit",    "TYPE": "float",   "LABEL": "Orbit",    "DEFAULT": 0.30, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "decay",    "TYPE": "float",   "LABEL": "Decay",    "DEFAULT": 0.982, "MIN": 0.80, "MAX": 1.0 },
    { "NAME": "zoom",     "TYPE": "float",   "LABEL": "Zoom",     "DEFAULT": 1.010, "MIN": 0.96, "MAX": 1.05 },
    { "NAME": "twist",    "TYPE": "float",   "LABEL": "Twist",    "DEFAULT": 0.006, "MIN": -0.05, "MAX": 0.05 },
    { "NAME": "drag",     "TYPE": "point2D", "LABEL": "Drag",     "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "hueShift", "TYPE": "float",   "LABEL": "Hue drift","DEFAULT": 0.004, "MIN": -0.05, "MAX": 0.05 },
    { "NAME": "hue",      "TYPE": "float",   "LABEL": "Source hue","DEFAULT": 0.55, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "reset",    "TYPE": "bool",    "LABEL": "Clear",    "DEFAULT": false }
  ],
  "PASSES": [
    { "TARGET": "history", "PERSISTENT": true, "FLOAT": true },
    { }
  ]
}*/

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 a = vec3(0.5), b = vec3(0.48), c = vec3(1.0);
    vec3 d = vec3(t) + vec3(0.0, 0.28, 0.58);
    return a + b * cos(TAU * d);
}

mat2 rot(float turns) {
    float a = turns * TAU;
    return mat2(cos(a), -sin(a), sin(a), cos(a));
}

void main() {
    vec2 uv = isf_FragNormCoord;
    float aspect = RENDERSIZE.x / RENDERSIZE.y;

    if (PASSINDEX == 0) {
        // The feedback pass. Read the previous frame through a transform, fade
        // it, and add whatever is new. The transform is what turns a trail into
        // a structure: zoom alone gives a tunnel, twist alone gives a spiral,
        // and the two together give the shape everyone recognises.
        //
        // Note the direction. To make the image appear to zoom *out*, the
        // lookup zooms *in*, because this is Unit 09's inverse-transform rule
        // arriving in a place nobody expects it.
        vec2 centred = (uv - 0.5) * vec2(aspect, 1.0);
        vec2 pull = (drag - 0.5) * vec2(aspect, 1.0);
        centred = rot(twist) * (centred - pull * 0.02) / zoom;
        vec2 look = centred / vec2(aspect, 1.0) + 0.5;

        vec4 previous = IMG_NORM_PIXEL(history, look);

        // Outside the buffer there is nothing to read, and reading the clamped
        // edge instead smears the border inward. Fading to black at the edge is
        // the cheap honest answer.
        vec2 edge = min(look, 1.0 - look);
        float inside = smoothstep(0.0, 0.02, min(edge.x, edge.y));
        previous *= inside;

        // Decay. Below 1.0 the trail fades; at exactly 1.0 nothing ever leaves
        // and the buffer saturates within seconds, which is worth doing once.
        previous.rgb *= decay;

        // Drift the hue of what is already there, so a trail changes colour as
        // it ages. This is the one effect here that has no equivalent without
        // feedback: the colour depends on how long ago the pixel was written.
        if (abs(hueShift) > 1e-5) {
            float lum = dot(previous.rgb, vec3(0.2126, 0.7152, 0.0722));
            previous.rgb = mix(previous.rgb, palette(fract(lum + TIME * hueShift)) * lum * 1.7, 0.035);
        }

        // The new material: a small bright source travelling on a closed path,
        // so that with decay at 1.0 the figure would draw a rosette.
        vec2 p = (uv - 0.5) * vec2(aspect, 1.0);
        vec2 s = (source - 0.5) * vec2(aspect, 1.0);
        float phase = TIME * orbit;
        s += 0.22 * vec2(cos(phase * TAU), sin(phase * TAU * 0.75));

        float d = length(p - s) - sourceSize;
        float blob = smoothstep(sourceSize * 0.9 + fwidth(d), -fwidth(d), d);
        vec3 ink = palette(fract(hue + phase * 0.1)) * 1.4;

        vec3 result = previous.rgb + ink * blob;

        if (reset || FRAMEINDEX < 2) result = ink * blob;

        gl_FragColor = vec4(result, 1.0);

    } else {
        // The display pass. The buffer is a float target so it can hold values
        // above one without clipping; tone-mapping here rather than in the
        // feedback pass is what keeps the accumulation linear.
        vec3 c = IMG_THIS_NORM_PIXEL(history).rgb;
        c = c / (1.0 + c);                       // Reinhard, one line
        c = pow(max(c, 0.0), vec3(1.0 / 1.15));  // a light lift, no more
        gl_FragColor = vec4(c, 1.0);
    }
}
