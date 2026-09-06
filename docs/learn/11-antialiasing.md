---
layout: default
title: "Unit 11: Antialiasing a field with fwidth"
description: "fwidth fixes an edge for almost nothing and cannot fix detail finer than a pixel. This unit shows both halves against a supersampled ground truth, and says what to do about the half it cannot fix."
parent: Units
nav_order: 11
unit: "11"
permalink: /learn/11-antialiasing.html
reading_time: "12 min"
practice_time: "20 min"
glsl: "GLSL ES 3.00"
---

# Unit 11: Antialiasing a field with fwidth

{% include unit_meta.html %}

> **Before this unit** read [Unit 06]({{ site.baseurl }}/learn/06-step-and-smoothstep.html) and [Unit 07]({{ site.baseurl }}/learn/07-distance-fields-2d.html).
>
> **You will need** the player below, and to look at it while it moves.
>
> **You will build** the rule this course applies to every edge, and an honest account of where the rule stops working.

## Why this matters

[Unit 06]({{ site.baseurl }}/learn/06-step-and-smoothstep.html) introduced `fwidth` as the correct width for an edge. This unit makes it a rule and then tests it to destruction, because the most useful thing to know about a technique is where it fails.

`fwidth` fixes edges, and only edges. It cannot fix a pattern that is finer than a pixel, because it works from one sample and there is no information in one sample about what happened between samples. That distinction, between an **edge** you can measure and **detail** you cannot, is the whole of practical antialiasing, and it decides whether you reach for a derivative or for more samples.

Getting this wrong is expensive in a way that is easy to miss. A shader that shimmers in a browser window will shimmer far worse on a projector, in a dome, or on any surface where a viewer's eye can travel across the image, and by the time you find out you are usually on site.

## The idea

**Aliasing is undersampling.** Your function is evaluated once per pixel, at its centre. Anything in the signal that varies faster than the sample spacing cannot be reconstructed and reappears as a lower frequency: moiré where the pattern is fine, a staircase where the edge is straight, and crawling where either one moves. It is not a rendering artefact; it is arithmetic, and it happens to every sampled system.

**Motion is what makes it unacceptable.** A still image with a hard edge looks acceptable more often than not. The same edge in motion has pixels flipping between two colours as it crosses them, and the eye is extremely good at seeing that. Test antialiasing while the picture is moving, always.

**The rule for edges.**

```glsl
float d = field(p);
float w = fwidth(d);
float coverage = smoothstep(w, -w, d);
```

`fwidth(d)` is `abs(dFdx(d)) + abs(dFdy(d))`, computed by the hardware from a 2 by 2 block of neighbouring pixels. It is how much the field changes across one pixel, which is exactly the width the transition should occupy. The result is resolution independent, zoom independent, and costs a handful of instructions.

**Why it works on a distance field in particular.** For an exact field, `fwidth(d)` is close to the pixel size in field units, and `d` is a linear ramp across the edge, so `d / fwidth(d)` is approximately the signed distance in pixels. That is precisely the quantity a coverage estimate wants. Every step of the argument depends on the field being close to exact, which is why [Unit 07]({{ site.baseurl }}/learn/07-distance-fields-2d.html) made such a point of it.

**Where it stops.**

- **Detail finer than a pixel.** A zone plate, a fine grid, a fan of rays converging to a point. One sample cannot see the pattern, so no width derived from that sample will help.
- **Corners and discontinuities.** `fwidth` is a difference between neighbours, so at a corner it over-estimates and the geometry goes soft for a pixel or two.
- **Fields that are not distances.** `fwidth` of a thresholded value is zero almost everywhere and enormous at the edge.
- **Vertex and compute shaders.** Derivatives do not exist there. [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html) returns to this.

**What to do about detail: fade it, do not draw it.** A line thinner than a pixel is genuinely partially covering that pixel, so the correct answer is to draw it at reduced opacity rather than at full opacity somewhere and nowhere elsewhere. Compute the feature's width in pixels and multiply the coverage by `clamp(widthInPixels, 0.0, 1.0)`. This is what good line renderers have always done, it costs two instructions, and it turns a crawling moiré into a clean fade.

The same idea generalises: when a repeating pattern gets too fine, fade towards its average value. That is what a mipmap does for a texture, and it can be done analytically for a procedural pattern, which is where Quilez's filtered-noise articles lead.

