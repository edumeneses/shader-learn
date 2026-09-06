---
layout: default
title: "Unit 33: Beyond GLSL, and what portability actually costs"
description: "SPIR-V, WGSL, HLSL, MSL, and Slang. What each is for, which ones you will meet, and why ossia score can run the same shader on four graphics APIs."
parent: Units
nav_order: 36
unit: "33"
permalink: /learn/33-beyond-glsl.html
reading_time: "13 min"
practice_time: "15 min"
glsl: "GLSL ES 3.00"
---

# Unit 33: Beyond GLSL, and what portability actually costs

{% include unit_meta.html %}

> **Before this unit** read [Unit 28]({{ site.baseurl }}/learn/28-the-pipeline.html).
>
> **You will need** nothing. This is a reading unit.
>
> **You will build** a map of the languages, so that a phrase like "compile to SPIR-V and cross-compile to MSL" stops being noise.

## Why this matters

This course is entirely GLSL, and that is a deliberate narrowing. GLSL is what WebGL accepts, what ISF is defined in, and what *ossia score* takes, so it covers everything the course needs. It is also, in the wider world, one of five languages that do the same job, and the reason there are five is worth understanding.

The practical reason to care: *ossia score* runs on Linux, macOS, and Windows, over OpenGL, Vulkan, Metal, and Direct3D. Metal does not speak GLSL. Direct3D does not speak GLSL. Yet the ISF file you wrote in [Unit 29]({{ site.baseurl }}/learn/29-isf.html) runs on all of them, and knowing how that happens tells you which of its behaviours you can rely on and which are accidents of your machine.

## The idea

**GLSL** is the OpenGL Shading Language, and it comes in incompatible dialects. Desktop GLSL runs from `#version 110` to `#version 460`. **GLSL ES** is the embedded profile, and `#version 300 es` is what WebGL 2 accepts and what this course uses throughout. A desktop GLSL 3.30 shader and a GLSL ES 3.00 shader are close enough to look identical and different enough to fail to compile.

**HLSL** is Microsoft's, for Direct3D, and it is also the language most game engines author in regardless of platform. Same concepts, different spellings: `float4` for `vec4`, `Texture2D` and a separate `SamplerState` instead of a combined `sampler2D`, `mul(m, v)` instead of `m * v`, and matrices that are row-major by default rather than column-major, which is the difference most likely to produce a picture that is wrong in a way you cannot see.

**MSL**, Metal Shading Language, is Apple's, and it is C++14 rather than a C dialect. Shaders are functions with attributes, resources are arguments rather than globals, and the whole thing feels like a different discipline. It is the only way to reach Apple hardware natively.

**SPIR-V** is not a language. It is a binary intermediate representation, the thing Vulkan actually accepts, and it is the hinge the whole modern ecosystem turns on. You compile GLSL or HLSL *to* SPIR-V once, and then either hand it to Vulkan directly or cross-compile it to MSL or HLSL for platforms that need those. `glslangValidator`, which this course's CI runs on every shader, is the reference compiler that produces it.

**WGSL** is WebGPU's, and it is the successor to WebGL's GLSL in the browser. It is Rust-flavoured, deliberately strict, and it has compute, which WebGL 2 does not. It is the reason [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html)'s shader will eventually run in a browser and does not today.

**Slang** is the newest of these: a superset of HLSL with modules, generics, and automatic differentiation, compiling to all of the above. It is what to watch if you are starting a large project now.

## How ossia score runs one file on four APIs

*score* uses Qt's rendering hardware interface, which abstracts over OpenGL, Vulkan, Metal, and Direct3D. Shaders are authored once and translated for whichever backend is in use.

This has one consequence you have already been living with, and it is the reason [Unit 29]({{ site.baseurl }}/learn/29-isf.html) was emphatic about it. The four APIs **do not agree on which way the y axis runs**, or on the depth range, or on where the texture origin is. *score*'s documentation says this outright and tells you not to use `gl_FragCoord` for exactly that reason. `isf_FragNormCoord` is the host's promise that one coordinate means one thing everywhere.

