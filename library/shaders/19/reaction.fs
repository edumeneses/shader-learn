/*{
  "DESCRIPTION": "A Gray-Scott reaction-diffusion simulation held entirely in a persistent float buffer: two chemicals, four numbers, and a whole zoo of patterns.",
  "CREDIT": "Eduardo Meneses, Learn shader art. The Gray-Scott model is Pearson's parameterisation.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Simulation"],
  "INPUTS": [
    {
      "NAME": "recipe",
      "TYPE": "long",
      "LABEL": "Recipe",
      "VALUES": [0, 1, 2, 3, 4, 5],
      "LABELS": ["custom", "coral", "mitosis", "worms", "spots", "waves"],
      "DEFAULT": 1
    },
    { "NAME": "feed",   "TYPE": "float",   "LABEL": "Feed",       "DEFAULT": 0.0545, "MIN": 0.005, "MAX": 0.11 },
    { "NAME": "kill",   "TYPE": "float",   "LABEL": "Kill",       "DEFAULT": 0.0620, "MIN": 0.03,  "MAX": 0.075 },
    { "NAME": "diffuseA","TYPE": "float",  "LABEL": "Diffuse A",  "DEFAULT": 1.00,   "MIN": 0.2,   "MAX": 1.4 },
    { "NAME": "diffuseB","TYPE": "float",  "LABEL": "Diffuse B",  "DEFAULT": 0.50,   "MIN": 0.1,   "MAX": 1.0 },
    { "NAME": "dt",     "TYPE": "float",   "LABEL": "Step size",  "DEFAULT": 1.0,    "MIN": 0.1,   "MAX": 1.4 },
    { "NAME": "seedAt", "TYPE": "point2D", "LABEL": "Seed",       "DEFAULT": [0.5, 0.5], "MIN": [0.0, 0.0], "MAX": [1.0, 1.0] },
    { "NAME": "seeding","TYPE": "bool",    "LABEL": "Seed while dragging", "DEFAULT": false },
    { "NAME": "reset",  "TYPE": "bool",    "LABEL": "Clear",      "DEFAULT": false },
    { "NAME": "hue",    "TYPE": "float",   "LABEL": "Hue",        "DEFAULT": 0.08,   "MIN": 0.0,   "MAX": 1.0 },
    { "NAME": "spread", "TYPE": "float",   "LABEL": "Hue spread", "DEFAULT": 0.30,   "MIN": 0.0,   "MAX": 1.0 },
    { "NAME": "relief", "TYPE": "float",   "LABEL": "Relief",     "DEFAULT": 0.55,   "MIN": 0.0,   "MAX": 1.0 }
  ],
  "PASSES": [
    { "TARGET": "state", "PERSISTENT": true, "FLOAT": true, "WIDTH": "$WIDTH/2", "HEIGHT": "$HEIGHT/2" },
    { "TARGET": "state", "PERSISTENT": true, "FLOAT": true, "WIDTH": "$WIDTH/2", "HEIGHT": "$HEIGHT/2" },
    { "TARGET": "state", "PERSISTENT": true, "FLOAT": true, "WIDTH": "$WIDTH/2", "HEIGHT": "$HEIGHT/2" },
    { "TARGET": "state", "PERSISTENT": true, "FLOAT": true, "WIDTH": "$WIDTH/2", "HEIGHT": "$HEIGHT/2" },
    { }
  ]
}*/

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 a = vec3(0.5), b = vec3(0.47), c = vec3(1.0);
    vec3 d = vec3(hue) + spread * vec3(0.0, 0.30, 0.60);
    return a + b * cos(TAU * (c * t + d));
}

float hash21(vec2 p) {
    vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    q += dot(q, q.yzx + 33.33);
    return fract((q.x + q.y) * q.z);
}

void recipeValues(out float f, out float k) {
    if (recipe == 1)      { f = 0.0545; k = 0.0620; }  // coral
    else if (recipe == 2) { f = 0.0367; k = 0.0649; }  // mitosis
    else if (recipe == 3) { f = 0.0580; k = 0.0630; }  // worms
    else if (recipe == 4) { f = 0.0300; k = 0.0620; }  // spots
    else if (recipe == 5) { f = 0.0140; k = 0.0450; }  // waves
    else                  { f = feed;   k = kill;   }
}

