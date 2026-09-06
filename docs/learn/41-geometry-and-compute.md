---
layout: default
title: "Unit 41: Compute, vertex, and 3D scenes inside score"
description: "The three processes that are not a fragment shader on a quad, when each is the right tool, and what it costs to reach for one."
parent: Units
nav_order: 44
unit: "41"
permalink: /learn/41-geometry-and-compute.html
score_version: "3.8.2"
reading_time: "14 min"
practice_time: "35 min"
glsl: "GLSL ES 3.00"
---

# Unit 41: Compute, vertex, and 3D scenes inside score

{% include unit_meta.html %}

> **Before this unit** read [Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html) and [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html).
>
> **You will need** *score* {{ page.score_version }}.
>
> **You will build** an accurate map of when to stop reaching for a fragment shader.

## Why this matters

Thirty-nine units have solved everything with a fragment shader on a full-screen quad, and that is a defensible default: it is portable, it is cheap, and it is what ISF is for.

It is also the wrong tool three times, and this unit is about recognising them. When you need to *scatter*, a fragment shader cannot. When you need real geometry with real depth, raymarching it is expensive and a mesh is nearly free. When you need to reduce many values to few, a fragment shader has no mechanism at all.

*score* has a process for each, and the point of this unit is knowing which question sends you to which.

## The idea

**The VSA Shader process** runs [Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html)'s vertex shaders. Reach for it when the thing you are drawing is naturally a set of *points* rather than a field: particle clouds, point-cloud data, line drawings, anything with tens of thousands of elements. It is the cheapest process in this course, because the fragment stage does nothing.

Its inputs are declared exactly as ISF's are, so [Unit 29]({{ site.baseurl }}/learn/29-isf.html) and [Unit 37]({{ site.baseurl }}/learn/37-parameters-as-inlets.html) apply unchanged: a VSA shader's parameters become inlets and an automation curve drives them.

**The Compute Shader process** runs [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html)'s. Reach for it when the operation is not one-output-per-pixel: reductions, histograms, sorting, physics with real scattering, or image processing where a workgroup can load a tile once and share it. Its resource block is `RESOURCES` rather than `INPUTS` and it needs an explicit `EXECUTION_MODEL`, and otherwise it is a process like any other.

Do not reach for it for ordinary per-pixel effects. The graphics pipeline is optimised for exactly that shape and a fragment shader is usually as fast with less code.

**The Model Display process** applies a shader to loaded geometry rather than to a quad. This is the one most likely to surprise someone who arrived through Shadertoy: a mesh with a vertex and a fragment shader is the ordinary way to render three dimensions, and it is much cheaper than raymarching for anything a mesh can represent.

The rule of thumb: **if the shape exists as a model, load it; if it exists as an equation, march it**. Module G is for the second, and it is not a substitute for the first.

**Textures and arrays move between processes.** *score* has utilities for converting between them: a texture into a pixel array, an array into a texture, an array into geometry. That is how a compute shader's output reaches something that expects a mesh, and how a shader's output reaches [LED mapping]({{ site.docs_baseurl }}/common-practices/13-led-design.html), where the "image" is a strip of physical lights.

**Everything still ends as a texture.** Whichever process produced it, the result goes into the same graph, through the same chain, to the same window. [Unit 38]({{ site.baseurl }}/learn/38-chaining.html) applies to all of them, which means a compute shader's output can be graded by a fragment shader and a VSA cloud can be blurred.

## Build it

1. **Load a VSA shader.** Take [Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html)'s from this course's library. Confirm its inputs became inlets.
2. **Drive one from an automation curve.** No difference from a fragment shader; that is the point.
3. **Put a filter after it.** A blur on a point cloud is a genuinely good look and it is two processes.
4. **Load a compute shader.** [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html)'s histogram. Feed it a video and read its `RESOURCES` against the inlets.
5. **Load a model into Model Display** and apply a shader to it. Compare the cost against a raymarched scene of similar complexity; the difference is usually an order of magnitude.
6. **Now build one chain with all three.** A compute analysis, a VSA cloud, a model, mixed, graded, out. This is what a large patch actually looks like.
7. **Measure it.** [Unit 34]({{ site.baseurl }}/learn/34-cost.html)'s method, on a patch rather than a shader.

