/*{
  "DESCRIPTION": "One shader exercising every ISF input type and both pass features, so the header can be read against the panel it produces.",
  "CREDIT": "Eduardo Meneses, Learn shader art.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Utility"],
  "INPUTS": [
    { "NAME": "source",  "TYPE": "image", "LABEL": "An image input" },
    { "NAME": "enabled", "TYPE": "bool",  "LABEL": "A bool", "DEFAULT": true },
    {
      "NAME": "arrangement",
      "TYPE": "long",
      "LABEL": "A long, with labels",
      "VALUES": [0, 1, 2],
      "LABELS": ["quarters", "columns", "rings"],
      "DEFAULT": 0
    },
    { "NAME": "amount",  "TYPE": "float",   "LABEL": "A float", "DEFAULT": 0.45, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "anchor",  "TYPE": "point2D", "LABEL": "A point2D", "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "tintA",   "TYPE": "color",   "LABEL": "A color", "DEFAULT": [0.27, 0.88, 0.83, 1.0] },
    { "NAME": "level",   "TYPE": "audioFFT","LABEL": "An audioFFT input" },
    { "NAME": "trailing","TYPE": "float",   "LABEL": "Trail", "DEFAULT": 0.90, "MIN": 0.0, "MAX": 0.99 }
  ],
  "PASSES": [
    { "TARGET": "quarter", "WIDTH": "$WIDTH/4", "HEIGHT": "$HEIGHT/4" },
    { "TARGET": "memory", "PERSISTENT": true, "FLOAT": true },
    { }
  ]
}*/

const float TAU = 6.28318530718;

void main() {
    vec2 uv = isf_FragNormCoord;

    if (PASSINDEX == 0) {
        // A pass with a sized target. WIDTH and HEIGHT are expressions in
        // $WIDTH and $HEIGHT, so a buffer can be a fraction of the output
        // without the shader knowing the output's size in advance. This one is
        // a quarter, which is how a cheap blur or a downsample is done.
        gl_FragColor = IMG_NORM_PIXEL(source, uv);
        return;
    }

    if (PASSINDEX == 1) {
        // A persistent pass. It reads its own previous frame, which is the only
        // memory a shader has.
        vec4 previous = IMG_NORM_PIXEL(memory, uv);

        // audioFFT arrives as a one-row texture: x is frequency, and the value
        // is that band's level. Reading a few bands and taking the maximum is
        // the usual way to get a single number to drive something with, and it
        // is a good deal steadier than reading one band.
        float bass = 0.0;
        for (int i = 0; i < 8; i++) {
            bass = max(bass, IMG_NORM_PIXEL(level, vec2(float(i) / 64.0, 0.5)).r);
        }

        float d = length((uv - anchor) * vec2(RENDERSIZE.x / RENDERSIZE.y, 1.0));
        float pulse = smoothstep(0.06 + 0.20 * bass, 0.0, d);

        vec3 fresh = tintA.rgb * pulse * (0.4 + 1.6 * bass);
        gl_FragColor = vec4(max(previous.rgb * trailing, fresh), 1.0);
        return;
    }

    // The display pass, showing all three sources side by side so the header
    // and the picture can be read against each other.
    vec3 colour;

    if (arrangement == 0) {
        vec2 q = fract(uv * 2.0);
        bool right = uv.x > 0.5, top = uv.y > 0.5;
        if (top && !right)      colour = IMG_NORM_PIXEL(source, q).rgb;
        else if (top && right)  colour = IMG_NORM_PIXEL(quarter, q).rgb;
        else if (!top && !right) colour = IMG_NORM_PIXEL(memory, q).rgb;
        else                    colour = vec3(uv.x, uv.y, amount);
    } else if (arrangement == 1) {
        float band = floor(uv.x * 3.0);
        vec2 q = vec2(fract(uv.x * 3.0), uv.y);
        if (band < 0.5)      colour = IMG_NORM_PIXEL(source, q).rgb;
        else if (band < 1.5) colour = IMG_NORM_PIXEL(quarter, q).rgb;
        else                 colour = IMG_NORM_PIXEL(memory, q).rgb;
    } else {
        float r = length((uv - 0.5) * vec2(RENDERSIZE.x / RENDERSIZE.y, 1.0));
        float ring = fract(r * 3.0 - TIME * 0.2);
        colour = mix(IMG_NORM_PIXEL(source, uv).rgb,
                     IMG_NORM_PIXEL(memory, uv).rgb, step(0.5, ring));
    }

    // A bool used as an actual switch, and a float used as a mix amount, so the
    // two simplest input types are visible in the result.
    if (!enabled) colour = vec3(dot(colour, vec3(0.2126, 0.7152, 0.0722)));
    colour = mix(colour, colour * tintA.rgb * 1.6, amount);

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
