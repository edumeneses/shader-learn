---
layout: default
title: "Unit 15: Domain warping, the technique that makes noise look like art"
description: "Ask the noise where to look before you look. Three lines, one extra fbm call, and the difference between a heat map and marble."
parent: Units
nav_order: 16
unit: "15"
permalink: /learn/15-domain-warping.html
reading_time: "13 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 15: Domain warping, the technique that makes noise look like art

{% include unit_meta.html %}

> **Before this unit** read [Unit 14]({{ site.baseurl }}/learn/14-fbm.html).
>
> **You will need** the player below.
>
> **You will build** the highest-return three lines in this course.

## Why this matters

Fractal Brownian motion gives you clouds. Real clouds, but only clouds: a field of soft blobs at several scales, statistically uniform, with no direction and no structure. It looks like weather data. Almost nothing else in the world looks like that, because almost everything has been *pushed around* by something: flowing, folding, stretching, eroding.

Domain warping is how you push it around, and it is startlingly cheap. Instead of asking the noise about the point you are at, ask it about a point displaced by *another* noise field. Do that twice and the result stops looking like data and starts looking like marble, agate, oil on water, smoke, or wood grain, depending entirely on the numbers.

The ratio of visual return to code is higher here than anywhere else in this course. Three lines.

## The idea

**The form.**

```glsl
vec2 q = vec2(fbm(p),               fbm(p + vec2(5.2, 1.3)));
vec2 r = vec2(fbm(p + 4.0 * q),     fbm(p + 4.0 * q + vec2(8.3, 2.8)));
float n = fbm(p + 4.0 * r);
```

Read it as three questions. `q` is a vector field: two fbm calls at offset origins, one used as an x displacement and one as a y. `r` is the same thing again, but asked about a point that has already been displaced by `q`. The final answer is the noise at a point displaced by `r`.

**The offsets are arbitrary.** `(5.2, 1.3)` and `(8.3, 2.8)` are Quilez's, and any pair far enough apart to be uncorrelated does the job. They exist only so that the x and y components of the vector field are different fields rather than the same one.

**One level gives flow.** The picture develops direction: streaks, currents, a sense that the material moved. This alone is often enough.

**Two levels give material.** The streaks themselves become curved and folded, and the result acquires the small-scale structure that the eye reads as a substance rather than as a pattern. This is where marble and agate come from.

**Three levels rarely pays.** The cost is another two fbm calls, which at five octaves each is ten noise evaluations, and the picture usually gets muddier rather than richer.

**The warp amount matters more than the level count.** Small amounts, well under a lattice cell, give a gently disturbed field. Amounts around one cell give flow. Large amounts tear the field apart into something that no longer reads as continuous. The interesting range is narrow and worth exploring slowly.

**Warping is not limited to noise.** Displace the coordinate before any function and you have warped it. A distance field warped by noise gives a shape with an eroded surface. A texture lookup warped by noise gives heat haze or water. The technique is about the coordinate, not about what reads it, which is why it composes with everything in Modules C and F.

**Shading it costs almost nothing.** Treat the final field as a heightfield and light it with its own screen-space gradient, `dFdx` and `dFdy`. Two derivative instructions, no extra samples, and it is the difference between a coloured field and something with relief. The player below does this and the effect of turning it off is worth seeing.

## Build it

{% include shader.html id="15-warp" height="440" pointer="none" caption="Step the warp level from none to two and watch what happens. Then use the two diagnostic views: the warp field shows the displacement as colour, and the displacement view shows how far each point was moved to get its answer." %}

