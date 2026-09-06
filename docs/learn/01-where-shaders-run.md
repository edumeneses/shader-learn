---
layout: default
title: "Unit 01: Where shaders run, and the four places this course uses"
description: "A shader needs a host to run inside. This unit installs the four hosts the course uses, explains what each one supplies, and names the differences between them that actually bite."
parent: Units
nav_order: 1
unit: "01"
permalink: /learn/01-where-shaders-run.html
score_version: "3.8.2"
reading_time: "12 min"
practice_time: "20 min"
glsl: "GLSL ES 3.00"
---

# Unit 01: Where shaders run, and the four places this course uses

{% include unit_meta.html %}

> **Before this unit** read [Unit 00]({{ site.baseurl }}/learn/00-what-a-shader-is.html).
>
> **You will need** a browser, and about 400 MB of disk for *ossia score*. Nothing else is required until Phase 4, so you can defer the install if you would rather start writing.
>
> **You will build** a working setup: the same shader file opening in the browser, in *score*, and in a text editor, with the reader knowing which of the three to trust when they disagree.

## Why this matters

A shader is not a program you run. It is a program you hand to something else, which compiles it against a driver and calls it for you. That something else, the **host**, decides which uniforms exist, what the coordinate system is, what your output is called, and what version of the language you are allowed to write. Two hosts running the same GPU can reject each other's shaders.

This is the single most common source of wasted hours in shader work. A shader copied from Shadertoy does not compile in *ossia score*, and the error says nothing useful, because the problem is not in the code: it is that Shadertoy supplies `iTime` and a `mainImage` entry point, and *score* supplies `TIME` and a `main`. Unit 30 does that translation properly. This unit is about knowing that the question exists.

The course answers it once and then never mentions it again: **everything here is ISF, in GLSL ES 3.00**. That combination runs unchanged in all four hosts below, which is why it was chosen.

## The idea

**The stack, from the bottom.** Your GPU executes machine code. A **driver** turns a shader language into that machine code. A **graphics API** is how a program talks to the driver: OpenGL, OpenGL ES, Vulkan, Metal on Apple platforms, Direct3D on Windows, WebGL and WebGPU in a browser. A **host application** sits on top of the API, and it is the host that defines what a shader file looks like.

**The language and the format are different things.** GLSL is the language. ISF is a **format**: a GLSL fragment shader with a JSON header describing its inputs, so that a host can build a control panel from the file rather than from a separate configuration. Shadertoy is also a format, an undocumented one, with a fixed entry point and a fixed set of uniforms. Unit 29 covers ISF in full and Unit 30 covers moving between formats.

**GLSL has versions, and they are not compatible.** `#version 120` is the old desktop dialect, where the output was a built-in called `gl_FragColor` and textures were read with `texture2D`. `#version 300 es` is the modern embedded dialect: it declares its own output, reads textures with `texture`, and requires you to state the precision of your floats. Everything in this course is `#version 300 es`, written in ISF's older spelling and translated on the way through, which is why the source you read says `gl_FragColor` and the compiled source in the player's second tab does not.

### The four hosts

**1. This site.** Every player on these pages is WebGL 2, which is OpenGL ES 3.0 with the sharp edges removed. It is the fastest way to see a change, it runs on the reader's own GPU, and it needs nothing installed. Its limitation is that WebGL 2 has no compute stage, so Unit 32's shaders appear here as rendered clips rather than as players.

**2. A text editor.** The shader is a file. Nothing about it requires an IDE, and the whole course is written so that a plain editor is enough. Any editor with GLSL syntax highlighting will do.

**3. *ossia score*.** The host this course is aimed at. It loads ISF files, builds an inlet for every input in the header, and lets those inlets be driven by automation curves, OSC, MIDI, audio analysis, or another process's output. It also live-compiles a shader while the score is playing, which is the fastest editing loop of the four. Phase 4 is entirely about it.

**4. An offline renderer.** The clips in this course are produced by `scripts/render.py`, which runs the same shader headless on a GPU and encodes the frames. You do not need it to take the course. It matters here because it is the reason a figure and the player beside it show the same thing: they are the same source, compiled the same way.

### The differences that actually bite

**The Y axis.** Some APIs put the origin at the bottom left, some at the top left. A shader that reads `gl_FragCoord` directly is upside down on half of them. ISF's answer is `isf_FragNormCoord`, a normalised coordinate the host guarantees, and *ossia score*'s documentation is explicit that you should use it for exactly this reason. Every shader in this course does.

