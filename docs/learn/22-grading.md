---
layout: default
title: "Unit 22: Colour grading, curves, and lookup tables"
description: "Exposure, white balance, contrast, lift/gamma/gain, saturation, and a tone curve, in that order and in the right space. The order is not a preference; each step assumes the last."
parent: Units
nav_order: 24
unit: "22"
permalink: /learn/22-grading.html
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 22: Colour grading, curves, and lookup tables

{% include unit_meta.html %}

> **Before this unit** read [Unit 04]({{ site.baseurl }}/learn/04-colour.html).
>
> **You will need** the player below, with the compare wipe on.
>
> **You will build** a grading chain in the order a colourist works, and an understanding of why the last step decides whether the others had room.

## Why this matters

Grading is the difference between output that looks like a render and output that looks like a picture. It is also the part of a visual pipeline most often done by accident: a multiply here, a `pow` there, a saturation boost at the end, and the result is muddy in a way nobody can locate.

There is a standard order, it comes from film and it is worth following. Each step assumes the one before it, and doing them out of order produces artefacts that look like bugs in unrelated code. In particular, the **tone curve goes last**, and without one every operation before it is fighting a hard clip at 1.

For live visual work there is a second reason. A grade is where a piece is matched to a room: the same shader looks completely different on a laptop, on a projector at four hundred lumens, and inside a dome. Exposing a grade as parameters means you can fix that during a technical rehearsal instead of editing shader code with an audience waiting.

## The idea

**Do all of it in linear light.** [Unit 04]({{ site.baseurl }}/learn/04-colour.html)'s rule: convert when scaling or combining, and a grade is nothing but scaling. Convert once at the top, grade, convert once at the bottom.

**Exposure, in stops.** `colour *= exp2(stops)`. One stop is a doubling, which is the unit anyone who has held a camera already thinks in, and it is more useful than a linear multiplier because equal steps look equal.

**White balance.** Correctly, a chromatic adaptation matrix. In practice, scale red up and blue down for warmer, and the reverse for cooler, with a second control for green against magenta. The cheap version is what most live tools do and it is one line.

**Contrast about a pivot.** `pivot + (c - pivot) * contrast`. The pivot matters: scaling about zero darkens everything as contrast rises, so the picture gets contrastier *and* dimmer and you fight it with exposure. **0.18 is middle grey** and is the default in every grading tool for that reason.

**Lift, gamma, gain: the three-way.** Lift adds to the shadows, gain scales the highlights, gamma bends the middle without moving either end. Together they are the classic colour-balance control, and their value is that each one targets a different part of the range, so you can warm the shadows without warming the highlights.

**Saturation, against luminance.** `mix(vec3(luma), colour, saturation)`, with `luma` the weighted dot product rather than the channel average. Values above 1 oversaturate, and values below 0 invert the hue, which is occasionally useful.

**The tone curve, last, and it is the one that matters.** Without it, anything above 1 is clipped: a highlight that the grade pushed to 1.4 becomes a flat white hole with no shape. A tone curve rolls it off instead.

- **Reinhard**, `c / (1 + c)`, is one operation and desaturates the highlights.
- **A filmic curve** keeps more contrast in the mid tones and rolls off longer. Narkowicz's ACES fit is five constants and one line, and it is what most real-time work ships.

**Lookup tables are the production answer.** A 3D LUT stores a colour for every input colour, so an artist can grade in a tool they know and export a cube that the shader applies with one texture read. In a fragment shader the standard trick is to unroll the cube into a 2D strip and do two lookups with a manual interpolation between slices. This course does not implement one, because it needs an asset and the whole point of the course's shaders is that they travel as one file; the technique is worth knowing exists.

## Build it

{% include shader.html id="22-grading" height="460" pointer="none" caption="The compare wipe is at the middle by default: graded on the left, untouched on the right. The strip along the bottom is a luminance histogram of the graded image." %}

1. **Raise Exposure by one stop.** The whole image gets brighter, and the highlights clip: the white bar and the bright end of the ramp become the same flat white. Watch the histogram pile up against its right edge.
2. **With exposure still up, set the tone curve to filmic.** The pile-up resolves into a roll-off and the bright end of the ramp gets its shape back. This is the single most useful thing in the unit.
3. **Compare Reinhard and filmic at the same exposure.** Reinhard desaturates as it compresses, so bright colours drift towards white; filmic holds them longer. Both are correct and they are different looks.
4. **Set contrast to 2 and watch the pivot.** Move the pivot to 0.5 and the picture darkens as contrast rises; move it back to 0.18 and it does not. That is the whole argument for the pivot control.
5. **Tint Lift towards blue and Gain towards orange.** Cold shadows, warm highlights, which is the most reused grade in cinema. Note that the three pickers default to mid grey and are read as offsets, so an untouched picker changes nothing.
6. **Take Saturation to 0 and look at the colour bars.** They become greys, and they are *not* all the same grey: the weighted luminance makes green much lighter than blue, which is correct and is what an unweighted average would have got wrong.
7. **Take Saturation to 2.5.** The bars clip and the continuous-tone region posterises. Oversaturation destroys detail, which is easy to forget when the picture is getting more colourful.
8. **Read the histogram's caveat in the source.** It samples along a scanline rather than the whole frame, because a fragment shader cannot accumulate across pixels. A true histogram needs a compute pass, which is [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html).

## Look at these

{% include toy.html id="lslGzl" title="Filmic tone mapping comparison" by="Shadertoy community" note="Reinhard, Uncharted 2, and ACES side by side on the same source." %}
{% include toy.html id="XljGzV" title="LUT application" by="Shadertoy community" note="The unrolled-cube trick, with the manual slice interpolation written out." %}

Krzysztof Narkowicz's [ACES filmic curve](https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/) is the source of the fit this unit uses.

## Common mistakes

- **Grading in code space.** Everything is subtly wrong and nothing is obviously wrong, which is the worst kind of bug.
- **No tone curve.** Clipped highlights with no shape, and every grading move fighting the clip.
- **Contrast without a pivot**, so the picture darkens as it gets contrastier.
- **Saturation from the channel average.** Green and blue come out equally dark, which they are not.
- **Grading before the effect rather than after.** A grade belongs at the end of a chain, on the finished picture; grading an input and then compositing means grading each layer separately and matching them by hand.
- **A LUT applied without slice interpolation**, giving visible banding between the cube's levels.
- **Grading on the monitor you authored on** and not checking on the output device. This is the one that costs a show.

## Exercise

Add a vignette and a film grain to this unit's grading chain, and put them in the right places.

Requirements: the vignette must be applied in linear light, before the tone curve, because it models light falling off rather than a darkening of the image; the grain must be applied after the conversion back, because it models the display's noise floor and not the scene's; a `float` controls each; and both must be resolution independent.

**Success criterion:** with the tone curve on, raising the vignette darkens the corners without crushing them to a flat black, and the grain is the same visual size at 800 pixels wide and at 4000. If the vignette crushes, it is being applied after the curve; if the grain changes size with resolution, it is indexed by a normalised coordinate rather than by a pixel one.

## Going further

- [Narkowicz, ACES filmic tone mapping curve](https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/), the one-line fit.
- [Timothy Lottes, on tone mapping](https://gpuopen.com/), for the parameterised family.
- [The ASC CDL specification](https://en.wikipedia.org/wiki/ASC_CDL), which is lift, gamma, gain formalised into an interchange format.
