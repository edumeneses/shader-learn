---
layout: default
title: "Unit 30: Porting between Shadertoy, glslsandbox, KodeLife, and ISF"
description: "Most published shader art is in a format no host loads. The translation is five lines of defines plus a decision about what the parameters should have been called."
parent: Units
nav_order: 33
unit: "30"
permalink: /learn/30-porting.html
score_version: "3.8.2"
reading_time: "14 min"
practice_time: "35 min"
glsl: "GLSL ES 3.00"
---

# Unit 30: Porting between Shadertoy, glslsandbox, KodeLife, and ISF

{% include unit_meta.html %}

> **Before this unit** read [Unit 29]({{ site.baseurl }}/learn/29-isf.html).
>
> **You will need** a Shadertoy you like, and the player below.
>
> **You will build** a shim you will reuse constantly, and a habit about naming that is the actual point of the unit.

## Why this matters

Almost every shader worth reading is on Shadertoy, and Shadertoy's format loads in exactly one place: Shadertoy. The same is true of glslsandbox, of KodeLife, of TouchDesigner, and of every other environment with its own conventions. The techniques are portable and the files are not.

The mechanical part of the translation is trivial, five `#define`s, and this unit will hand them to you. The part worth a unit is what happens next.

A Shadertoy has no parameters. Every number in it is a constant, tuned by its author, in a file with no interface. Porting it faithfully gives you a shader that does exactly one thing, and if you wanted it to do one thing you could have recorded a video. **The port is where a fixed picture becomes an instrument**, and doing that well is a matter of deciding which constants should have been controls and what they should be called.

## The idea

**The five renames.**

| Shadertoy | ISF |
|:----------|:----|
| `iTime` | `TIME` |
| `iResolution` | `vec3(RENDERSIZE, 1.0)` |
| `iTimeDelta` | `TIMEDELTA` |
| `iFrame` | `FRAMEINDEX` |
| `iDate` | `DATE` |

As `#define`s at the top of the file, they cost nothing and leave the body byte-identical to the original, which is worth a great deal when you come back to compare against an updated version.

**The entry point.** Shadertoy calls `mainImage(out vec4 fragColor, in vec2 fragCoord)` with `fragCoord` in pixels. ISF calls `main()`. One wrapper:

```glsl
void main() {
    mainImage(gl_FragColor, isf_FragNormCoord * RENDERSIZE);
}
```

**`iChannel0` to `iChannel3` become `image` inputs**, read through the `IMG_` accessors instead of `texture`. This is the one rename that is not mechanical, because Shadertoy's channels can also be sound, video, a cubemap, or a buffer, and each needs a different ISF equivalent.

**Buffers become passes.** A Shadertoy with Buffer A is a multi-pass shader, and it maps directly onto `PASSES` with a `TARGET`, usually `PERSISTENT`. [Unit 18]({{ site.baseurl }}/learn/18-feedback.html) is the same machinery.

**`iMouse` is the one that is not a rename**, and it is where this unit's argument lives. `iMouse` is a `vec4`: xy is the current position in pixels, zw carries the click. Mapping it to an ISF input is easy. Mapping it to an input *called* `iMouse` is a mistake, and it is the mistake almost every port makes.

A shader with an input called `mouse` can be driven by a mouse. A shader with an input called `focus`, `origin`, or `target` can be driven by a mouse, an automation curve, an OSC message from a phone, a MIDI fader, a face tracker, or a hand. Nothing about the code changes; only the name does. **That rename is the difference between a shader you can look at and a shader you can perform**, and it takes ten seconds.

This is not a hypothetical failing. `led-with-shaders.score`, which ships with *ossia score*'s own documentation, contains a ported Shadertoy whose inputs are named `iMouse`, `iZoom`, `iSteps`, and `iColor`. The port was faithful and it kept the prefix, the device name, and `iMouse`'s range of 0 to 640 by 480 **in pixels**, so an automation curve driving it has to be drawn in the coordinate space of a window nobody has any more. The shader works. It is harder to play than it needed to be, and the only difference is four names and one range.

**glslsandbox** uses `time`, `resolution`, `mouse`, and `surfacePosition`, and its shaders are usually a single `main`. Fewer renames, same argument.

**KodeLife** uses `time`, `resolution`, `mouse`, and `spectrum`, and its shaders often already have a parameter block, which makes them the easiest to port.

