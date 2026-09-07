---
layout: default
title: "Unit 35: The score graphics pipeline, end to end"
description: "How a texture gets from a process to a window in ossia score, what the render graph does, and why the same ISF file runs on four graphics APIs."
parent: Units
nav_order: 38
unit: "35"
permalink: /learn/35-score-pipeline.html
score_version: "3.8.2"
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 35: The score graphics pipeline, end to end

{% include unit_meta.html %}

> **Before this unit** read [Unit 28]({{ site.baseurl }}/learn/28-the-pipeline.html) and install *ossia score* {{ page.score_version }}.
>
> **You will need** *score*, and one shader from this course's library.
>
> **You will build** the smallest complete patch: a shader, a window, and a texture travelling between them.

## Why this matters

Phase 4 is about putting these shaders somewhere they can be performed, and the first thing to understand is what *score* actually does with one.

The short answer is that a shader is a **process**, processes have texture inlets and outlets, and connecting them builds a graph that *score* renders each frame. That is a small model and it explains almost everything: why a chain works, why a shader can be swapped while playing, why a texture size is not always the window size, and why a shader written for one graphics API runs on another.

It also explains the constraints, and the constraints are where a show goes wrong.

## The idea

**Everything visual is a process on the timeline.** A shader, a video file, a camera, a 3D scene, a mixer: each is a process placed in an interval, each produces a texture, and each may consume one.

**Processes are connected into a render graph.** *score* builds it from the connections in your patch and renders it in a separate thread from the audio and the interface. Each process writes to its own render target, and the graph decides the order. You do not schedule anything; you describe the connections.

**Qt RHI is the abstraction underneath.** *score*'s documentation is explicit: it uses Qt's rendering hardware interface, and it can run over OpenGL ES 2.0, Vulkan, Metal, or Direct3D 11. That is why the ISF file you wrote runs unchanged on Linux, macOS, and Windows, and it is also why [Unit 29]({{ site.baseurl }}/learn/29-isf.html) was insistent about `isf_FragNormCoord`: those four APIs disagree about which way the y axis runs, and the abstraction only holds if you stay inside it.

**A texture reaches a window through an address, not a cable.** This is the part that surprises people. A window is a **device**, like an OSC device or a MIDI device, and a process sends its output to it by addressing its outlet at that device rather than by drawing a connection. The address is `Window:/`, and you can confirm it without opening the application: a `.score` document is JSON, and any document that puts something on screen has that string in it. Once you have seen it once it is obvious; until then it looks like the video has nowhere to go.

**Texture sizes are not the window size.** A process's render target has its own size, and a chain may change it. This is the practical reason [Unit 20]({{ site.baseurl }}/learn/20-sampling.html) insisted on `IMG_SIZE` over `RENDERSIZE` for the input: they are frequently different, and they are almost always equal on the machine you develop on.

**Almost everything is editable during playback.** *score*'s live-coding page says so directly: processes, sounds, and shaders can be added, removed, and altered while the score plays. There is one documented exception, and it is a big one for visual work: **a device cannot be added during playback**. A window is a device. So the window has to exist before you press play, and discovering that in a venue is a bad afternoon.

**The graph runs in its own thread.** Visual work does not block audio, and a heavy shader drops visual frames rather than glitching the sound. That is the right trade and it means the frame rate you see is not a measure of anything but the graphics thread.

## Build it

There are no figures in this unit yet; it is a walkthrough in the application. Every step is short.

1. **Open *score* and make a new document.** You need one interval; drag in the empty scenario editor to make one.
2. **Add a window.** This is a device, so it goes in the device explorer rather than on the timeline. Open the add-device dialog and choose the window protocol. Do this **before** anything else, and remember that you cannot do it during playback.
3. **Open the process library** and find the ISF shader process. It is under the visual processes.
4. **Drop a shader onto the interval.** Take any `.fs` from this course's library, or one of the several hundred Vidvox shaders that ship with *score*.
5. **Look at the process's inlets.** They are the `INPUTS` from the JSON header, with the labels and ranges the header declared. That is [Unit 29]({{ site.baseurl }}/learn/29-isf.html) arriving in the application.
6. **Send its output to the window.** Address the process's texture outlet at the window device. The output appears.
7. **Play, and edit the shader while it plays.** Open the script editor from the node header and change a constant. It recompiles without stopping. That loop is the reason Phase 4 exists.
8. **Look at the process inspector's texture preview.** It shows the live output frame inside the main window, which is the only reliable way to see what a process is producing when the output window is on another screen or a projector.

## What each visual process is for

- **ISF Shader**: what this course writes. A generator with no input, a filter with one, a mixer with several.
- **VSA Shader**: [Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html)'s vertex-stage shaders.
- **Compute Shader**: [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html)'s.
- **Video / Camera / Image**: sources that produce a texture from outside.
- **Model Display**: applies a shader to loaded geometry rather than to a full-screen quad.
- **Window**: the device a texture is sent to.
- **Video Mixer**: an eight-channel mixer with per-channel opacity and blend mode, in the user library under Visuals, ISF Shader, Utility. It is itself an ISF shader, which is worth knowing: everything in [Unit 23]({{ site.baseurl }}/learn/23-compositing.html) is inside it.
- **Video mapping**: a four-point mapping object in the same library, for getting output onto a surface that is not a rectangle.

## Generators, filters, and mixers

A shader's shape is decided entirely by how many `image` inputs it declares, and
it is worth naming the three cases because a patch is built out of them.

**A generator has no image input.** Everything in Phases 1 and 2 is one. In a
patch it is a source: it has an outlet and no texture inlet, so it sits at the
head of a chain.

**A filter has one.** Everything in Module F. It has an inlet and an outlet, so
it goes in the middle, and several in a row is a chain.

**A mixer has several.** The eight-channel video mixer that ships with *score*
is one. It sits where several chains meet.

The useful consequence is that this is not a property you declare anywhere. You
write `"TYPE": "image"` in a header and *score* works out where the process can
go. A shader that takes an optional input, using it if connected and generating
if not, is a legitimate thing to write and it is how several of the library's
utility shaders behave.

## Common mistakes

- **Adding the window device during playback.** It is the one documented exception to live editing, and it fails silently enough to be confusing.
- **Looking for a cable to the window.** It is an address.
- **Assuming the input texture is the output size.** Use `IMG_SIZE`.
- **Judging performance by the interface's responsiveness.** The graph is on another thread.
- **Using `gl_FragCoord`.** Four APIs, four conventions.
- **Building the patch before checking the shader runs.** Load it, see a picture, then wire it.

## Exercise

Build the smallest complete visual patch: one window, one interval, one generator shader from this course, output addressed to the window.

Then, without stopping playback, replace the shader's source with a different one from the library and confirm the output changes.

**Success criterion:** the picture appears in the window, and the swap happens without a gap in playback. If the window shows nothing, check the address before the shader; if the swap stops the score, you stopped it yourself.

## Going further

- [*ossia score*'s graphics pipeline]({{ site.docs_baseurl }}/in-depth/video.html), which is one page and says what this unit expands.
- [Live coding in *score*]({{ site.docs_baseurl }}/common-practices/8-live-coding.html), including the device exception.
- [Video mixing and mapping]({{ site.docs_baseurl }}/common-practices/11-video-mixing-and-mapping.html).
