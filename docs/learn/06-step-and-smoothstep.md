---
layout: default
title: "Unit 06: step, smoothstep, and the anatomy of an edge"
description: "Four ways to turn a number into a coverage value, magnified so the difference is visible. The last one is a preview of the only correct answer, and the unit says why the other three keep being used anyway."
parent: Units
nav_order: 6
unit: "06"
permalink: /learn/06-step-and-smoothstep.html
reading_time: "12 min"
practice_time: "20 min"
glsl: "GLSL ES 3.00"
---

# Unit 06: step, smoothstep, and the anatomy of an edge

{% include unit_meta.html %}

> **Before this unit** read [Unit 03]({{ site.baseurl }}/learn/03-coordinates.html).
>
> **You will need** the player below. Look at it closely; the whole unit is at the scale of a few pixels.
>
> **You will build** the habit of asking, every time you draw an edge, how wide it should be and in what units.

## Why this matters

Everything visible in a shader is an edge. A shape is where a value crosses a threshold. A ring is where it crosses two. A pattern is a great many of them. So the operation that turns a continuous number into "in" or "out" is the most-used operation in the whole field, and it is the one people spend the least time thinking about.

`step` is the obvious answer and it is almost always the wrong one, because it produces a jagged edge that also flickers when it moves. `smoothstep` is the usual fix and it is only half a fix, because it needs to be told how wide the transition should be and the number people pick is a guess that stops being right at another resolution.

This unit is a careful look at four answers, magnified enough to see. [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html) makes the fourth one a rule.

## The idea

**Coverage is a number from 0 to 1**, and it is the thing you actually want. Not "is this pixel inside" but "how much of this pixel is inside". A pixel is a little square with area, and an edge crossing it covers some fraction. Get that fraction approximately right and the picture looks smooth; answer only 0 or 1 and it staircases.

**`step(edge, x)`** returns 0 below the threshold and 1 above it, with nothing in between. Coverage is 0 or 1 and never 0.4. Use it when the value being tested is already discrete, or when you genuinely want a hard boundary that will never move, and expect it to alias otherwise.

Note the argument order, which catches everyone: `step(edge, x)` is 1 when `x >= edge`. It reads backwards from the comparison you have in your head.

**A linear ramp**, `clamp(0.5 - d / w, 0.0, 1.0)`, is the honest approximation. Coverage really is close to linear in the distance for a straight edge crossing a square pixel. It is better than `step` and it has corners: the slope changes abruptly where the ramp meets 0 and 1, and on a slowly moving edge you can see those corners travel.

**`smoothstep(a, b, x)`** is a cubic with zero slope at both ends, so it has no corners. Note that it interpolates from `a` to `b`, so an edge where "inside" means a negative distance is written `smoothstep(w, -w, d)`, with the larger value first. That inversion is idiomatic and is worth recognising on sight.

Its problem is `w`. Written as a constant, `w` is in field units, so the edge is a fixed fraction of the picture. Zoom in and the edge becomes a wide blurry band; render at four times the resolution and it becomes four times as many blurry pixels. It looks correct at exactly the scale you tuned it.

**`fwidth(d)`** answers the question the other three were guessing at: how much does this value change between this pixel and the one next to it. The hardware computes it by running your shader on 2 by 2 blocks of pixels and differencing the results, which is why it is nearly free and why it is only available in a fragment shader. `smoothstep(fwidth(d), -fwidth(d), d)` therefore gives an edge that is about one pixel wide at any resolution, any zoom, and any distance.

That is the correct answer, and this course uses it everywhere, with one caveat it is honest to state now: `fwidth` is a difference between neighbours, so where a field changes abruptly, at a corner or across a discontinuity, it over-estimates and the edge gets soft in a small region. There are fixes and they cost more than the problem is usually worth.

**Why the other three survive.** `step` is one instruction. A fixed `smoothstep` is deliberate art direction when you want a specific glow. And `fwidth` is unavailable in a vertex or compute shader, so the far side of Module H has to guess like everybody else.

## Build it

The player draws one rounded rectangle, tilted so no edge is axis-aligned, with a magnifier that snaps onto the nearest edge and shows it at up to forty-eight times.

{% include shader.html id="06-edge" height="400" pointer="focus" caption="Drag the canvas to move the magnifier; it snaps to the nearest edge, so it always has something to show. Step through the four methods and watch what happens inside the lens." %}

