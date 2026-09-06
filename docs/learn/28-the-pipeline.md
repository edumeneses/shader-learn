---
layout: default
title: "Unit 28: What a shader program actually is"
description: "Stages, pipelines, and the machinery under the one function you have been writing. Nothing here changes what you can do; it changes what you can debug."
parent: Units
nav_order: 31
unit: "28"
permalink: /learn/28-the-pipeline.html
reading_time: "14 min"
practice_time: "15 min"
glsl: "GLSL ES 3.00"
---

# Unit 28: What a shader program actually is

{% include unit_meta.html %}

> **Before this unit** finish Phase 2. This is the first unit of Phase 3.
>
> **You will need** nothing to install. This is a reading unit with one small experiment.
>
> **You will build** an accurate model of the machinery under the function you have been writing.

## Why this matters

Twenty-seven units have treated a shader as one function from a coordinate to a colour. That model is correct and it is deliberately incomplete, and everything from here on depends on the parts it left out.

The gaps show up as questions that have no answer inside the model. Why can a fragment shader not write to another pixel? Why does `fwidth` exist in one stage and not in another? Why does a shader with a loop bound taken from a uniform compile on your machine and fail in a browser? Why does an ISF shader load in *ossia score* and a Shadertoy not? Each answer is somewhere in this unit.

Phase 3 is about formats and stages, and this is the map.

## The idea

**A shader is not a program you run; it is a stage in a pipeline.** The host builds a **program** by compiling and linking one shader for each stage it wants, then issues a **draw call**, and the hardware runs the stages in a fixed order with fixed handoffs.

**The graphics pipeline, in the order things happen.**

1. **Vertex shader.** Runs once per vertex. Its job is to decide where that vertex lands, by writing `gl_Position` in clip space. It can also output anything else it likes for the stages downstream. It cannot see other vertices, and it cannot see pixels because none exist yet.
2. **Primitive assembly.** Vertices are grouped into points, lines, or triangles.
3. **Rasterisation.** Fixed-function hardware, not programmable. Each primitive is turned into the fragments it covers, and every value the vertex shader output is **interpolated** across the primitive. This is where `isf_FragNormCoord` comes from: a vertex shader wrote it at three corners and the rasteriser filled in everything between.
4. **Fragment shader.** Runs once per fragment. Writes a colour. This is the only stage this course has used so far.
5. **Blending and the depth test.** Fixed-function again: the fragment's colour is combined with what is already in the framebuffer according to state the host set.

**A full-screen shader is a trick.** Every shader in Phases 1 and 2 has been a fragment shader run over two triangles, or one large one, covering the screen. There is a vertex shader; ISF supplies it, it does almost nothing, and you have never seen it. Recognising that the "shader" you have been writing is one stage of five explains most of what follows.

**Fragments are processed in 2 by 2 quads.** This is why `fwidth`, `dFdx`, and `dFdy` exist and are almost free: the hardware runs your shader on four neighbouring pixels together and differences the results. It also means a fragment on the edge of a triangle causes its three neighbours to be shaded and discarded, which is why very small triangles are disproportionately expensive.

**A compute shader is not in this pipeline at all.** No vertices, no rasteriser, no fragments. A grid of invocations, a shared memory per workgroup, and the ability to read and write arbitrary memory. It is a different machine wearing the same language, and [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html) is about it.

**Uniforms, attributes, varyings, and buffers** are the four ways data reaches a shader. A **uniform** is the same for every invocation and is set by the host. An **attribute** is per vertex, from a buffer. A **varying**, spelled `out` in the vertex shader and `in` in the fragment shader, is interpolated between them. A **buffer** is arbitrary memory, available to compute and, on some hardware, to other stages.

**Compilation happens on the target machine, in the driver.** There is no portable binary. This is why a shader can compile on your GPU and fail on someone else's, why the error messages differ between vendors, and why an intermediate representation like SPIR-V exists. [Unit 33]({{ site.baseurl }}/learn/33-beyond-glsl.html) picks that up.

## Build it: find the vertex shader you have been using

There is nothing to install, and one thing worth doing.

1. **Open any player on this site and press `‹›`.** The second tab is the compiled GLSL: what the browser actually runs.
2. **Notice what is not there.** The vertex shader is not shown, because it is generated. It is in this repository at the top of `scripts/isf.py`, as `VERTEX_PREAMBLE`, and it is nine lines: it takes a position, writes `gl_Position`, and hands the fragment stage a normalised coordinate.
3. **Read those nine lines.** Every picture in Phases 1 and 2 came through them.
4. **Now open [Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html)'s player** and press `‹›` there. That shader has no fragment stage of its own: the roles are inverted, and the fragment shader is the generated one.
5. **Count the stages in each.** Two in both cases. Which one you write is a choice about what the shader is for.

## The other pipelines

**The compute pipeline.** One stage. Dispatch a grid of workgroups, each with shared memory and synchronisation. Used for anything that is not naturally per-pixel: physics, sorting, histograms, image processing that needs to scatter.

**Mesh shading**, on recent hardware, replaces the vertex and geometry stages with two compute-like stages that generate geometry directly. It is not available in WebGL or in *ossia score*'s pipeline, and it is worth knowing the name.

**Tessellation and geometry shaders** sit between vertex and fragment and can create geometry. Both exist in desktop OpenGL, neither exists in WebGL 2, and both are largely superseded by compute and mesh shading.

**The ray tracing pipeline**, on hardware with dedicated units, has its own stage set entirely. Not available anywhere this course reaches.

## Common mistakes

- **Thinking a fragment shader could write elsewhere if only you knew the trick.** It cannot. The pipeline forbids it, which is [Unit 19]({{ site.baseurl }}/learn/19-state-in-a-texture.html)'s whole limitation.
- **Expecting `fwidth` in a vertex or compute shader.** It comes from the 2 by 2 quad, which exists only in the fragment stage.
- **A loop bound from a uniform.** Some drivers require it to be a compile-time constant so they can unroll. Compiles here, fails there.
- **Assuming a compiled shader is portable.** It is compiled by the driver, on the machine, every time.
- **Thinking the vertex shader is optional.** It is generated for you, and it is still there.
- **Reading a compile error's line number against your own file.** It is against the translated source, which is why the players show it and why this repository's renderer prints it numbered on failure.

## Exercise

No new shader. Do this instead.

Take any shader from Phase 1 and write down, in one sentence each, what happens to it at each of the five pipeline stages: what the vertex shader does, what the rasteriser interpolates, what the fragment shader receives, what it writes, and what blending does with the result.

Then answer three questions from what you wrote. How many times does the vertex shader run for one frame of that shader? How many times does the fragment shader run at 1920 by 1080? And which of the two would you have to change to make the shape appear on a rotating cube rather than on the whole screen?

**Success criterion:** your answers are three, about two million, and the vertex shader. If the last one surprised you, [Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html) is where it becomes useful.

## Going further

- [The OpenGL rendering pipeline](https://www.khronos.org/opengl/wiki/Rendering_Pipeline_Overview), the reference description.
- [Learn OpenGL](https://learnopengl.com/Getting-started/Hello-Triangle), for the host side of what this unit describes.
- [*ossia score*'s graphics pipeline]({{ site.docs_baseurl }}/common-practices/11-video-mixing.html), which is [Unit 35]({{ site.baseurl }}/learn/35-score-pipeline.html).
