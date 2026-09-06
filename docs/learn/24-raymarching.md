---
layout: default
title: "Unit 24: Raymarching, the sphere-traced loop"
description: "Twelve lines put three dimensions inside a fragment shader. The loop advances by the distance to the nearest surface, which is always safe, and the cross-section view lets you watch it happen."
parent: Units
nav_order: 26
unit: "24"
permalink: /learn/24-raymarching.html
reading_time: "15 min"
practice_time: "35 min"
glsl: "GLSL ES 3.00"
---

# Unit 24: Raymarching, the sphere-traced loop

{% include unit_meta.html %}

> **Before this unit** read [Unit 07]({{ site.baseurl }}/learn/07-distance-fields-2d.html). Everything about exactness matters now.
>
> **You will need** the player below, on the cross-section view.
>
> **You will build** the loop that the whole of Module G runs on.

## Why this matters

A fragment shader has no geometry, no vertices, no depth buffer, and no scene graph. It has a coordinate and some arithmetic. Raymarching turns that into three dimensions with about twelve lines, and it is the reason a single file with no assets can contain a lit, shadowed, reflective scene.

The idea is one observation. If you have an exact distance field, then the value it returns at a point is a radius you can move through in *any* direction without hitting anything. So a ray can advance by that distance, ask again, advance again, and it will never overshoot a surface. It is called **sphere tracing** because each step is the radius of a sphere known to be empty.

That is the whole algorithm. What makes it worth a unit is everything around it: what "exact" turns out to mean in practice, what happens when a field lies, and where the cost goes.

## The idea

**The loop.**

```glsl
float t = 0.0;
for (int i = 0; i < MAX_STEPS; i++) {
    vec3 at = ro + rd * t;
    float d = map(at);
    if (d < EPSILON) { hit = true; break; }
    if (t > FAR) break;
    t += d;
}
```

`ro` is the ray origin, the camera. `rd` is the ray direction, one per pixel, which is what makes the image a perspective view. `map` is the scene, a function from a point to a distance, and it is the only part you write.

**Why it terminates in a useful way.** Far from anything, `d` is large and the ray covers ground quickly. Approaching a surface, `d` shrinks and the steps get finer, converging on the boundary. The algorithm spends its effort exactly where the detail is, with no acceleration structure and no sorting.

**Epsilon decides where "hit" is.** Too large and surfaces look inflated and lose fine detail; too small and the loop runs out of steps near grazing angles and leaves soft haloes around silhouettes. Scaling it with distance, so distant surfaces get a looser test, is the standard refinement and costs one multiply.

**The step count is the cost.** Every step is a full evaluation of `map`, and `map` is your whole scene. A hundred steps means calling your scene function a hundred times **per pixel**. This is why [Unit 34]({{ site.baseurl }}/learn/34-cost.html) exists and why the step-count view below is the most useful debugging tool in three-dimensional shader work.

**Grazing angles are where the time goes.** A ray nearly parallel to a surface stays close to it for a long way, so `d` is small and every step is short. Silhouettes and shallow ground planes are almost always the expensive part of the image, and the step view shows it immediately.

**A field that over-estimates will step through surfaces.** Any operation that breaks exactness, from [Unit 09]({{ site.baseurl }}/learn/09-transforms.html)'s non-uniform scale to a displacement added to the distance, makes the field lie. The universal fix is to multiply the step by a factor below 1, which trades speed for safety. That factor is on a control below, and it is worth understanding as a real dial rather than a magic number.

**Write the loop bound as a compile-time constant.** Some WebGL drivers reject a loop whose bound is a uniform, and a raymarcher written the natural way compiles on a desktop and fails in a browser with an unhelpful message. Loop to a fixed maximum and `break` on the real limit. Every raymarcher in this course does.

## Build it

{% include shader.html id="24-raymarch" height="460" pointer="aim" caption="Start on the cross-section: the same algorithm in two dimensions, where it can be drawn. Each circle is one step, and its radius is the distance the field reported. Drag the canvas to aim the ray." %}