1. **Method 0, `step`.** Inside the lens, the edge is a staircase. Every pixel is fully one colour or the other, so the edge can only lie on pixel boundaries.
2. **Turn Spin up and stay on method 0.** Now watch the outside of the lens, at normal size. The staircase does not just look rough; it *crawls*. Pixels flip between the two colours as the edge sweeps across them, and moving aliasing is far more objectionable than static aliasing. This is the real reason to care.
3. **Method 1, linear ramp.** The staircase is gone. Raise Width and look at where the ramp meets the flat regions: there is a visible crease on both sides.
4. **Method 2, `smoothstep` at a fixed width.** The creases are gone. Now change Zoom from 4 to 40 and watch the edge get wider and wider in the lens. The transition is a fixed fraction of the field, so magnifying it magnifies the blur, which is not what an edge does.
5. **Method 3, `fwidth`.** Change Zoom across its whole range. The edge stays one pixel wide. That is the property the other three do not have.
6. **Compare 2 and 3 at Zoom 1.** They are nearly identical, which is why method 2 survives: at the one scale you tuned, it is right.
7. **Read `coverage()` in the source.** Four branches, one per method, each two lines. Note that method 3 multiplies `fwidth` by 0.75 rather than using it directly; that constant is taste, and values between 0.5 and 1.0 all look reasonable.

## Look at these

{% include toy.html id="ltBXRc" title="Antialiasing comparison" by="Shadertoy community" note="The same four methods on a grid of shapes, side by side rather than magnified." %}
{% include toy.html id="Xl2XWt" title="Analytic antialiasing" by="Inigo Quilez" note="What to do when fwidth is not good enough, which is rarer than people think." %}

## The other use of smoothstep

`smoothstep` is not only for edges. Because it is a smooth ramp from 0 to 1 with flat ends, it is the default easing curve in shader work: use it on time to make something start and stop gently, on a distance to make a falloff, or on a noise value to increase contrast without clipping.

`smoothstep(0.4, 0.6, n)` on a noise field pushes values towards 0 and 1 and leaves a soft band in the middle. That single expression is how most of the shapes in Module D are cut out of noise, and it is worth recognising as contrast rather than as an edge.

## Common mistakes

- **Getting `step`'s argument order backwards.** `step(x, edge)` is not `step(edge, x)`, and the picture is inverted rather than broken, which is harder to spot.
- **Writing `smoothstep(-w, w, d)` when inside is negative.** You get the shape's complement. The idiom is `smoothstep(w, -w, d)`.
- **Tuning a fixed width at your working resolution.** It will be wrong on a projector, in a dome, and in a rendered export, which are the three places it matters.
- **Using `fwidth` on a value that is not a distance.** `fwidth` of an already-thresholded value is 0 almost everywhere and enormous at the edge, which gives you back the staircase with extra steps.
- **Antialiasing after compositing instead of before.** Compute coverage, then `mix` with it. Blurring the final image is not antialiasing; it is blurring.
- **Expecting `fwidth` in a compute shader.** It does not exist there. [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html) says what to do instead.

## Exercise

Take the rays you built for [Unit 03]({{ site.baseurl }}/learn/03-coordinates.html)'s exercise, which almost certainly flicker at the outer edge, and give them a `long` input that selects between `step`, a fixed-width `smoothstep`, and an `fwidth` version.

Then add a slow rotation and watch each one move.

**Success criterion:** with `step`, the rays visibly crawl as they rotate. With the `fwidth` version, they do not, at any resolution and with the rays as thin as you can make them. If the `fwidth` version still flickers where the rays converge at the centre, that is not a mistake in your edge; it is that the rays are genuinely narrower than a pixel there, and no edge function can fix undersampling. [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html) says what can.

## Going further

- [Inigo Quilez, on distance-based antialiasing](https://iquilezles.org/articles/distfunctions2d/), which is where this and Module C meet.
- [The Book of Shaders, chapter 5](https://thebookofshaders.com/05/), for `smoothstep` as a shaping function rather than as an edge.
- [GLSL ES 3.00 derivatives](https://registry.khronos.org/OpenGL-Refpages/es3.0/), for what `fwidth`, `dFdx`, and `dFdy` are actually specified to do.
