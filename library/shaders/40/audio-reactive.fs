/*{
  "DESCRIPTION": "Audio-reactive done properly: three bands, separate attack and release per band, an impulse on onset, and every mapping exposed so it can be tuned to the room rather than to the file.",
  "CREDIT": "Eduardo Meneses, Learn shader art",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Generator", "Audio"],
  "INPUTS": [
    { "NAME": "spectrum", "TYPE": "audioFFT", "LABEL": "Spectrum" },
    { "NAME": "lowGain",  "TYPE": "float", "LABEL": "Low gain",    "DEFAULT": 1.4, "MIN": 0.0, "MAX": 4.0 },
    { "NAME": "midGain",  "TYPE": "float", "LABEL": "Mid gain",    "DEFAULT": 1.0, "MIN": 0.0, "MAX": 4.0 },
    { "NAME": "highGain", "TYPE": "float", "LABEL": "High gain",   "DEFAULT": 1.6, "MIN": 0.0, "MAX": 4.0 },
    { "NAME": "floorDb",  "TYPE": "float", "LABEL": "Noise floor", "DEFAULT": 0.18, "MIN": 0.0, "MAX": 0.8 },
    { "NAME": "attack",   "TYPE": "float", "LABEL": "Attack s",    "DEFAULT": 0.02, "MIN": 0.001, "MAX": 0.5 },
    { "NAME": "release",  "TYPE": "float", "LABEL": "Release s",   "DEFAULT": 0.35, "MIN": 0.02,  "MAX": 3.0 },
    { "NAME": "swell",    "TYPE": "float", "LABEL": "Low drives size",  "DEFAULT": 0.35, "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "warp",     "TYPE": "float", "LABEL": "Mid drives warp",  "DEFAULT": 0.60, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "sparkle",  "TYPE": "float", "LABEL": "High drives detail","DEFAULT": 0.50, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "hue",      "TYPE": "float", "LABEL": "Hue",         "DEFAULT": 0.60, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "spread",   "TYPE": "float", "LABEL": "Hue spread",  "DEFAULT": 0.30, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "showMeters","TYPE": "bool", "LABEL": "Show the meters", "DEFAULT": true }
  ],
  "PASSES": [
    { "TARGET": "envelopes", "PERSISTENT": true, "FLOAT": true, "WIDTH": "4", "HEIGHT": "1" },
    { }
  ]
}*/

const float TAU = 6.28318530718;

vec3 palette(float t) {
    vec3 d = vec3(hue) + spread * vec3(0.0, 0.30, 0.60);
    return 0.5 + 0.46 * cos(TAU * (vec3(t) + d));
}

// Sum a range of FFT bins. Reading one bin is the beginner's mistake: it is
// noisy, it misses the note whenever the pitch moves, and it makes the whole
// patch depend on the tuning of whatever is playing. A band is steady.
float band(float from, float to) {
    float sum = 0.0;
    float n = 0.0;
    for (int i = 0; i < 64; i++) {
        float t = float(i) / 64.0;
        if (t < from || t >= to) continue;
        sum += IMG_NORM_PIXEL(spectrum, vec2(t, 0.5)).r;
        n += 1.0;
    }
    return n > 0.0 ? sum / n : 0.0;
}