## Choosing, in one table

| The question | The tool | Why |
|:-------------|:---------|:----|
| What colour is this pixel? | Fragment shader | The default, and usually right |
| Where does this point go? | VSA shader | The only stage that scatters cheaply |
| Reduce many values to few | Compute shader | Shared memory and atomics |
| Render a mesh someone made | Model Display | Rasterising a triangle beats marching a field |
| Render a shape defined by an equation | Fragment shader, raymarched | No mesh exists to load |
| Simulate a field | Fragment shader, persistent buffer | Gather is enough |
| Simulate particles that collide | Compute shader | Scatter is required |

## What this course did not cover

Being explicit about the edges, since this is the last technical unit.

**Geometry and tessellation shaders** exist in desktop OpenGL, do not exist in WebGL, and are largely superseded. *score*'s pipeline targets OpenGL ES 2.0 as its lowest common denominator, so they are not the route to reach for.

**Mesh shaders** are newer, faster, and not available here.

**Ray tracing hardware** is not exposed through this pipeline.

**Multi-GPU and multi-machine rendering** for large installations is a real subject, and it is a *score* subject rather than a shader one.

**LED and non-rectangular output** is where a lot of this work actually lands. *score*'s LED design page and its four-point video mapping object are the entry points, and the shaders in this course feed both without modification, because a strip of lights is just a very small texture read differently.

## Where the boundaries actually are

Three limits are worth stating plainly, because each one has caught someone.

**A compute shader cannot use derivatives.** No `fwidth`, no `dFdx`, no
`dFdy`, because there is no 2 by 2 fragment quad. Every antialiasing technique
in [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html) is unavailable and
the substitute is either supersampling or knowing the scale analytically.

**A vertex shader cannot read what it wrote.** Each invocation produces one
vertex and cannot see the others. Anything requiring vertices to interact needs
a compute pass first, writing to a buffer the vertex shader then reads.

**A fragment shader cannot know the frame it is part of.** No total, no average,
no count. Every statistic in this course, the histogram in
[Unit 22]({{ site.baseurl }}/learn/22-grading.html), the auto-exposure exercise
in [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html), is a compute
problem wearing a fragment costume.

Each limit has a shape: **gather but not scatter, per-element but not across
elements, per-pixel but not per-frame.** Recognising which one you have hit is
most of choosing the right process.

## Common mistakes

- **Raymarching something that exists as a model.** Expensive, and it will not look better.
- **A compute shader for a per-pixel effect.** More code, no faster.
- **A fragment shader for particles.** [Unit 19]({{ site.baseurl }}/learn/19-state-in-a-texture.html) explained why it cannot work; this unit is where you stop trying.
- **Assuming a VSA shader is expensive because there are many points.** It is the cheapest thing here.
- **Forgetting a compute shader needs its dispatch size right.** Round up, or a strip of the image is never written.
- **Treating these as separate worlds.** They all produce a texture and they all chain.

## Exercise

Build a patch that uses at least two of the three processes in this unit and one fragment shader, in one chain, driven by a single set of parameters.

Requirements: every parameter is role-named; the chain ends at a window; the whole thing holds frame rate at your output resolution; and you can say, for each process, why it is that process and not a fragment shader.

**Success criterion:** the justification survives someone asking. If any process could have been a fragment shader on a quad with less code, it should have been.

## Going further

- [Vertex Shader Art in *score*]({{ site.docs_baseurl }}/processes/vertex-shader-art.html).
- [Compute shaders in *score*]({{ site.docs_baseurl }}/processes/compute-shaders.html).
- [LED design]({{ site.docs_baseurl }}/common-practices/13-led-design.html), for output that is not a screen.
