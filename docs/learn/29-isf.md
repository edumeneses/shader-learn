---
layout: default
title: "Unit 29: The Interactive Shader Format in full"
description: "The JSON header, every input type, passes and their sizing, imported images, and the automatic uniforms. Everything the course has been using without explaining."
parent: Units
nav_order: 32
unit: "29"
permalink: /learn/29-isf.html
score_version: "3.8.2"
reading_time: "15 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 29: The Interactive Shader Format in full

{% include unit_meta.html %}

> **Before this unit** read [Unit 28]({{ site.baseurl }}/learn/28-the-pipeline.html).
>
> **You will need** the player below and, if you have it, *ossia score*.
>
> **You will build** a complete reading of the format every shader in this course is written in.

## Why this matters

Every shader you have read has had a JSON header, and every unit has told you only as much about it as that unit needed. This is the unit that reads it properly.

The reason the format matters more than a file format usually does is that **the header is the interface**. It is what makes a shader a *process* rather than a source file: a host reads it and builds a control panel, an inlet list, or a patch node, with no separate configuration and nothing to register. Drop an ISF file into *ossia score*'s library and it becomes a process with typed, ranged, labelled inlets that an automation curve or an OSC message can drive.

That is a small idea with a large consequence: a shader written to this format is performable, and one written to Shadertoy's conventions is not.

## The idea

**The file is GLSL with a JSON comment at the top.**

```glsl
/*{
  "DESCRIPTION": "...",
  "CREDIT": "...",
  "ISFVSN": "2.0",
  "CATEGORIES": ["Generator"],
  "INPUTS": [ ... ],
  "PASSES": [ ... ]
}*/
```

The comment does not have to be the first thing in the file, which matters because shaders exported from other tools often carry a licence banner above it.

**The input types, all of them.**

| Type | Becomes | Notes |
|:-----|:--------|:------|
| `bool` | a checkbox | `DEFAULT` true or false |
| `long` | a menu | `VALUES` plus matching `LABELS` |
| `float` | a slider | `MIN`, `MAX`, `DEFAULT` |
| `point2D` | an XY control | `MIN`, `MAX`, `DEFAULT` as two-element arrays |
| `color` | a colour picker | a `vec4`, RGBA |
| `image` | a texture inlet | read with the `IMG_` accessors |
| `audio` | a waveform texture | one row, x is time |
| `audioFFT` | a spectrum texture | one row, x is frequency |
| `event` | a momentary trigger | true for one frame |

Every one carries a `LABEL`, which is what a reader sees, separately from its `NAME`, which is what the code uses. Use both: a name that reads well in code rarely reads well in a panel.

**What *score* does with them, verified.** Reading the documents *score* ships, the rule is exact: **every input becomes an inlet, in declaration order.** An `image` input becomes a **texture inlet**; every other type becomes a **value inlet** carrying the header's `DEFAULT` as its initial value and its `MIN` and `MAX` as its domain. The inlet is also exposed for OSC under a **lower-cased** version of the name, so an input called `blurAmount` is reachable at `bluramount`.

**A float with no `MIN` and `MAX` gets a domain of 0 to 0.** This is not a hypothetical: `led-with-shaders.score`, which ships with *score*'s own documentation, declares `blurAmount` with no range, and the inlet *score* built for it has `Min: 0.0, Max: 0.0`. An automation curve mapped onto that inlet can only ever produce zero. **Always give a float a range.**

**The automatic uniforms**, supplied by the host and available with no declaration: `RENDERSIZE`, `TIME`, `TIMEDELTA`, `DATE`, `FRAMEINDEX`, `PASSINDEX`, and the coordinate `isf_FragNormCoord`.

**Never use `gl_FragCoord`.** *ossia score*'s documentation is explicit about why: the pipeline can run on OpenGL, Vulkan, Metal, or Direct3D, and they do not agree on which way the y axis runs. `isf_FragNormCoord` is the host's guarantee, and a shader that reaches past it is a shader that will be upside down on someone else's machine.

**Images are read through accessors, not `texture`.** `IMG_NORM_PIXEL(img, uv)` takes a normalised coordinate, `IMG_PIXEL(img, xy)` takes pixels, `IMG_THIS_NORM_PIXEL(img)` reads at this fragment, and `IMG_SIZE(img)` gives the dimensions. They exist for the same reason: the host may hand you a texture whose origin or coordinate convention differs from what you expect, and the accessor absorbs it.

**`PASSES` is a list of draws per frame.**

- `TARGET` names a buffer this pass writes; a pass with no target draws to the output.
- `PERSISTENT` keeps that buffer to the next frame, which is memory.
- `FLOAT` makes it a float buffer, which anything accumulating needs.
- `WIDTH` and `HEIGHT` are expressions in `$WIDTH` and `$HEIGHT`, so a buffer can be a fraction of the output without knowing its size.

