---
layout: default
title: "Unit 34: Reading the cost of a shader, and paying less"
description: "What a line of GLSL costs, how to measure rather than guess, and the five optimisations that account for almost all the wins. Late in the course on purpose."
parent: Units
nav_order: 37
unit: "34"
permalink: /learn/34-cost.html
reading_time: "14 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 34: Reading the cost of a shader, and paying less

{% include unit_meta.html %}

> **Before this unit** finish [Milestone P3]({{ site.baseurl }}/learn/p3-raymarched-scene.html), which is where cost first became a constraint.
>
> **You will need** a shader of your own that is too slow.
>
> **You will build** a method for finding out where the time goes, and the short list of things worth doing about it.

## Why this matters

This unit is deliberately the thirty-fifth. Optimising a shader before you can write one produces work that is fast and ugly, and the field is full of people who learned the tricks before the technique.

It arrives now because from here the course is about performance in a room. A piece that runs at 45 frames per second on your machine will run at 20 on the venue's, and the moment to find that out is not during the get-in. The discipline is the same as any other performance work and the specifics are unusual enough to be worth a unit: the cost model of a GPU is not the cost model of a CPU, and most of the intuitions transfer badly.

## The idea

**Everything is multiplied by the pixel count.** At 1920 by 1080 that is two million. One extra texture read per pixel is two million reads per frame, a hundred and twenty million per second. This is the fact that makes shader optimisation different: there are no rare paths, and a line that runs once conceptually runs two million times.

**Arithmetic is nearly free; memory is not.** A multiply-add is one cycle and the hardware has thousands of lanes. A texture read that misses cache is hundreds of cycles. The first instinct on a CPU, to precompute and store, is often exactly wrong here: recomputing a value is frequently cheaper than fetching it.

**Divergence costs.** A GPU runs threads in lockstep batches of 32 or 64. If some take a branch and others do not, **both sides run** and the results are masked. A branch that is coherent across the screen, such as sky against geometry, is nearly free; one that differs per pixel costs the sum of both sides. This is why `mix` and `step` are so common: they have no branch to diverge.

**Loops with a variable trip count cost their worst case in a batch.** A raymarcher where one pixel needs 200 steps and its neighbours need 20 costs 200 for all of them. This is exactly why silhouettes are expensive.

**The expensive built-ins**, roughly in order: `pow`, `exp`, `log`, `sin`, `cos`, `atan`, and division. A few per pixel is nothing; a few inside a loop that runs a hundred times is the whole frame. `inversesqrt` is fast, `normalize` is a division, and `length` is a square root.

**Precision is a real dial on mobile.** `mediump` is often twice as fast as `highp` on a phone and identical on a desktop, which is why a shader can be fine on your machine and slow on a tablet.

### Measuring, not guessing

**Scale the resolution.** Halve the render size. If it gets four times faster, you are limited by per-pixel work. If it barely changes, you are limited by something else, and now you know.

**Use the step view.** In anything raymarched, the step count *is* the cost, and a heat map finds the expensive region in seconds. [Milestone P3]({{ site.baseurl }}/learn/p3-raymarched-scene.html) builds one.

**Comment things out.** Crude, reliable, and it works when nothing else does. Remove the reflection, remove the shadow, remove the occlusion, and note the difference each makes.

**Time a real render.** In this repository:

```bash
time python3 scripts/render.py <shader> --out /tmp/x \
    --formats mp4 --size 1920x1080 --duration 5 --fps 60
```

Three hundred frames, divided, gives a per-frame time uncontaminated by a browser compositing a page around it.

**Use a real profiler when you need one.** RenderDoc and NVIDIA Nsight give per-draw timings and instruction counts. They are the right tool once the crude methods have narrowed it down, and the wrong first step.

### The five optimisations that account for almost everything

1. **Render at a lower resolution.** Halving costs a quarter. For anything soft, a blur, a bloom, a feedback buffer, a reaction-diffusion, nobody will see it. This is by far the largest and least-used win.
2. **Do less per pixel.** Fewer octaves, fewer taps, fewer steps, fewer samples. Every one of those is a control in this course's shaders for exactly this reason.
3. **Make loops exit early and coherently.** A raymarcher's far plane and step budget are optimisations, and pulling the far plane in and hiding it with fog is free quality.
4. **Move work out of the loop.** Anything not depending on the loop variable is being computed a hundred times for nothing. This is the one the compiler often catches and sometimes does not.
5. **Separate what is separable.** [Unit 21]({{ site.baseurl }}/learn/21-convolution.html)'s blur: `2n` instead of `n²`. When it applies it is the biggest algorithmic win available.

Almost nothing else is worth doing before all five are done.

## Build it: measure something of your own

1. **Take your P3 scene**, or any shader you have that is expensive.
2. **Render it at 1920 by 1080 and time it.** Write the number down. This is the only number that matters and everything else is a hypothesis about it.
3. **Halve the resolution and time it again.** Four times faster means per-pixel work; less than that means you are bound by something else.
4. **Turn on the step view** and photograph it. That image is your budget, spatially.
5. **Comment out the single most expensive feature** you can identify, and time it. Repeat three times. You now have a ranked list, measured.
6. **Apply optimisation 1 to the most expensive thing that can tolerate it.** Blurs, occlusion, and feedback buffers almost always can; the primary geometry pass usually cannot.
7. **Re-time.** If it did not improve, put it back. An optimisation that does not measure as a win is complexity you now have to maintain.
8. **Stop when it fits the budget.** Not when it is fast. There is no prize for a shader that renders in 2 milliseconds when it had 16.

## Look at these

{% include toy.html id="4ttSWf" title="Rainforest" by="Inigo Quilez" note="Read the comments: they name what was cut and why, which is rarer and more useful than the technique." %}
{% include toy.html id="Xds3zN" title="Raymarching primitives" by="Inigo Quilez" note="A bounding-volume test before the expensive branch, which is optimisation 3 in practice." %}

## Common mistakes

- **Optimising without measuring.** The most common and the most expensive.
- **Optimising the wrong thing.** It is almost never the shading; it is the march, the taps, or the resolution.
- **Precomputing into a texture.** Sometimes right, often slower than recomputing.
- **Removing a branch that was coherent.** A branch that skips expensive work for most of the screen is a win, not a cost.
- **Trusting a browser's frame rate.** It is compositing a page, throttling, and vsyncing.
- **Optimising for your GPU.** Test on the slowest machine the piece will run on.
- **Continuing past the budget.** Time spent making a 16 ms frame into a 12 ms frame is time not spent on the work.

## Exercise

Take a shader of yours that runs at under 60 frames per second at 1920 by 1080 and make it fit, without removing anything a viewer would notice.

Keep a log: for each change, the measured per-frame time before and after. Reverse anything that does not measure as an improvement.

**Success criterion:** the shader fits, you have a log of at least five measured changes, and at least one of them is a reversal. If nothing was reversed, you were not guessing hard enough to be learning anything; the reversals are where the intuition comes from.

## Going further

- [Inigo Quilez, on performance in raymarching](https://iquilezles.org/articles/raymarchingdf/).
- [RenderDoc](https://renderdoc.org/), the profiler to reach for when the crude methods run out.
- [Milestone P4]({{ site.baseurl }}/learn/p4-visual-instrument.html), where this becomes a constraint with an audience attached.