1. **Stay on the cross-section and drag the ray around.** Watch the circles: large in open space, shrinking as the ray approaches something, and stopping exactly at a surface. **No circle ever overlaps a surface**, and that is the whole guarantee.
2. **Aim the ray so it passes close to the sphere without hitting it.** The circles bunch up: the ray is creeping. This is the grazing-angle cost, and it is why silhouettes are expensive.
3. **Lower Max steps to about 12** and aim at something far away. The ray stops short. In a real render that pixel would be background, so an under-budgeted marcher eats holes in its own geometry.
4. **Raise Hit epsilon to 0.05.** The ray stops well before the surface. Now imagine that as a render: everything is slightly inflated and small features are gone.
5. **Raise Step scale above 1.** The circles overshoot their own radius and the ray passes through the thin bar. This is the failure mode of an over-estimating field, caused deliberately.
6. **Lower Step scale to 0.4.** Many more, much smaller circles. Safe, and the price is steps.
7. **Switch to steps taken.** The heat map of the three-dimensional scene. The bright regions are the silhouettes and the ground plane at grazing angle, exactly as the cross-section predicted.
8. **Switch to the render** and compare it against the step view. Everything that looks cheap is dark, and everything that looks like nothing, the empty ground stretching away, is bright.

## Look at these

{% include toy.html id="Xds3zN" title="Raymarching primitives" by="Inigo Quilez" note="The reference implementation, with about twenty primitives. Read `castRay` first and ignore the rest." %}
{% include toy.html id="ld3Gz2" title="Snail" by="Inigo Quilez" note="What this technique looks like taken as far as anyone has taken it." %}
{% include toy.html id="4dSfRc" title="Raymarching, explained step by step" by="Shadertoy community" note="The same loop with the intermediate values visualised, from a different angle than this unit's." %}

Quilez's [raymarching articles](https://iquilezles.org/articles/raymarchingdf/) are the reference for everything in Module G.

## What this is not

**It is not ray tracing.** A ray tracer solves for the intersection analytically, which is exact and fast and requires the geometry to be something you can solve for. Raymarching finds the intersection by walking, which works for anything you can write a distance function for, including things with no surface description at all.

**It is not volume rendering**, though the loops look similar. A volume renderer steps at fixed intervals and accumulates; this one steps adaptively and stops. The two are combined in practice, and cloud rendering is exactly that combination.

**It is not how games render.** Rasterising triangles is orders of magnitude faster for the same picture. Raymarching earns its place where the geometry would be impossible or enormous: fractals, infinite repetition, procedural terrain, and anything that has to arrive as one file with no assets.

## Common mistakes

- **A loop bound that is a uniform**, working locally and failing in a browser.
- **Forgetting to normalise the ray direction.** The distance `t` is then in the wrong units and every step is wrong by the same factor, which looks like a broken field.
- **An epsilon that does not scale with distance**, so distant geometry is expensive and near geometry is inflated.
- **Stepping by more than the reported distance.** Holes in thin geometry, and they move as the camera moves.
- **A field built from operations that break exactness**, then marched at full step. Reduce the step, or fix the field.
- **Testing `d == 0.0`.** Floating point never lands on a surface.
- **Assuming the cost is in the shading.** It almost never is; it is in the march, and the step view proves it in ten seconds.

## Exercise

Write a raymarcher from scratch: a camera you can orbit with a `point2D`, one sphere, one plane, and flat colour on hit.

Then add a `long` input that switches the display between the render, the step count, and the final distance `t` as a depth map.

**Success criterion:** the sphere is round from every camera angle, the horizon is at the correct place, and the step view shows a bright band exactly where the ground plane recedes to the horizon. If the sphere is an ellipse, the ray direction is being built in a space that is not square, which is [Unit 03]({{ site.baseurl }}/learn/03-coordinates.html) arriving in three dimensions.

## Going further

- [Inigo Quilez, raymarching distance fields](https://iquilezles.org/articles/raymarchingdf/), the reference.
- [John Hart, *Sphere Tracing*](https://link.springer.com/article/10.1007/s003710050084), 1996, the paper that named it.
- [Unit 25]({{ site.baseurl }}/learn/25-fields-3d.html), next, which fills in the scene function this unit left as `map`.