**What to do when you must have the detail: take more samples.** Cost is linear in the sample count, and this is the only thing that helps. Offline, do it without hesitation; `scripts/render.py` in this repository renders at two or four times and averages down for exactly this reason. In real time it is usually the last resort, and the host may offer multisampling more cheaply than you can do it yourself.

## Build it

The player renders a zone plate and a fan of converging rays, both chosen because they are hostile, and offers no antialiasing, `fwidth`, and two supersampled ground truths.

{% include shader.html id="11-antialias" height="420" pointer="none" caption="Turn Drift on and leave it on. Aliasing that is tolerable in a still image is intolerable in motion, and motion is the test." %}

1. **Start with no antialiasing and Drift on.** The rosette at the centre of the fan and the rings at the edge of the zone plate both boil. Nothing about this picture is correct.
2. **Switch to `fwidth`.** The rays' edges are now clean where they are wide, near the outside. The centre, where they converge, is no better and is arguably worse: the moiré is now smoothly shaded instead of hard, which does not make it right.
3. **Switch to supersample 4, then 16.** The rosette softens into grey, which is the correct answer: the true average of a pattern finer than a pixel is its mean, and grey is the mean of black and white stripes.
4. **Go back to `fwidth` and turn on Fade thin rays.** The centre goes grey, at one sixteenth of the cost of supersampling. This is the analytic fade, and it is the technique worth taking away from this unit.
5. **Look at the zone plate's outer ring in all four modes.** No method fixes it except supersampling, and even 16 samples only pushes the problem outward. There is a limit, and knowing where it is stops you from optimising the wrong thing.
6. **Raise Detail and repeat.** The point at which each method fails moves inward. Nothing changes qualitatively.
7. **Read the supersampling loop.** Note that it iterates over a fixed 4 by 4 and skips the samples it does not want, rather than looping to a variable bound. That is not style: some WebGL drivers require a compile-time loop bound, and a raymarcher written the natural way will fail in a browser. Module G writes every loop this way.

## Look at these

{% include toy.html id="4tByz3" title="Analytic filtering" by="Inigo Quilez" note="Filtering a procedural pattern by integrating it, which is the far end of the fade in step 4." %}
{% include toy.html id="MdBGzG" title="Filtered checkerboard" by="Inigo Quilez" note="The canonical example: a checkerboard that fades to grey at distance instead of shimmering." %}

Quilez's [filtering articles](https://iquilezles.org/articles/) are the reference for everything beyond `fwidth`.

## Common mistakes

- **Testing antialiasing on a still image.** It will look fine. Move it.
- **Using `fwidth` on a field that is not a distance**, including one that has been through a `max` or a non-uniform scale, where it silently gives the wrong width.
- **Multiplying `fwidth` by a large number to hide shimmer.** That is a blur, and it will not fix the shimmer because the shimmer is not at the edge.
- **Supersampling a shader that is already expensive** without measuring. Four times the samples is four times the cost, and a raymarcher is already the most expensive thing in this course.
- **Assuming the host will handle it.** MSAA antialiases geometry edges, and a full-screen quad has four of those. Everything inside it is your problem.
- **Forgetting that the offline render and the live view differ.** The figures in this course are supersampled and the players are not, so a player can shimmer where the clip above it does not. That is honest and it is worth knowing when you compare them.

## Exercise

Take the polar repetition from [Unit 10]({{ site.baseurl }}/learn/10-repetition.html), which converges to a point at the centre and therefore aliases badly there, and fix it.

Requirements: use `fwidth` for the edges; compute each arm's width in pixels and fade the arm towards the background when that width drops below one; and do it without a single extra sample.

**Success criterion:** with the pattern rotating, the centre resolves to a smooth disc of the average colour rather than a boiling rosette, and the outer arms are as sharp as they were. If the centre goes black instead of grey, you faded the coverage towards the background rather than towards the pattern's average, which is a real distinction and worth a comment in your code.

## Going further

- [Inigo Quilez, filtering procedural textures](https://iquilezles.org/articles/), the whole set.
- [Inigo Quilez, on `fwidth` and derivatives](https://iquilezles.org/articles/distfunctions2d/), for the distance-field case specifically.
- [GLSL ES 3.00 derivative functions](https://registry.khronos.org/OpenGL-Refpages/es3.0/), for what the hardware is specified to compute.
