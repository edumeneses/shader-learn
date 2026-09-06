/*{
  "DESCRIPTION": "A luminance histogram computed with atomics, which is the operation a fragment shader cannot perform: every invocation writes to a location it chooses rather than to the one it was given.",
  "CREDIT": "Eduardo Meneses, Learn shader art",
  "ISFVSN": "2.0",
  "MODE": "COMPUTE_SHADER",
  "CATEGORIES": ["Course", "Utility"],
  "RESOURCES": [
    {
      "NAME": "inputImage",
      "TYPE": "IMAGE",
      "ACCESS": "read_only",
      "FORMAT": "RGBA8"
    },
    {
      "NAME": "outputImage",
      "TYPE": "IMAGE",
      "ACCESS": "write_only",
      "FORMAT": "RGBA8",
      "WIDTH": "$WIDTH_inputImage",
      "HEIGHT": "$HEIGHT_inputImage"
    },
    {
      "NAME": "gain",
      "TYPE": "float",
      "LABEL": "Histogram gain",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 8.0
    },
    {
      "NAME": "overlay",
      "TYPE": "float",
      "LABEL": "Overlay amount",
      "DEFAULT": 0.55,
      "MIN": 0.0,
      "MAX": 1.0
    }
  ],
  "PASSES": [
    {
      "LOCAL_SIZE": [16, 16, 1],
      "EXECUTION_MODEL": { "TYPE": "2D_IMAGE", "TARGET": "outputImage" }
    }
  ]
}*/

// The whole point of this shader is the two lines with `atomicAdd` in them.
//
// A fragment shader writes exactly one texel: its own. It can read anywhere and
// write nowhere else, which is why Unit 22's histogram had to sample a scanline
// and call itself an approximation, and why Unit 19 could simulate a field but
// not a particle system.
//
// A compute invocation writes wherever it likes. Two thousand invocations
// wanting to increment the same counter is a data race, and `atomicAdd` is the
// hardware's answer: the increment happens once per caller, in some order, with
// none lost. That single guarantee is what compute buys.

shared uint bins[64];

void main() {
    ivec2 coord = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(inputImage);

    uint local = gl_LocalInvocationIndex;

    // Clear this workgroup's shared bins. Shared memory is per workgroup and is
    // uninitialised, so a shader that forgets this reads whatever the last
    // workgroup left, which looks like a wildly unstable histogram.
    if (local < 64u) {
        bins[local] = 0u;
    }
    barrier();

    vec4 texel = vec4(0.0);
    if (coord.x < size.x && coord.y < size.y) {
        texel = imageLoad(inputImage, coord);
        float luma = dot(texel.rgb, vec3(0.2126, 0.7152, 0.0722));
        uint bin = uint(clamp(luma, 0.0, 0.999) * 64.0);
        // Scatter. This invocation decides which counter to touch.
        atomicAdd(bins[bin], 1u);
    }
    barrier();

    if (coord.x >= size.x || coord.y >= size.y) return;

    // Draw each workgroup's own histogram *inside its own tile*.
    //
    // This is not a global histogram and it is not pretending to be one. A
    // workgroup's shared memory is private to that workgroup, so one pass can
    // only produce one histogram per group; combining them needs a second pass
    // reducing through a buffer, which is a different unit's worth of material.
    //
    // Drawn this way it is honest and it is more informative than a global one
    // would have been: the mosaic shows how the tonal distribution varies
    // across the frame, and each tile is visibly the work of one workgroup.
    vec2 tile = vec2(gl_LocalInvocationID.xy) / vec2(gl_WorkGroupSize.xy);
    uint bin = uint(clamp(tile.x, 0.0, 0.999) * 64.0);
    float groupPixels = float(gl_WorkGroupSize.x * gl_WorkGroupSize.y);
    float height = clamp(float(bins[bin]) / groupPixels * 64.0 * gain, 0.0, 1.0);

    // Row 0 is the top of the image, so the bar grows downward unless the
    // comparison is inverted. Getting this backwards is the compute equivalent
    // of Unit 20's Y flip.
    float fromBottom = 1.0 - tile.y;
    vec3 bar = fromBottom < height ? vec3(0.90, 0.94, 1.00) : vec3(0.03, 0.04, 0.06);

    vec3 colour = mix(texel.rgb, bar, overlay);

    imageStore(outputImage, coord, vec4(colour, 1.0));
}