1. **Start at no warp.** Plain fbm. Clouds, uniform, directionless. This is the baseline and it is worth looking at long enough to be dissatisfied with.
2. **Go to one level.** Direction appears. The field now has currents, and it does so because neighbouring points are looking up values from places that are further apart than they are.
3. **Go to two levels.** The currents fold. Filaments appear inside the currents. This is the step that changes the category of the picture.
4. **Sweep Warp 1 from 0 to 12 slowly.** Watch where it stops improving. Around 3 to 5 is usually the interesting range; past that the field tears.
5. **Switch to the warp field view.** Red is horizontal displacement, green is vertical. It is a smooth, low-frequency field, which is the point: the displacement itself is gentle, and the drama in the result comes from the *difference* in displacement between neighbours.
6. **Switch to how far each point moved.** The bright regions are where the lookup travelled furthest, and if you flip back to the result you will find that those are exactly where the filaments are. Nothing here is mysterious once you can see both.
7. **Turn Shading down to zero and back.** Without it the picture is a coloured field. With it, it is a surface. Two derivative instructions.
8. **Turn Drift up.** The field flows. Note that this is not a loop; [Milestone P2]({{ site.baseurl }}/learn/p2-living-surface.html) is about making it one.

## Look at these

{% include toy.html id="4s23zz" title="Warping" by="Inigo Quilez" note="The article's companion. The definitive demonstration, and the source of the constants everyone uses." %}
{% include toy.html id="lsl3RH" title="Warping, 2 levels" by="Inigo Quilez" note="The same thing with the intermediate fields shown, which is what this unit's diagnostic views do." %}
{% include toy.html id="4tdSWr" title="2D clouds" by="Drew Whitehouse" note="A warp applied to something with a purpose rather than to a test pattern." %}

Quilez's [domain warping article](https://iquilezles.org/articles/warp/) is one page and is the whole technique.

## Where else to warp

- **Before a distance field.** `sdSphere(p + 0.1 * warp(p))` gives an eroded, organic version of a hard shape. Note that this breaks the distance metric, so a raymarcher needs to divide its step size; [Unit 25]({{ site.baseurl }}/learn/25-fields-3d.html) returns to this.
- **Before a texture read.** Heat haze, water refraction, glitch. This is one of the most common uses in live visual work and it needs a texture to read, which is [Unit 20]({{ site.baseurl }}/learn/20-sampling.html).
- **Before a repetition.** Warp, then fold, and the lattice from [Unit 10]({{ site.baseurl }}/learn/10-repetition.html) stops being a lattice.
- **In polar coordinates.** Warping the angle rather than the position gives swirls and vortices, and it costs the same.

## Common mistakes

- **Reusing the same fbm for x and y displacement.** The displacement is then always along the diagonal and the result has an obvious bias. Offset the origins.
- **Warping by too much.** Past a certain amount the field stops being continuous in any useful sense and the picture becomes noise again, expensively.
- **Warping with a different scale of noise than the field being warped.** Usually a mistake, occasionally the whole idea. Do it deliberately.
- **Forgetting the cost.** Two levels is five fbm calls. At five octaves each that is twenty-five noise evaluations per pixel, and [Unit 34]({{ site.baseurl }}/learn/34-cost.html) will have things to say.
- **Expecting `fwidth` to behave.** A heavily warped field changes fast between neighbouring pixels, so derivative-based antialiasing and derivative-based shading both get noisy. The player scales its gradient by a constant rather than dividing by `fwidth` for exactly this reason.

## Exercise

Take the shape you built for [Milestone P1]({{ site.baseurl }}/learn/p1-poster.html) and warp its coordinate with one level of fbm before evaluating the distance field.

Requirements: a `float` input named `erosion` controls the warp amount from none to substantial; the shape must still be recognisable at maximum; and the edge must stay antialiased, which will need thought because the warp changes how fast the field varies.

**Success criterion:** at `erosion` zero the picture is identical to your P1 result, and at maximum the shape has a rough, weathered boundary with no aliasing anywhere along it. If the edge sparkles at high erosion, your `fwidth` is measuring a field that is no longer a distance, and the fix is to scale the field back down by roughly the warp's gradient.

## Going further

- [Inigo Quilez, domain warping](https://iquilezles.org/articles/warp/), the original.
- [Inigo Quilez, on the derivative of warped noise](https://iquilezles.org/articles/morenoise/), for keeping the gradient correct through a warp.
- [Unit 16]({{ site.baseurl }}/learn/16-voronoi.html), next, which is the other structure worth warping.