Everything else in this course has been written to that promise. It is why the shaders in `library/shaders/` can be claimed to run in a browser, in an offline renderer, and in *score* without modification: the claim rests on never having reached past the abstraction.

## What portability actually costs

**Precision qualifiers.** ES requires them, desktop GLSL does not. A shader that omits them compiles on a desktop and fails on a phone.

**Loop bounds.** Some drivers require a compile-time constant so they can unroll. This course writes every loop that way, and it is a real constraint rather than a stylistic one.

**Transcendental accuracy.** The spec leaves `sin` and `cos` accuracy to the implementation, which is why [Unit 12]({{ site.baseurl }}/learn/12-hashes.html)'s sine hash gives different numbers on different drivers. Nothing in a shader can fix this; the fix is not to depend on it.

**Integer support.** GLSL ES 3.00 has it; GLSL ES 1.00, which WebGL 1 used, did not. That is why the integer hash in [Unit 12]({{ site.baseurl }}/learn/12-hashes.html) is available to this course and was not available five years ago.

**Extensions.** Anything behind an `#extension` directive is a portability decision, and a shader that needs one is a shader that will not run somewhere.

**The honest summary:** portability is not free and it is cheaper than it looks, *if* you stay inside a defined profile and use the host's abstractions. Every portability problem this course has actually hit, and there have been five, was a name that was legal in GLSL as one driver implemented it and reserved in the specification: `flat`, `sample`, `round`, `layout`, `reflect`. All five compiled on the machine the figures are rendered on. None would have worked in a browser.

## Build it: read a translation

1. **Open any player and press `‹›`.** Tab one is the ISF you wrote. Tab two is the GLSL ES 3.00 it became. That is one translation, and this repository does it in `scripts/isf.py`.
2. **Count what changed.** The header became uniform declarations. `gl_FragColor` became a `#define` onto a declared output. The `IMG_` accessors became texture reads. The body is otherwise untouched.
3. **Now imagine the next translation.** That GLSL ES source goes to `glslangValidator`, becomes SPIR-V, and on a Mac becomes MSL. Three translations between what you typed and what the GPU ran.
4. **Ask what survives all three.** Everything in the specification. Not the accuracy of `sin`, not a name that one driver happened to allow, and not a loop bound the compiler cannot see.

## Common mistakes

- **Assuming your driver's tolerance is the specification.** It is not, and NVIDIA's in particular is generous.
- **Testing on one GPU.** One vendor is not a portability test.
- **Reaching for `gl_FragCoord`** and getting a shader that is upside down on some backends.
- **Depending on transcendental accuracy.**
- **Using an extension without deciding it is worth the loss.**
- **Assuming HLSL matrix conventions match GLSL's.** They do not, and the result is a rotation that is wrong in a way that looks like a maths error.
- **Waiting for WebGPU before learning compute.** It is the same concepts in a different spelling.

## Exercise

No shader. Do this instead.

Take one shader you have written and list every construct in it that is not guaranteed by the GLSL ES 3.00 specification: any transcendental you depend on the exact value of, any loop whose bound is not a compile-time constant, any assumption about texture origin or coordinate direction, any name you are not certain is unreserved.

Then run it through `glslangValidator`, which this repository's `scripts/validate_glsl.py` does, and compare its complaints against your list.

**Success criterion:** the list and the complaints overlap, and where they do not, you can say why. If glslang complains about something you were confident was fine, that is the exercise working: it is the reference compiler, and your driver is not.

## Going further

- [The Khronos SPIR-V registry](https://www.khronos.org/spir/), for the intermediate representation.
- [WGSL specification](https://www.w3.org/TR/WGSL/), for what a browser will run next.
- [Slang](https://shader-slang.org/), for where shader languages appear to be going.
- [Qt's rendering hardware interface](https://doc.qt.io/qt-6/qtgui-index.html), which is how *ossia score* reaches four APIs.
