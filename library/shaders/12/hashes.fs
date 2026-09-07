/*{
  "DESCRIPTION": "Four ways to get a repeatable random number out of a coordinate, with the failure modes of the cheap ones made visible rather than described.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The sine hash is folklore; the fract-multiply hash is Dave Hoskins's; the integer mix is Chris Wellons's lowbias32, as recommended for GPU work by Jarzynski and Olano.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Noise"],
  "INPUTS": [
    {
      "NAME": "method",
      "TYPE": "long",
      "LABEL": "Hash",
      "VALUES": [0, 1, 2, 3],
      "LABELS": ["fract(sin) 1D", "fract(sin) 2D", "fract-multiply 2D", "integer bit mix"],
      "DEFAULT": 1
    },
    {
      "NAME": "view",
      "TYPE": "long",
      "LABEL": "View",
      "VALUES": [0, 1, 2],
      "LABELS": ["per pixel", "per cell", "value against neighbour"],
      "DEFAULT": 1
    },
    { "NAME": "cell",   "TYPE": "float", "LABEL": "Cell size",  "DEFAULT": 24.0, "MIN": 2.0, "MAX": 200.0 },
    { "NAME": "zoom",   "TYPE": "float", "LABEL": "Zoom",       "DEFAULT": 1.0,  "MIN": 0.05, "MAX": 400.0 },
    { "NAME": "offset", "TYPE": "float", "LABEL": "Offset",     "DEFAULT": 0.0,  "MIN": 0.0, "MAX": 5000.0 },
    { "NAME": "animate","TYPE": "bool",  "LABEL": "Reseed over time", "DEFAULT": false }
  ]
}*/

// --- 1. the sine hash, one dimension ---------------------------------------
//
// Multiply, take a sine, multiply by something large, keep the fractional part.
// It is one line and it is everywhere in published shaders. It is also the only
// function in this course whose result is not guaranteed: sin's precision is
// implementation defined, so this returns different numbers on different
// drivers, and on some mobile hardware it degenerates into visible bands.
float hash11(float p) {
    return fract(sin(p * 12.9898) * 43758.5453123);
}

// --- 2. the sine hash, two dimensions --------------------------------------
//
// The same trick with a dot product. The classic constants are (12.9898,
// 78.233) and 43758.5453, and they are not magic: they are large, irrational
// enough, and were picked by someone in 2008. The dot product means the input
// is projected onto one direction first, which is exactly why this hash has a
// grain: points along a line perpendicular to that direction hash similarly.
float hash21(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453123);
}

// --- 3. fract-multiply, no transcendental ----------------------------------
//
// Dave Hoskins's approach: stay in the fractional part and stir. No sine, so no
// precision surprise, and it is cheaper. Good enough for almost everything in
// this course.
float hash21b(vec2 p) {
    vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    q += dot(q, q.yzx + 33.33);
    return fract((q.x + q.y) * q.z);
}

// --- 4. integer bit mixing --------------------------------------------------
//
// What a hash function is actually supposed to be. Convert to integers, mix the
// bits with shifts and multiplies until every input bit affects every output
// bit, then convert back. It is exact, identical on every conforming device,
// and has no grain, no bands, and no correlation between neighbours. It costs a
// handful of integer operations, which on any GPU made this decade is nothing.
uint hashUint(uint x) {
    x ^= x >> 16;
    x *= 0x7feb352du;
    x ^= x >> 15;
    x *= 0x846ca68bu;
    x ^= x >> 16;
    return x;
}

float hash21i(vec2 p) {
    uvec2 q = uvec2(ivec2(floor(p)) + 0x7fff);
    uint h = hashUint(q.x ^ hashUint(q.y * 0x9e3779b9u));
    return float(h) * (1.0 / 4294967296.0);
}

float hashOf(vec2 p) {
    if (method == 0) return hash11(p.x + p.y * 57.0);
    if (method == 1) return hash21(p);
    if (method == 2) return hash21b(p);
    return hash21i(p);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y;

    float seed = offset + (animate ? floor(TIME * 2.0) * 137.0 : 0.0);

    vec3 colour;

    if (view == 0) {
        // One number per pixel. At zoom 1 this is white noise; wind Zoom up and
        // the cheap hashes start to show structure that should not be there.
        vec2 q = p * zoom * 256.0 + seed;
        colour = vec3(hashOf(q));

    } else if (view == 1) {
        // One number per cell, which is how a hash is actually used: to give
        // each cell of a repetition its own value. Unit 13 builds noise out of
        // exactly this by interpolating between neighbouring cells.
        vec2 q = floor(p * cell + seed);
        colour = vec3(hashOf(q));

    } else {
        // A correlation plot: plot each cell's value against its neighbour's.
        // A good hash scatters evenly over the square; a bad one draws lines,
        // because its output depends on its input in a way simple enough to
        // see. This is the only view that separates methods 2 and 3, and it
        // separates them immediately.
        //
        // Every pixel loops over the whole sample set rather than owning one
        // sample, because a fragment shader cannot scatter: it can only ask
        // "does anything land on me". The loop bound is a compile-time constant
        // for the same portability reason as Unit 11's supersampler.
        const int SAMPLES = 192;
        float hit = 0.0;
        float radius = 2.5 / RENDERSIZE.y;
        for (int i = 0; i < SAMPLES; i++) {
            float fi = float(i);
            vec2 pt = vec2(hashOf(vec2(fi + seed, seed)),
                           hashOf(vec2(fi + 1.0 + seed, seed)));
            hit = max(hit, 1.0 - smoothstep(0.0, radius, length(uv - pt)));
        }
        colour = vec3(0.04, 0.05, 0.07) + vec3(0.30, 0.85, 0.80) * hit;
        // A frame, so the square reads as a plot rather than as a field.
        vec2 edge = min(uv, 1.0 - uv);
        colour += vec3(0.14) * smoothstep(2.0 / RENDERSIZE.y, 0.0, min(edge.x, edge.y));
    }

    gl_FragColor = vec4(colour, 1.0);
}
