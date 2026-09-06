/*{
  "DESCRIPTION": "The four smallest useful fragment shaders, one after another: a flat colour, the coordinate as colour, a single channel, and the coordinate moved by time.",
  "CREDIT": "Eduardo Meneses, Learn shader art",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course"],
  "INPUTS": [
    {
      "NAME": "stage",
      "TYPE": "long",
      "LABEL": "Stage",
      "VALUES": [0, 1, 2, 3],
      "LABELS": ["1. a flat colour", "2. the coordinate as colour", "3. one channel", "4. the coordinate, moved"],
      "DEFAULT": 1
    },
    { "NAME": "flatColour", "TYPE": "color", "LABEL": "Flat colour", "DEFAULT": [0.27, 0.88, 0.83, 1.0] },
    { "NAME": "rate",  "TYPE": "float", "LABEL": "Rate",        "DEFAULT": 0.20, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "gamma", "TYPE": "float", "LABEL": "Gamma",       "DEFAULT": 1.00, "MIN": 0.3, "MAX": 3.0 }
  ]
}*/

void main() {
    // The only thing that differs between the two million invocations of this
    // function. It runs from 0 at the left and bottom to 1 at the right and top.
    vec2 uv = isf_FragNormCoord;

    vec3 colour;

    if (stage == 0) {
        // Stage 1. The smallest shader that does anything: ignore the
        // coordinate entirely and answer the same colour everywhere.
        colour = flatColour.rgb;

    } else if (stage == 1) {
        // Stage 2. Use the coordinate as the answer. Red rises to the right
        // because that is x; green rises upwards because that is y. There is no
        // gradient object here and nothing was interpolated by you: each pixel
        // independently reported where it was.
        colour = vec3(uv, 0.0);

    } else if (stage == 2) {
        // Stage 3. One channel, so the picture is a grey ramp and it is obvious
        // that a colour is three numbers rather than a thing.
        colour = vec3(uv.x);

    } else {
        // Stage 4. Add a uniform to the coordinate before using it. Nothing
        // moves; each frame is computed from scratch with a larger TIME, and
        // fract wraps the result back into 0..1 so the ramp repeats.
        colour = vec3(fract(uv.x + TIME * rate), uv.y, 0.35);
    }

    // Gamma, here only so that the difference between a number and a brightness
    // is visible at this stage rather than in Unit 04, where it is the subject.
    colour = pow(max(colour, 0.0), vec3(1.0 / gamma));

    gl_FragColor = vec4(colour, 1.0);
}