// The five-point Laplacian, weighted. This is the only place a fragment shader
// gets to look at its neighbours, and it can only do it because the neighbours
// are in a *texture* it is reading, not on a screen it is writing. That
// distinction is the whole of this unit.
vec2 laplacian(vec2 uv, vec2 texel) {
    vec2 sum = vec2(0.0);
    sum += IMG_NORM_PIXEL(state, uv + vec2(-texel.x, 0.0)).xy * 0.20;
    sum += IMG_NORM_PIXEL(state, uv + vec2( texel.x, 0.0)).xy * 0.20;
    sum += IMG_NORM_PIXEL(state, uv + vec2(0.0, -texel.y)).xy * 0.20;
    sum += IMG_NORM_PIXEL(state, uv + vec2(0.0,  texel.y)).xy * 0.20;
    sum += IMG_NORM_PIXEL(state, uv + vec2(-texel.x, -texel.y)).xy * 0.05;
    sum += IMG_NORM_PIXEL(state, uv + vec2( texel.x, -texel.y)).xy * 0.05;
    sum += IMG_NORM_PIXEL(state, uv + vec2(-texel.x,  texel.y)).xy * 0.05;
    sum += IMG_NORM_PIXEL(state, uv + vec2( texel.x,  texel.y)).xy * 0.05;
    sum -= IMG_NORM_PIXEL(state, uv).xy;
    return sum;
}

void main() {
    vec2 uv = isf_FragNormCoord;

    // Four simulation passes, then one display pass. The model is only stable
    // at small steps, so one step per displayed frame would evolve about four
    // times too slowly to watch; four passes naming the same persistent target
    // are four real steps, because a persistent target swaps after the pass
    // that wrote it rather than at the end of the frame.
    if (PASSINDEX < 4) {
        vec2 texel = 1.0 / IMG_SIZE(state);

        float f, k;
        recipeValues(f, k);

        vec2 ab = IMG_NORM_PIXEL(state, uv).xy;

        // The initial condition. A field of A with a patch of B in the middle:
        // the simulation does nothing at all from a uniform state, so a seed is
        // not decoration, it is the only reason anything happens.
        if (reset || FRAMEINDEX < 2) {
            float d = length((uv - 0.5) * vec2(IMG_SIZE(state).x / IMG_SIZE(state).y, 1.0));
            float blob = step(d, 0.06) * (0.4 + 0.6 * hash21(uv * 512.0));
            gl_FragColor = vec4(1.0, blob, 0.0, 1.0);
            return;
        }

        // One step. Not a loop: the Laplacian reads the *texture*, so iterating
        // here would apply the reaction several times against a neighbourhood
        // that never updated, which diverges into a checkerboard within a
        // second. The first version of this shader did exactly that. Several
        // steps means several passes.
        vec2 lap = laplacian(uv, texel);
        float A = ab.x, B = ab.y;
        float reactionTerm = A * B * B;
        // Gray-Scott, in full:
        //   A' = Da * lap(A) - A*B^2 + feed * (1 - A)
        //   B' = Db * lap(B) + A*B^2 - (kill + feed) * B
        // A is fed in from outside and consumed; B is produced by the reaction
        // and removed at a fixed rate. Everything the model does comes from the
        // competition between those two.
        ab.x += dt * (diffuseA * lap.x - reactionTerm + f * (1.0 - A));
        ab.y += dt * (diffuseB * lap.y + reactionTerm - (k + f) * B);
        ab = clamp(ab, 0.0, 1.0);

        // Seeding only on the last simulation pass, so a drag adds one blob per
        // frame rather than four.
        if (seeding && PASSINDEX == 3) {
            float d = length((uv - seedAt) * vec2(IMG_SIZE(state).x / IMG_SIZE(state).y, 1.0));
            ab.y = max(ab.y, smoothstep(0.045, 0.0, d));
        }

        gl_FragColor = vec4(ab, 0.0, 1.0);

    } else {
        // The display pass. B is the interesting chemical; A is its complement
        // almost everywhere, so colouring by B and shading by its gradient is
        // enough to make the structure read.
        float b = IMG_THIS_NORM_PIXEL(state).y;

        vec3 colour = palette(0.15 + b * 1.6);

        vec2 g = vec2(dFdx(b), dFdy(b)) * 90.0;
        vec3 normal = normalize(vec3(-g, 0.4));
        float lambert = clamp(dot(normal, normalize(vec3(-0.5, 0.65, 0.55))), 0.0, 1.0);
        colour *= mix(1.0, 0.5 + 0.9 * lambert, relief);

        gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
    }
}
