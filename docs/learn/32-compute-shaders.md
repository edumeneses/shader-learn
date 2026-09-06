---
layout: default
title: "Unit 32: Compute shaders, workgroups, images, and buffers"
description: "A different machine wearing the same language. No pipeline, no fragments, and the ability to write anywhere, which is the thing every other unit in this course has worked around."
parent: Units
nav_order: 35
unit: "32"
permalink: /learn/32-compute-shaders.html
score_version: "3.8.2"
reading_time: "15 min"
practice_time: "35 min"
glsl: "GLSL ES 3.00"
---

# Unit 32: Compute shaders, workgroups, images, and buffers

{% include unit_meta.html %}

> **Before this unit** read [Unit 19]({{ site.baseurl }}/learn/19-state-in-a-texture.html) and [Unit 28]({{ site.baseurl }}/learn/28-the-pipeline.html).
>
> **You will need** *ossia score*, or this repository's renderer. **This unit's shader does not run in the browser**, and the reason is the unit.
>
> **You will build** a shader that writes wherever it likes.

## Why this matters

Three units have run into the same wall from different directions. [Unit 19]({{ site.baseurl }}/learn/19-state-in-a-texture.html) could simulate a field and not a particle system. [Unit 22]({{ site.baseurl }}/learn/22-grading.html)'s histogram had to sample a scanline and call itself an approximation. [Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html) got round it by inverting the pipeline.

A compute shader removes the wall. It is not part of the graphics pipeline at all: no vertices, no rasteriser, no fragments, and no rule that an invocation writes one predetermined location. It has a grid of invocations, memory shared within a group, atomic operations, and arbitrary reads and writes. Anything that is awkward because it does not fit "one output per pixel" becomes straightforward.

**It also does not exist in WebGL 2.** WebGL 2 is OpenGL ES 3.0, which has no compute stage, so every player on this site is incapable of running one. That is not a gap in this course's tooling; it is the boundary of the platform, and it is worth meeting head on, because it is the first technique here that genuinely does not travel everywhere.

## The idea

**A dispatch, not a draw.** The host says "run this many workgroups" and the hardware runs them. There is nothing on screen unless the shader writes something to an image.

**The hierarchy.** An invocation is one thread. A **workgroup** is a block of invocations, whose size you declare with `layout(local_size_x = 16, local_size_y = 16) in;`. A **dispatch** is a grid of workgroups. Three built-ins tell an invocation where it is: `gl_LocalInvocationID` within its group, `gl_WorkGroupID` among the groups, and `gl_GlobalInvocationID` overall.

**Workgroup size matters and 16 by 16 is a reasonable default.** Hardware executes threads in fixed-size batches, 32 or 64 depending on the vendor, so a workgroup size that is not a multiple of that wastes lanes. 256 invocations, as 16 by 16, divides well on everything.

**Shared memory is the reason workgroups exist.** `shared uint bins[64];` is memory visible to every invocation in the group and to no other group. It is much faster than global memory and it is how invocations cooperate. It is also uninitialised, so a shader that forgets to clear it reads whatever the last group left.

**`barrier()` synchronises a workgroup.** Every invocation in the group waits until all have arrived. Without it, one invocation can read a shared value another has not written yet, and the symptom is a result that changes between runs. **Every invocation in the group must reach the barrier**, so a `barrier()` inside a conditional that only some invocations take is undefined behaviour and a genuinely hard bug.

**Atomics are what make scattering safe.** `atomicAdd(bins[bin], 1u)` increments a counter that hundreds of invocations may be incrementing at once, and guarantees none is lost. Without atomics, concurrent writes to one location are a data race and the result is arbitrary.

**Images, not textures.** A compute shader reads and writes with `imageLoad` and `imageStore` on an `image2D`, declared with its format: `layout(binding = 0, rgba8) uniform readonly image2D inputImage;`. A `sampler2D` is still available for filtered reads, and the difference matters: an image access is a raw texel with no filtering, and a sampler access is filtered and cannot be written.

**A memory barrier is not `barrier()`.** `barrier()` synchronises invocations; `memoryBarrier()` and its relatives make writes visible. Reading an image another invocation wrote needs both.

**In ossia score**, the header is `"MODE": "COMPUTE_SHADER"`, the input block is called `RESOURCES` rather than `INPUTS`, and `PASSES` carries a `LOCAL_SIZE` and an `EXECUTION_MODEL`. `"TYPE": "2D_IMAGE"` with a `TARGET` means "enough workgroups to cover that image", which is a ceiling division; getting it wrong by rounding down leaves a strip of the image never written.

