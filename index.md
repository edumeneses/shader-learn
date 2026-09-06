---
layout: default
title: Learn shader art
nav_order: 0
permalink: /
---

# Learn *shader art*

{{ site.glsl_version }} · *ossia score* {{ site.score_version }}
{: .label .label-purple}

A course in writing shaders as an art form, from the first line of GLSL to a piece you can perform. Forty-seven units, each between ten and fifteen minutes to read, take a reader from colouring a single pixel to a controllable visual instrument running inside *ossia score*.

[Start the course]({{ site.baseurl }}/learn){: .btn .btn-primary }
[Browse the shader library]({{ site.baseurl }}/library){: .btn }

## Every shader on this site is running

The course is about moving images, so it does not describe them. Each technique appears as a live player with every parameter on a control, running in your browser on your own GPU. Drag the canvas, move a slider, and watch what the maths does. Then take the same file, unmodified, and open it in *ossia score*.

{% include shader.html id="00-hello-field" height="340" pointer="focus" caption="A signed distance field to a circle, coloured by a cosine palette. Drag the canvas to move the focus, or open the parameter panel and take the softness to zero to see the edge alias. This is Unit 07 and Unit 11, side by side." %}

That is the whole method of the course. A technique you can only read about is a technique you have not learned.

## What it covers

**Phase 1 — foundations.** Coordinates, colour, palettes, edges; signed distance fields and the operators that combine them; hashes, noise, fractal Brownian motion, domain warping, Voronoi. By the end of it you can make a still image worth printing.

**Phase 2 — techniques.** Time and easing, feedback buffers, simulation with state held in a texture; sampling, convolution, grading, compositing; raymarching, lighting, and materials.

**Phase 3 — formats.** What a shader program actually is, the Interactive Shader Format in full, porting from Shadertoy, vertex shaders, compute shaders, and the languages under GLSL. Then one unit on what a shader costs and how to pay less.

**Phase 4 — *ossia score*.** The graphics pipeline, live coding, parameters as inlets driven by automation and OSC, chaining, video and camera input, audio-reactive work, and a capstone.

Four **Make it work** milestones are interleaved. A milestone introduces nothing new; it assembles what the preceding units taught into something you can show.

## Parameters, not devices

Every shader in this course exposes its numbers as named parameters, and none of them is named after the thing that happens to drive it. A shader takes a `focus`, not a mouse position; a `drive`, not an audio level. The pointer on this page, an OSC message from a phone, an automation curve in a score document, and a hand on a MIDI fader all reach the same input with nothing renamed in between. That convention is what makes a lesson shader usable in a performance rather than only in a browser tab.

## What you need

A browser with WebGL 2, which is every current browser, and a text editor. Phase 4 needs *ossia score* {{ site.score_version }}, which is free and runs on Linux, macOS, and Windows.

**No specific graphics card is required.** The course's own figures are rendered on an RTX 4080, because a figure has to be reproducible and a recording has to be made somewhere; nothing in the course needs that card, and every shader is written to run on integrated graphics. Where a technique is genuinely expensive, the unit says so and gives the cheaper version.

## Licence and status

Unit text, figures, and shader sources are licensed [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). The course is authored independently of the *ossia* project and of Vidvox, and is currently a draft under review.
