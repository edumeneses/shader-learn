---
layout: default
title: "Milestone P3: a raymarched scene that runs at frame rate"
description: "The third milestone. Build a scene, then make it fit a step budget, and be able to say where every step goes."
parent: Units
nav_order: 30
unit: "P3"
permalink: /learn/p3-raymarched-scene.html
reading_time: "13 min"
practice_time: "75 min"
glsl: "GLSL ES 3.00"
---

# Milestone P3: a raymarched scene that runs at frame rate

{% include unit_meta.html %}

> **Before this milestone** finish Units 24 to 27. Nothing here is new.
>
> **You will need** about an hour and a quarter, and ideally a slower machine to test on.
>
> **You will build** one raymarched scene that holds a stated frame rate on stated hardware, with the evidence to prove it.

## Why this matters

The previous two milestones asked for something that looked right. This one asks for something that *runs*, which is a different problem and the one that decides whether a piece can be performed.

A raymarched scene is the first thing in this course expensive enough that it can simply fail. Not look worse: fail, dropping to fifteen frames per second on a projector at a venue, on the machine that was available, in front of an audience. And it fails asymmetrically, because the machine you author on is almost always faster than the machine you show on.

The discipline that prevents it is not cleverness. It is measuring, budgeting, and being able to say where the steps go.

## The brief

**One raymarched scene. A stated budget. Evidence that it fits.**

Requirements:

1. **At least four distinct objects**, at least two materials, and at least one use of repetition from [Unit 10]({{ site.baseurl }}/learn/10-repetition.html).
2. **An orbitable camera** on a `point2D`, and a movable light on another. A scene you cannot look around is a scene you cannot inspect.
3. **Soft shadows and ambient occlusion**, both from [Units 26]({{ site.baseurl }}/learn/26-lighting.html) and [27]({{ site.baseurl }}/learn/27-materials-and-fog.html).
4. **Fog, with a height falloff**, and a tone curve at the end.
5. **A step budget as a named input**, and a view that shows cost against it.
6. **Every quality control exposed**: step budget, shadow steps, occlusion samples, step scale. These are the dials you will turn at a venue.
7. **It holds 60 frames per second at 1920 by 1080** on the machine you name, and you can say what happens on a machine half as fast.

## The reference solution

{% include shader.html id="p3-scene" height="480" pointer="orbit" caption="Nine towers in polar repetition around a centrepiece. Drag to orbit, and switch to the cost view: green finished early, yellow used most of the budget, red ran out before hitting anything. Red is not slow, it is wrong." %}

Read its source for three things it does deliberately.

**The polar fold checks three sectors, not one.** [Unit 10]({{ site.baseurl }}/learn/10-repetition.html) warned that a fold assumes the nearest copy is in your own cell. In two dimensions breaking that gave chopped shapes. Here it makes the field *over-estimate*, and an over-estimating field lets the marcher step through a surface: the symptom was a wedge bitten out of whichever tower sat nearest a sector boundary, and it looked exactly like a modelling error. Three evaluations, one line each, and it is gone.

**The fold rebuilds a real position rather than using arc length.** `a * r` is the arc, which is longer than the straight line, so an arc-length fold over-estimates for the same reason and produces the same artefact.

**Surface detail defaults to zero.** The control is there, and raising it adds a displacement to the distance, which breaks the metric and needs a lower step scale. That is a genuine trade and the milestone wants you to make it deliberately rather than inherit it.

## Build it

1. **Block the scene out with no lighting at all.** Flat colour on hit. Get the composition and the camera right while every frame is cheap.
2. **Turn on the step view and leave it on** while you build. It is the only view that tells you what you are actually paying for, and getting used to reading it is most of this milestone.
3. **Add lighting, then shadows, then occlusion, then fog**, in that order, checking the step view after each. Shadows will roughly double your cost; nothing else should change it much.
4. **Now find your budget.** Lower the step budget until the cost view starts showing red, then raise it until the red is gone. That number is what your scene actually needs, and it is usually lower than the 128 everyone copies.
5. **Reduce it.** Every scene has cheap wins: a `smin` with a smaller `k`, a far plane pulled in, an epsilon that scales with distance, a shadow march with fewer steps and a coarser minimum. Take each one and watch the step view.
6. **Measure.** Render a clip at your target resolution and time it:

   ```bash
   time python3 scripts/render.py library/shaders/p3/scene.fs \
       --out /tmp/p3 --formats mp4 --size 1920x1080 --duration 5 --fps 60
   ```

   Three hundred frames. Divide, and you have a per-frame time on this GPU. It is not a browser measurement, and it is a real number that a browser's frame rate cannot give you while it is also compositing a page.

7. **Test on something slower.** Halve the resolution and double the expected cost, or better, open it on a laptop. Write down what breaks first.
8. **Write the numbers down in the shader's header.** The `DESCRIPTION` field is a good place. A scene whose budget is documented can be tuned by someone else at a venue; one whose budget lives in your head cannot.

## Where the steps actually go

Three answers cover almost every scene, and knowing them shortens the search.

**Grazing angles.** A ray nearly parallel to a surface creeps along it, taking many short steps. The ground plane receding to the horizon is the classic case, and it is usually the brightest region of any step view. The fix is a far plane pulled in and fog to hide it, not a cleverer field.

**Silhouettes.** A ray that passes just outside an object gets small distances all the way past it and never hits anything. Every object in the scene is outlined in the step view for this reason, and there is no fix; it is what the algorithm does. It is worth knowing so you stop looking for a bug.

**Degraded fields.** Anywhere the field stopped being an exact distance, the marcher has to creep. A `smin` fillet, a displacement, a twist, a repetition with a cell that is slightly too small: each shows up as a bright region that does not correspond to anything a viewer would call complicated. These are the ones worth hunting, because they are the only ones you can actually fix.

The working method is to look at the step view and ask, region by region, which of the three it is. Two of them you accept and one of them you go and fix.

## Success criteria

- **The cost view shows no red anywhere** at your stated budget, from every camera angle you intend to use.
- **You can state a measured per-frame time** at 1920 by 1080, and name the GPU.
- **You can name the three most expensive things in the scene** and say roughly what each costs.
- **Turning every quality control to its minimum** produces a scene that is worse and still recognisably the same scene. This is the setting you will use when the venue's machine is worse than promised.
- **The camera can go anywhere** without the cost tripling. A scene that is only affordable from one angle is a photograph.

## Common ways this goes wrong

- **Authoring at 800 by 450 and shipping at 1920 by 1080.** Four times the pixels, four times the cost, and the step count per pixel does not change to help you.
- **A step budget copied from a Shadertoy.** Measure yours.
- **Blaming the shading.** It is almost never the shading. Look at the step view.
- **An inexact field discovered late.** Every hole is a field that over-estimated. Find it before you build the lighting on top of it.
- **Reflections added before the base scene fits its budget.**
- **No slower machine to test on.** If you genuinely cannot get one, halve the resolution and treat the result as an optimistic estimate.
- **Quality controls that are constants.** The whole point is to be able to turn them down without recompiling, in a room, under time pressure.

## Going further

- [Inigo Quilez, on raymarching performance](https://iquilezles.org/articles/raymarchingdf/), the section on step counts and epsilon.
- [Unit 34]({{ site.baseurl }}/learn/34-cost.html), which is this milestone's discipline made into a unit, with tools.
- [Unit 41]({{ site.baseurl }}/learn/41-geometry-and-compute.html), for what to do when a fragment shader is genuinely the wrong tool.