## Build it

This shader computes a luminance histogram with atomics and draws one per workgroup over the image. It cannot run in the player, so the figure is rendered.

{% include figure.html unit="32" name="32-01" alt="A test card overlaid with a mosaic of small white histograms, one per 16 by 16 tile, each showing that tile's tonal distribution" caption="Each tile is one workgroup's histogram of its own 256 pixels, computed with atomicAdd into shared memory. Flat colour bars give a single spike; the frequency wedges give a broad spread; the continuous-tone region varies from tile to tile. Rendered with scripts/render.py, which dispatches the shader on the GPU." %}

{% include shader.html id="32-histogram" height="200" caption="The same shader in the library. The player reports why it cannot run it, and shows the source." %}

1. **Read the two `atomicAdd` lines.** They are the unit. Every other line exists to set them up or to display their result.
2. **Read the `barrier()` calls and ask what each one waits for.** The first waits for the shared bins to be cleared; the second waits for every invocation to have counted its pixel. Remove either in your head and work out what goes wrong.
3. **Look at the mosaic in the figure.** Each tile is one workgroup, and each histogram is of that tile's own 256 pixels. Flat regions give a single tall spike; the wedges give a broad spread.
4. **Note what it is not.** It is not a global histogram, and the shader says so. Shared memory is per workgroup, so combining them needs a second pass reducing through a buffer. One pass gets you this, and this is honest.
5. **Run it yourself**, if you have this repository:

   ```bash
   python3 scripts/render.py library/shaders/32/histogram.fs \
       --out /tmp/hist --formats png --size 900x506
   ```

6. **Load it in *ossia score*.** The `RESOURCES` block becomes inlets exactly as `INPUTS` does, and the process takes a texture and produces one.
7. **Change `LOCAL_SIZE` to 8 by 8 and re-render.** Four times as many tiles, each from a quarter as many pixels, and noticeably noisier histograms. Workgroup size is a real parameter with a visible result.

## What compute is actually for

- **Reductions**: histograms, exposure metering, sums, maxima. Anything where many inputs become few outputs.
- **Particle systems**, with real scattering and no vertex-count limit.
- **Physics**: fluids with proper pressure solves, cloth, rigid bodies, flocking.
- **Sorting**, which enables order-independent transparency and spatial data structures.
- **Image processing that needs neighbourhood cooperation**, where shared memory lets a workgroup load a tile once and reuse it many times instead of every invocation reading nine texels.

Note what is *not* on the list: ordinary per-pixel image effects. A fragment shader is usually as fast or faster for those, because the pipeline around it is optimised for exactly that shape.

## Common mistakes

- **Uninitialised shared memory**, giving results that change every run.
- **A `barrier()` that not every invocation reaches.** Undefined behaviour, and it usually looks like a driver bug.
- **`barrier()` where `memoryBarrier()` was needed**, or the reverse.
- **Non-atomic writes to a shared location**, which is a race with an arbitrary winner.
- **A dispatch size that rounds down**, leaving a strip of the image untouched. Ceiling division, always.
- **A workgroup size that is not a multiple of the hardware's batch size**, wasting lanes.
- **Reaching for compute for an ordinary image effect.** It is not automatically faster and it is more code.
- **Expecting it to work in a browser.** WebGL 2 has no compute. WebGPU does, and it is not GLSL.

## Exercise

Write a compute shader that computes an image's average luminance and uses it to auto-expose the image.

Requirements: a first pass reduces each workgroup's pixels to one value using shared memory and a barrier; a second pass reduces those to a single value in a buffer; and a third applies the exposure. A `float` named `target` sets the luminance to aim for and a `float` named `speed` controls how fast the exposure adapts across frames, which means it needs to persist.

**Success criterion:** pointing the shader at a dark image brightens it and at a bright image darkens it, both settling at roughly `target`, and the adaptation takes about as long as `speed` says regardless of frame rate. If it oscillates, the feedback is too fast, which is [Unit 17]({{ site.baseurl }}/learn/17-time.html)'s frame-rate-independent smoothing arriving in a new place.

## Going further

- [*ossia score*'s compute shaders]({{ site.docs_baseurl }}/processes/compute-shaders.html), including the `RESOURCES` and `EXECUTION_MODEL` keys in full.
- [The OpenGL compute shader wiki](https://www.khronos.org/opengl/wiki/Compute_Shader), for barriers and memory semantics.
- [WebGPU](https://gpuweb.github.io/gpuweb/), which is where compute in a browser will eventually come from, and which is [Unit 33]({{ site.baseurl }}/learn/33-beyond-glsl.html).