**Check the licence.** Shadertoy's default is Creative Commons Attribution-NonCommercial-ShareAlike unless the author says otherwise, and a great many authors do say otherwise in a comment. NonCommercial is a real constraint for a performance you are paid for. Read the header, credit the author in your `CREDIT` field, and ask if you are unsure.

## Build it

{% include shader.html id="30-ported" height="440" pointer="pointer" caption="A Shadertoy-shaped shader running as ISF. Everything below the shim is written exactly as it would be on Shadertoy, including mainImage and the pixel-space fragCoord; the defines above it are the entire translation." %}

1. **Read the source from the top.** Three `#define`s, then a body that would compile on Shadertoy unchanged, then a four-line `main` that calls it.
2. **Find `iMouse` in the shim.** It is built from an input named `pointer`, in the shape Shadertoy expects. The shader body never learns that its mouse is a parameter.
3. **Drag the canvas**, then move the `pointer` control in the panel. Same input, two drivers, and a third would be an automation curve in *ossia score*.
4. **Look at what became a control.** `rate`, `scale`, `layers`, `hue`, `spread`. In the original these would all have been constants, and choosing them is the judgement this unit is about.
5. **Now do one yourself.** Take a Shadertoy under 100 lines that uses nothing but `iTime` and `iResolution`. Paste it under the shim, add a header, and run it.
6. **Then do the part that matters.** Go through it and find every number that changes the picture. Promote five of them to inputs with real names and ranges. Do not name any of them after a device.
7. **Check what breaks.** Two things usually do: a loop whose bound is now a uniform, which some drivers reject, and a texture channel with no ISF equivalent. Both are in Common mistakes below.

## Look at these

{% include toy.html id="Xds3zN" title="Raymarching primitives" by="Inigo Quilez" note="A good first port: long, readable, and it uses only iTime and iResolution." %}
{% include toy.html id="XsXXDn" title="Creation" by="Danilo Guanabara" note="Twelve lines, and a good test of whether your shim is right, because there is nowhere for a mistake to hide." %}

## Porting out of ISF

The reverse direction is worth knowing because it is how you share work. To put an ISF shader on Shadertoy, invert the shim and replace each input with a constant or a `iMouse`-driven value; to put one in TouchDesigner or Resolume, both read ISF directly, which is one of the format's better arguments.

*ossia score* reads ISF natively, which is why this course chose it. A shader written here needs no port at all to reach a performance.

## Common mistakes

- **A loop bound that was a constant and is now a uniform.** Shadertoy's compiler is permissive; some WebGL drivers are not. Loop to a fixed maximum and `break`.
- **Precision qualifiers.** Shadertoy adds them for you. In ISF the preamble does too, but a shader that declares its own `float` at the top level may need `highp`.
- **`texture` against `IMG_NORM_PIXEL`.** Both work in many hosts and only one is correct.
- **`gl_FragCoord`.** Shadertoy's `fragCoord` is in pixels with the origin at the bottom left, and reaching for `gl_FragCoord` directly instead of using the shim gives a shader that flips on some backends.
- **Porting the constants faithfully.** The most common failure, and it produces a working shader that is useless in performance.
- **Naming an input after a device.** `mouse`, `mic`, `webcam`, `midiCC7`. All of them lock a shader to one driver, and the habit is widespread enough to be in shipped examples.
- **Keeping a pixel range on a ported pointer.** `iMouse` is in pixels on Shadertoy. An inlet ranged 0 to 640 is one nobody can drive sensibly. Normalise it to 0 to 1 and let the shader multiply.
- **Not checking the licence**, and not crediting the author.

## Exercise

Port a Shadertoy of at least fifty lines into ISF, and make it playable.

Requirements: the body below the shim stays as close to the original as you can manage, so the two can be diffed; at least six constants become named inputs with labels and ranges; **no input is named after a device**; the `CREDIT` field names the original author and links to it; and it runs in *ossia score* with its inlets built from the header.

**Success criterion:** you can make the shader do at least three things its author's version could not, using only the panel, and someone else can too. If you find yourself unable to name an input without referring to a mouse, that is the unit's real exercise: what is that point *for* in the picture? Whatever the answer is, that is its name.

## Going further

- [The ISF specification]({{ site.isf_baseurl }}/), for the target format.
- [Shadertoy's uniform list](https://www.shadertoy.com/howto), for the ones this unit did not cover.
- [Unit 37]({{ site.baseurl }}/learn/37-parameters-as-inlets.html), where the parameters you just created become things an automation curve can drive.