void main() {
    vec2 uv = isf_FragNormCoord;

    if (PASSINDEX == 0) {
        // A four-texel persistent buffer holding the three band envelopes and
        // one onset value. This is the whole reason the shader has a pass: an
        // envelope has memory, and memory means a persistent buffer.
        float which = floor(uv.x * 4.0);

        // Bands, chosen by ear rather than by theory: bass under about 200 Hz,
        // mids to about 2 kHz, and everything above. The FFT texture's x axis
        // is linear in frequency, so the low band is a small slice of it.
        float raw =
              which < 0.5 ? band(0.00, 0.06) * lowGain
            : which < 1.5 ? band(0.06, 0.28) * midGain
            : which < 2.5 ? band(0.28, 1.00) * highGain
            : 0.0;

        // Subtract a noise floor before scaling. Without it, room tone drives
        // the visuals and the piece never sits still between cues.
        raw = max(raw - floorDb, 0.0) / max(1.0 - floorDb, 1e-3);

        vec4 previous = IMG_NORM_PIXEL(envelopes, uv);

        if (which < 2.5) {
            // Separate attack and release, both frame-rate independent. A fixed
            // per-frame fraction feels twice as fast at 120 fps as at 60, which
            // is the single most common bug in audio-reactive work and it only
            // shows up on someone else's machine.
            float target = raw;
            float current = previous.r;
            float tau = target > current ? attack : release;
            float k = 1.0 - exp(-TIMEDELTA / max(tau, 1e-4));
            gl_FragColor = vec4(mix(current, target, k), 0.0, 0.0, 1.0);
        } else {
            // Onset: how much the low band rose this frame, held with a fast
            // decay. This is what to drive a flash or a kick from, and it is
            // not the same signal as the envelope.
            float low = IMG_NORM_PIXEL(envelopes, vec2(0.125, 0.5)).r;
            float rise = max(band(0.00, 0.06) * lowGain - floorDb, 0.0) - low;
            float held = previous.r * exp(-TIMEDELTA / 0.12);
            gl_FragColor = vec4(max(held, clamp(rise * 6.0, 0.0, 1.0)), 0.0, 0.0, 1.0);
        }
        return;
    }

    float low   = IMG_NORM_PIXEL(envelopes, vec2(0.125, 0.5)).r;
    float mid   = IMG_NORM_PIXEL(envelopes, vec2(0.375, 0.5)).r;
    float high  = IMG_NORM_PIXEL(envelopes, vec2(0.625, 0.5)).r;
    float onset = IMG_NORM_PIXEL(envelopes, vec2(0.875, 0.5)).r;

    vec2 p = (uv - 0.5) * RENDERSIZE / RENDERSIZE.y;

    // Each band drives one property, and one only. A patch where everything is
    // driven by one level pumps: the whole image breathes together and reads as
    // a meter rather than as a picture. Different bands on different properties
    // with different time constants is what makes it read as music.
    float r = length(p) / (0.42 + swell * low);
    float a = atan(p.y, p.x) / TAU;

    float rings = sin((r * 9.0 - TIME * 0.35 + warp * mid * sin(a * TAU * 3.0 + TIME)) * TAU);
    float grain = sin((a * 40.0 + r * 22.0) * TAU) * sparkle * high;

    float field = rings * 0.5 + 0.5 + grain * 0.25;
    vec3 colour = palette(0.15 + field * 0.5 + low * 0.15);
    colour *= smoothstep(1.35, 0.15, r);
    colour += palette(0.5) * onset * 0.55 * smoothstep(1.0, 0.0, r);

    if (showMeters) {
        // Three meters and an onset lamp. Every audio-reactive patch needs
        // these while it is being tuned, and every one of them should have them
        // switched off before it is shown.
        // uv.y runs upward, so the meters sit at the bottom without a flip.
        if (uv.y < 0.06 && uv.x < 0.5) {
            float slot = floor(uv.x / 0.5 * 3.0);
            float v = slot < 0.5 ? low : (slot < 1.5 ? mid : high);
            float x = fract(uv.x / 0.5 * 3.0);
            vec3 bar = x < v ? vec3(0.85, 0.92, 1.0) : vec3(0.10, 0.12, 0.16);
            colour = mix(colour, bar, 0.9);
        }
        colour = mix(colour, vec3(1.0, 0.35, 0.35),
                     step(uv.x, 0.02) * step(uv.y, 0.04) * onset);
    }

    gl_FragColor = vec4(clamp(colour, 0.0, 1.0), 1.0);
}
