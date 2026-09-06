/*{
  "DESCRIPTION": "A distance field to a circle, coloured by a palette, with every control exposed as a named parameter rather than a hard-coded number.",
  "CREDIT": "Eduardo Meneses, Learn shader art",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Generator", "Course"],
  "INPUTS": [
    { "NAME": "focus",     "TYPE": "point2D", "LABEL": "Focus",     "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "radius",    "TYPE": "float",   "LABEL": "Radius",    "DEFAULT": 0.30, "MIN": 0.02, "MAX": 0.80 },
    { "NAME": "softness",  "TYPE": "float",   "LABEL": "Softness",  "DEFAULT": 0.02, "MIN": 0.00, "MAX": 0.40 },
    { "NAME": "rings",     "TYPE": "float",   "LABEL": "Rings",     "DEFAULT": 6.0,  "MIN": 0.0,  "MAX": 40.0 },
    { "NAME": "drift",     "TYPE": "float",   "LABEL": "Drift",     "DEFAULT": 0.25, "MIN": 0.0,  "MAX": 2.0 },
    { "NAME": "tint",      "TYPE": "color",   "LABEL": "Tint",      "DEFAULT": [1.0, 0.62, 0.18, 1.0] }
  ]
}*/

// A palette in the Inigo Quilez cosine form: four vec3 constants make a whole
// family of gradients, and it costs three cosines instead of a texture lookup.
vec3 palette(float t, vec3 tint) {
    vec3 a = vec3(0.5);
    vec3 b = vec3(0.5);
    vec3 c = vec3(1.0, 1.0, 1.0);
    vec3 d = tint * vec3(0.00, 0.33, 0.67);
    return a + b * cos(6.28318530718 * (c * t + d));
}

void main() {
    // Square the coordinate space so a circle is a circle at any aspect ratio.
    vec2 uv = (isf_FragNormCoord * RENDERSIZE - 0.5 * RENDERSIZE) / RENDERSIZE.y;
    vec2 centre = (focus * RENDERSIZE - 0.5 * RENDERSIZE) / RENDERSIZE.y;

    float d = length(uv - centre) - radius;

    // Rings read the raw distance, so they show what the field actually is
    // rather than only where it crosses zero.
    float bands = sin(d * rings - TIME * drift * 6.28318530718);
    vec3 colour = palette(d * 1.5 + TIME * drift * 0.1, tint.rgb);
    colour *= 0.55 + 0.45 * bands;

    // One smoothstep is the edge. Softness of zero gives a hard edge that
    // aliases, which is the point of the lesson this figure belongs to.
    float edge = smoothstep(softness + 0.002, -0.002, d);
    colour = mix(colour * 0.12, colour, edge);

    gl_FragColor = vec4(colour, 1.0);
}