Several passes may name the same target, which is how a simulation takes several steps in one frame.

**`IMPORTED` loads images by path**, keyed by name, available through the same accessors. It is the one part of the format this course avoids, because a shader that imports is a shader that no longer travels as one file.

**`ISFVSN`** declares the version. `"2.0"` is current. Version 1 shaders exist and mostly still work; the differences are in the input spellings.

**The extensions this course uses.** `"MODE": "VERTEX_SHADER_ART"` and `"MODE": "COMPUTE_SHADER"` are *ossia score*'s, not part of the ISF specification, and they are covered in [Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html) and [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html).

## Build it

{% include shader.html id="29-every-input" height="460" pointer="anchor" caption="One shader with every input type and three passes: a quarter-resolution buffer, a persistent float buffer driven by audio, and the display. Press the source button and read the header against the panel." %}

1. **Open the source and put it beside the parameter panel.** Every row in the panel is one entry in `INPUTS`, in order. That correspondence is the entire value of the format.
2. **Find the `LABEL` and `NAME` of each.** The panel shows both, because this course's player prints the code name in grey beside the label. In *ossia score* you will see the label.
3. **Look at the top-right quadrant.** That is the quarter-resolution pass, and it is visibly softer. Its size came from `"WIDTH": "$WIDTH/4"`, with the shader never knowing the output resolution.
4. **Look at the bottom-left quadrant.** A persistent float buffer, fading, driven by the audio input. Change the audio source to your microphone in the player's toolbar and speak.
5. **Turn the bool off.** It desaturates. Change the long. Move the point2D. Each is one line in the code and one row in the panel.
6. **Read how the audioFFT input is sampled.** A one-row texture, x running over frequency. The shader takes the maximum of the first eight bands rather than one band, which is far steadier and is the usual thing to do.
7. **Take the file to *ossia score*.** Drop it in and count the inlets against the `INPUTS` array. The panel you have been using and the inlets *score* builds come from the same eight lines of JSON.

## Look at these

The [ISF reference]({{ site.isf_baseurl }}/) is short and is the authority. The [ISF test collection]({{ site.isf_baseurl }}/) is a set of shaders that exist to exercise particular header features, and reading a few is the fastest way to see the format's corners.

{% include toy.html id="XsBSDR" title="ISF-compatible generators" by="Vidvox" note="The library that ships with ossia score is this collection; several hundred readable examples." %}

## Common mistakes

- **A trailing comma in the header.** The JSON fails to parse and the shader has no inputs at all, with no useful message. This is the most common ISF error by a wide margin: if your controls vanish, check the header before the code.
- **A `NAME` that is a GLSL keyword or built-in.** `flat`, `sample`, `round`, `layout`, `reflect`. The uniform will not compile, and the driver's error points at the line after it. This course's toolchain refuses these at parse time by name, having been bitten five times.
- **A float input with no `MIN` and `MAX`.** Its inlet's domain is 0 to 0 and an automation curve on it produces nothing. Verified in a shipped example, above.
- **`MIN` and `MAX` as numbers on a `point2D`.** They must be two-element arrays.
- **`VALUES` and `LABELS` of different lengths** on a `long`, giving a menu with missing or wrong entries.
- **`PERSISTENT` without `FLOAT`.** Trails stick at low values and never fade. [Unit 18]({{ site.baseurl }}/learn/18-feedback.html).
- **Using `gl_FragCoord`.** Correct on your machine, upside down on a third of the others.
- **Assuming every host implements every key.** `IMPORTED` and some sizing expressions vary. Test in the host you intend to ship in.

## Exercise

Take any shader you have written and give it a complete, production-quality header.

Requirements: a `DESCRIPTION` that says what it does in one sentence; a `CREDIT` naming anything you borrowed; `CATEGORIES` that would let someone find it; every parameter with a `LABEL` distinct from its `NAME`, and a `MIN` and `MAX` that cannot produce a broken picture at either end; and at least one `long` with `LABELS`.

Then load it in *ossia score* and drive one inlet from an automation curve.

**Success criterion:** a reader who has never seen the shader can produce three visibly different, non-broken pictures using only the panel, and the automation curve in *score* moves the same parameter your slider moved. If any slider produces a black or white frame at one end, its range is wrong, and ranges are the part of a header that most repays care.

## Going further

- [The ISF specification]({{ site.isf_baseurl }}/), which is short.
- [*ossia score*'s ISF process]({{ site.docs_baseurl }}/processes/shaders.html).
- [Unit 30]({{ site.baseurl }}/learn/30-porting.html), next, on getting other people's shaders into this format.
