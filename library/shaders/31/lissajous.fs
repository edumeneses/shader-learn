/*{
  "DESCRIPTION": "Sixty thousand points, each deciding where it goes from nothing but its own index. A vertex shader can scatter, which is the thing Unit 19 said a fragment shader cannot do.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The format is the Vertex Shader Art spec as ossia score implements it; the cosine palette is Inigo Quilez's.",
  "ISFVSN": "2.0",
  "MODE": "VERTEX_SHADER_ART",
  "CATEGORIES": ["Course", "Geometry", "Particles"],
  "POINT_COUNT": 60000,
  "PRIMITIVE_MODE": "POINTS",
  "BACKGROUND_COLOR": [0.02, 0.025, 0.04, 1.0],
  "INPUTS": [
    { "NAME": "figureA",  "TYPE": "float", "LABEL": "Ratio a",   "DEFAULT": 3.0, "MIN": 1.0, "MAX": 12.0 },
    { "NAME": "figureB",  "TYPE": "float", "LABEL": "Ratio b",   "DEFAULT": 2.0, "MIN": 1.0, "MAX": 12.0 },
    { "NAME": "phase",    "TYPE": "float", "LABEL": "Phase",     "DEFAULT": 0.25, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "strands",  "TYPE": "float", "LABEL": "Strands",   "DEFAULT": 60.0, "MIN": 1.0, "MAX": 300.0 },
    { "NAME": "spread",   "TYPE": "float", "LABEL": "Spread",    "DEFAULT": 0.30, "MIN": 0.0, "MAX": 1.2 },
    { "NAME": "twist",    "TYPE": "float", "LABEL": "Twist",     "DEFAULT": 0.55, "MIN": 0.0, "MAX": 3.0 },
    { "NAME": "drive",    "TYPE": "float", "LABEL": "Drive",     "DEFAULT": 0.5,  "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "pointSize","TYPE": "float", "LABEL": "Point size","DEFAULT": 2.0,  "MIN": 1.0, "MAX": 8.0 },
    { "NAME": "brightness","TYPE": "float","LABEL": "Brightness","DEFAULT": 0.16, "MIN": 0.01, "MAX": 0.6 },
    { "NAME": "hue",      "TYPE": "float", "LABEL": "Hue",       "DEFAULT": 0.58, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spreadHue","TYPE": "float", "LABEL": "Hue spread","DEFAULT": 0.32, "MIN": 0.0, "MAX": 1.0 }
  ]
}*/

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 d = vec3(hue) + spreadHue * vec3(0.0, 0.30, 0.60);
    return 0.5 + 0.46 * cos(TAU * (vec3(t) + d));
}

void main() {
    // Everything this point knows about itself is one number. Splitting it into
    // a strand index and a position along that strand is the whole technique:
    // one division and one modulo turn a flat list into a structured object.
    float perStrand = floor(vertexCount / max(strands, 1.0));
    float strand = floor(vertexId / perStrand);
    float along = mod(vertexId, perStrand) / perStrand;

    float t = TIME * drive;

    // A Lissajous figure: two sinusoids at a frequency ratio. The ratio is the
    // whole shape, and non-integer ratios never close, which is why the figure
    // fills in over time rather than repeating.
    float u = along * TAU;
    vec2 curve = vec2(sin(figureA * u + phase * TAU + t * 0.3),
                      sin(figureB * u + t * 0.21));

    // Each strand is the same figure at a slightly different phase and radius,
    // so the strands sweep past each other and the whole object reads as a
    // surface rather than as a line.
    float s = strand / max(strands, 1.0);
    float ang = s * TAU + t * 0.15;
    vec2 offset = vec2(cos(ang), sin(ang)) * spread * (0.35 + 0.65 * s);

    vec2 p = curve * 0.62 + offset;

    // Twist about the centre, by an amount that grows with radius, which is
    // Unit 09's transform applied to a position rather than to a lookup.
    float r = length(p);
    float a = atan(p.y, p.x) + r * twist * sin(t * 0.4);
    p = vec2(cos(a), sin(a)) * r;

    // Correct the aspect ratio here, in clip space, because a vertex shader has
    // no isf_FragNormCoord to have done it for us.
    float aspect = resolution.x / max(resolution.y, 1.0);
    gl_Position = vec4(p.x / aspect, p.y, 0.0, 1.0);
    gl_PointSize = pointSize;

    // Additive blending means brightness accumulates where strands cross, so
    // each point is drawn dim on purpose: with sixty thousand of them, a bright
    // point gives a white blob and nothing else.
    v_color = vec4(palette(s * 0.7 + along * 0.25) * brightness, 1.0);
}
