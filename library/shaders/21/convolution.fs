/*{
  "DESCRIPTION": "Convolution kernels, and a separable Gaussian done as two one-dimensional passes, with a control that switches to the naive two-dimensional version so the cost difference is visible rather than asserted.",
  "CREDIT": "Eduardo Meneses, Learn shader art",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Course", "Filter"],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image", "LABEL": "Source" },
    {
      "NAME": "kernel",
      "TYPE": "long",
      "LABEL": "Kernel",
      "VALUES": [0, 1, 2, 3, 4, 5],
      "LABELS": ["none", "gaussian blur", "sharpen", "edge detect", "emboss", "Sobel magnitude"],
      "DEFAULT": 1
    },
    { "NAME": "radius",   "TYPE": "float", "LABEL": "Radius",   "DEFAULT": 8.0, "MIN": 0.0, "MAX": 24.0 },
    { "NAME": "amount",   "TYPE": "float", "LABEL": "Amount",   "DEFAULT": 1.0, "MIN": 0.0, "MAX": 3.0 },
    { "NAME": "separable","TYPE": "bool",  "LABEL": "Separable  two 1D passes", "DEFAULT": true },
    { "NAME": "split",    "TYPE": "float", "LABEL": "Compare",  "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 }
  ],
  "PASSES": [
    { "TARGET": "horizontal" },
    { }
  ]
}*/

// A Gaussian weight. Computing it rather than storing a table means the radius
// can be a control: a kernel baked as constants can only be the size it was
// written as, which is why so many published blurs have a fixed radius.
float gaussian(float x, float sigma) {
    return exp(-(x * x) / (2.0 * sigma * sigma));
}

// The 3x3 kernels, as a function of the offset in texels. Written this way so
// all four share one loop instead of four nearly identical ones.
float weight3(int kind, ivec2 o) {
    if (kind == 2) {              // sharpen
        if (o == ivec2(0, 0)) return 5.0;
        if (abs(o.x) + abs(o.y) == 1) return -1.0;
        return 0.0;
    }
    if (kind == 3) {              // edge detect, a Laplacian
        if (o == ivec2(0, 0)) return 8.0;
        return -1.0;
    }
    if (kind == 4) {              // emboss
        return float(o.x + o.y) * (o == ivec2(0, 0) ? 0.0 : 1.0);
    }
    return 0.0;
}

vec3 sobel(vec2 uv, vec2 texel) {
    float gx = 0.0, gy = 0.0;
    for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
            vec3 s = IMG_NORM_PIXEL(inputImage, uv + vec2(float(i), float(j)) * texel).rgb;
            float lum = dot(s, vec3(0.2126, 0.7152, 0.0722));
            float wx = float(i) * (j == 0 ? 2.0 : 1.0);
            float wy = float(j) * (i == 0 ? 2.0 : 1.0);
            gx += lum * wx;
            gy += lum * wy;
        }
    }
    return vec3(length(vec2(gx, gy)));
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 texel = 1.0 / max(IMG_SIZE(inputImage), vec2(1.0));
    float sigma = max(radius, 0.001) * 0.5;
    int taps = int(clamp(ceil(radius * 1.5), 1.0, 24.0));

    if (PASSINDEX == 0) {
        // Pass one: the horizontal half of a separable blur, or a pass-through
        // when the kernel does not need one.
        if (kernel != 1 || !separable) {
            gl_FragColor = IMG_NORM_PIXEL(inputImage, uv);
            return;
        }
        vec3 sum = vec3(0.0);
        float norm = 0.0;
        for (int i = -24; i <= 24; i++) {
            if (abs(i) > taps) continue;
            float w = gaussian(float(i), sigma);
            sum += w * IMG_NORM_PIXEL(inputImage, uv + vec2(float(i), 0.0) * texel).rgb;
            norm += w;
        }
        gl_FragColor = vec4(sum / norm, 1.0);
        return;
    }

    vec3 original = IMG_NORM_PIXEL(inputImage, uv).rgb;
    vec3 result = original;

    if (kernel == 1) {
        if (separable) {
            // Pass two: the vertical half, reading pass one's output. Two
            // one-dimensional passes of n taps cost 2n samples and give exactly
            // the same result as one two-dimensional pass of n by n, which
            // costs n squared. At n = 25 that is 50 samples against 625.
            vec3 sum = vec3(0.0);
            float norm = 0.0;
            for (int i = -24; i <= 24; i++) {
                if (abs(i) > taps) continue;
                float w = gaussian(float(i), sigma);
                sum += w * IMG_NORM_PIXEL(horizontal, uv + vec2(0.0, float(i)) * texel).rgb;
                norm += w;
            }
            result = sum / norm;
        } else {
            // The naive version, capped hard. Without the cap this branch would
            // take 2401 samples per pixel at the maximum radius, which is a
            // genuinely bad time on integrated graphics.
            int n = min(taps, 12);
            vec3 sum = vec3(0.0);
            float norm = 0.0;
            for (int j = -12; j <= 12; j++) {
                if (abs(j) > n) continue;
                for (int i = -12; i <= 12; i++) {
                    if (abs(i) > n) continue;
                    float w = gaussian(float(i), sigma) * gaussian(float(j), sigma);
                    sum += w * IMG_NORM_PIXEL(inputImage, uv + vec2(float(i), float(j)) * texel).rgb;
                    norm += w;
                }
            }
            result = sum / norm;
        }

    } else if (kernel == 5) {
        result = sobel(uv, texel);

    } else if (kernel != 0) {
        vec3 sum = vec3(0.0);
        for (int j = -1; j <= 1; j++) {
            for (int i = -1; i <= 1; i++) {
                float w = weight3(kernel, ivec2(i, j));
                sum += w * IMG_NORM_PIXEL(inputImage, uv + vec2(float(i), float(j)) * texel).rgb;
            }
        }
        // Emboss and edge detect produce signed values, so they need a bias to
        // be visible at all; a filter that outputs negative light is not wrong,
        // it is just invisible.
        result = (kernel == 4) ? sum + 0.5 : sum;
    }

    result = mix(original, result, amount);

    // A wipe against the untouched source, because a filter is best judged
    // against what it replaced rather than on its own.
    if (split > 0.001) {
        float edge = smoothstep(1.5 / RENDERSIZE.x, 0.0, abs(uv.x - split));
        result = mix(result, original, step(split, uv.x));
        result = mix(result, vec3(1.0), edge * 0.7);
    }

    gl_FragColor = vec4(clamp(result, 0.0, 1.0), 1.0);
}