**Precision.** Desktop GLSL lets you omit precision qualifiers; ES does not. A shader that compiles on your machine and fails on someone's phone is usually a missing `precision highp float`. ISF's preamble supplies it, so you will not meet this until you write a shader outside the format.

**Available uniforms.** `TIME` in ISF, `iTime` on Shadertoy, `u_time` in glslsandbox, `time` in KodeLife. None of them is standard. This is why Unit 30 exists.

**Loop bounds.** WebGL requires that a loop's iteration count be known at compile time in some drivers. A raymarcher written with `for (int i = 0; i < steps; i++)`, where `steps` is a uniform, compiles on a desktop and fails in a browser. Module G writes the portable form from the start.

## Build it: get the four talking to each other

1. **Confirm the browser.** If the player in [Unit 00]({{ site.baseurl }}/learn/00-what-a-shader-is.html) ran, you have WebGL 2 and nothing else is needed for Phases 1 to 3.
2. **Download the course's shader library.** Every shader in every unit is a real file. Take them from the repository's `library/shaders/` directory, or from each player's `‹›` button, which shows the exact source and copies it.
3. **Install *ossia score* {{ page.score_version }}.** Get it from [ossia.io](https://ossia.io/score/download.html) for your platform. On Linux it is an AppImage: make it executable and run it. This is the version everything in Phase 4 is verified against.
4. **Find the shaders that ship with it.** *score* includes a large ISF library, courtesy of Vidvox, in its user library panel. Open it and scroll: several hundred working shaders, all of them readable, are a better reference than any tutorial once you can read GLSL.
5. **Load one of this course's shaders.** Drag an `.fs` file onto an interval in *score*, or drop it into the user library folder and pick it from the process list. The inputs declared in its JSON header appear as inlets on the process, with the ranges the header gave them.
6. **Change a number and watch it recompile.** Open the shader's code editor in *score*, edit a constant, and press {% include shortcut.html content="Ctrl+Enter" %}. The running shader is replaced without stopping the score. That loop, not the browser, is where the second half of this course lives.

There is nothing to check into a repository at the end of this unit. The success criterion is that a file you read on this site opens in *score* with its controls already built, and that you have seen it recompile while playing.

## Look at these

{% include toy.html id="XlfGRj" title="Star Nest" by="Pablo Roman Andrioli" note="Widely ported: search for it in ISF, in KodeLife, and in TouchDesigner, and compare the headers rather than the bodies." %}

The [ISF test and tutorial collection]({{ site.isf_baseurl }}/) is worth an hour on its own; it is the closest thing the format has to a specification with examples.

## Common mistakes

- **Pasting Shadertoy code into an ISF host and reading the compile error as a code problem.** It is a format problem. Unit 30.
- **Installing a different *score* version.** The course pins {{ page.score_version }}. Interface positions and process names move between releases, and a screenshot from another version will not match.
- **Editing a shader inside the host and forgetting to save it back to a file.** *score* stores the edited source in the score document, so a live-coded improvement can be trapped inside a `.score` file. Unit 36 covers getting it back out.
- **Assuming your GPU is the problem.** Almost every "it does not work on this machine" in shader work is a language version or a host difference, not hardware. Check the compile log before you check the driver.

## Exercise

Take `hello-field.fs` from [Unit 00]({{ site.baseurl }}/learn/00-what-a-shader-is.html), using the player's `‹›` button to copy it, and open it in three places: a text editor, this site's player, and *ossia score*.

In *score*, set its **Radius** inlet from the inspector rather than from a slider, and confirm the picture changes. Then look at the inlet list and count the inlets against the `INPUTS` array in the file.

**Success criterion:** the number of inlets matches the number of inputs in the JSON header, and you can point at the line in the file that produced any one of them. If an inlet is missing, the header has a syntax error, and the most common one is a trailing comma.

## Going further

- [*ossia score* download and install]({{ site.docs_baseurl }}/getting-started/install.html).
- [The ISF process in *score*]({{ site.docs_baseurl }}/processes/shaders.html), and [live coding]({{ site.docs_baseurl }}/common-practices/8-live-coding.html).
- [ISF quick reference]({{ site.isf_baseurl }}/), for the header keys Unit 29 will spell out.
- [WebGL 2 support tables](https://caniuse.com/webgl2), if a player on this site did not run.
