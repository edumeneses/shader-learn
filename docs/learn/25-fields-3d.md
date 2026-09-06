---
layout: default
title: "Unit 25: Distance fields in three dimensions and their operators"
description: "The 2D primitives and operators, with one more component. What is genuinely new is that a field that lies now costs you geometry rather than only isolines."
parent: Units
nav_order: 27
unit: "25"
permalink: /learn/25-fields-3d.html
reading_time: "14 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 25: Distance fields in three dimensions and their operators

{% include unit_meta.html %}

> **Before this unit** read [Unit 24]({{ site.baseurl }}/learn/24-raymarching.html).
>
> **You will need** the player below, and the step-count view.
>
> **You will build** a scene function, and an accurate sense of what each primitive costs.

## Why this matters

Almost everything in Module C transfers unchanged. `min` is still union, `max` is still intersection, `smin` is still the smooth minimum, `abs` still folds space, and `mod` still repeats it. If you can build a shape in two dimensions you can build it in three, and the code is nearly identical.

What changes is the consequence of getting it wrong. In two dimensions an inexact field gave you wrong isolines and a shape that was still correct. In three dimensions the field *is* the geometry: the marcher reads it to decide how far to step, so a field that over-estimates produces holes, and a field that under-estimates produces a scene that renders correctly and slowly. Exactness stops being a nicety.

## The idea

**The primitives are the 2D ones with a component added.** A sphere is `length(p) - r`. A box is the same two-term expression with a three-way `max` inside. The pattern from [Unit 07]({{ site.baseurl }}/learn/07-distance-fields-2d.html), fold with `abs`, handle outside with `length(max(d, 0.0))` and inside with `min(max(...), 0.0)`, is unchanged.

**Dimension reduction is the trick worth learning.** A torus is a 2D problem in disguise: measure the distance from the y axis, subtract the major radius, pair that with y, and take the length. Two lines. The same move gives you a cylinder, a cone, and anything with rotational symmetry, and recognising when a 3D shape is a 2D shape spun around something saves a great deal of work.

**Operators are identical.** `min`, `max`, `max(a, -b)`, `smin`. Nothing about [Unit 08]({{ site.baseurl }}/learn/08-combining-fields.html) changes, including the caveat that the result is a bound rather than an exact distance in the region between shapes. Here that caveat has teeth: it costs steps.

**Repetition is identical and much more useful.** `mod` folds three dimensions as happily as two, and an infinite lattice of objects costs the same as one. This is where raymarching genuinely beats rasterisation: a million cubes is one cube.

**Some things are not distance fields at all.** A gyroid, `dot(sin(p), cos(p.zxy))`, is an implicit surface: it has the right sign and its magnitude is not a distance to anything. It can still be marched, if the step is scaled down by a bound on its gradient, and the step view shows what that costs. Recognising the difference between "a distance" and "a number with the right sign" is the single most useful skill in this module.

**Carry a material with the distance.** Return a `vec2` from the scene function: distance and an id. The union operator becomes "whichever is nearer, and its id", and one march then produces a scene with several materials. It is bookkeeping, and it is why almost every published raymarcher's `map` returns two numbers.

**Cost is not uniform across primitives.** A sphere is a subtract and a length. A box is a dozen operations. A gyroid is three sines and three cosines, evaluated at every step, which at a hundred steps is six hundred transcendentals per pixel. The step view plus a look at the source is how you find out which half of that you are paying.

## Build it

{% include shader.html id="25-fields-3d" height="460" pointer="orbit" caption="Drag the canvas to orbit. Step through the primitives with the step view on and off, and watch which shapes are expensive: the gyroid is not expensive because it is complicated, it is expensive because it is not a distance." %}

1. **Start on the torus with the operator on smooth union.** Drag to orbit. Everything from Module C is here, one component wider.
2. **Switch through the primitives on the shaded view.** Note the rounded box: exactly as in two dimensions, rounding is a subtraction and it costs nothing.
3. **Switch to the step view and go through them again.** Sphere and box are cheap. The octahedron is cheap and slightly wrong: its function is a bound rather than an exact distance, which is why it is one line instead of ten.
4. **Select the gyroid, in the step view.** It is bright everywhere. The field's magnitude is not a distance, so the shader divides it down to be safe, and safety here is bought entirely with steps.
5. **With the gyroid selected, raise Step scale towards 1.2.** Holes appear. Lower it to 0.4 and they close. That dial is the whole relationship between exactness and cost, on a slider.
6. **Go back to the torus and sweep Smoothing on smooth union.** Watch the step view: the fillet region is measurably more expensive than either shape, because `smin` degrades the field exactly there.
7. **Raise Repeat.** A lattice of objects, at no cost in the field. Compare the step view before and after: it barely changes, which is the argument for this whole technique in one picture.
8. **Set the operator to subtraction and orbit into the cut.** Note that the interior surface is correctly lit, which a rasteriser would have needed geometry for.

## Look at these

{% include toy.html id="Xds3zN" title="Raymarching primitives" by="Inigo Quilez" note="Twenty exact primitives with their derivations, and the material-id pattern this unit describes." %}
{% include toy.html id="MsVGWG" title="Gyroid" by="Shadertoy community" note="An implicit surface marched safely; look for the divisor on the step." %}
{% include toy.html id="3lsSzf" title="3D distance function operators" by="Inigo Quilez" note="Elongation, rounding, onioning, twisting, bending, each in one line." %}

Quilez's [3D distance functions article](https://iquilezles.org/articles/distfunctions/) has about sixty primitives, each with a note on whether it is exact.

## The operators that only exist in three dimensions

Three transforms are worth knowing because they have no useful two-dimensional equivalent and they produce most of the shapes people associate with this technique.

**Twist.** Rotate the coordinate about an axis by an amount that depends on the position along that axis. Four lines, and a box becomes a helix. It breaks the metric, because a twist stretches space by an amount that grows with radius, so the step has to come down.

**Bend.** The same idea with a rotation that depends on a perpendicular axis. Same caveat.

**Revolution.** Take any two-dimensional distance function, feed it `vec2(length(p.xz) - r, p.y)`, and you have spun it around the y axis. This is the torus trick generalised, it is exact, and it means every shape from [Unit 07]({{ site.baseurl }}/learn/07-distance-fields-2d.html) is also a three-dimensional shape for one line of work.

The pattern behind all three: **a transform that stretches space breaks the field, and the fix is always to divide by a bound on how much it stretches**. A twist of `k` radians per unit at radius `r` stretches by up to `k * r`, so dividing the result by `1 + k * r` keeps the marcher safe. Getting that bound roughly right and then lowering the step scale until the holes close is the honest working method.

## Common mistakes

- **Assuming a published function is exact.** Many widely copied ones are bounds. Quilez's article marks which are which, and it matters here in a way it did not in two dimensions.
- **A non-uniform scale without dividing the result** by the smallest scale factor. [Unit 09]({{ site.baseurl }}/learn/09-transforms.html), with holes as the symptom instead of wrong isolines.
- **Adding a displacement to the distance** and marching at full step. Any bump, any noise, any texture-driven detail breaks the metric and needs a smaller step.
- **A repetition cell smaller than the object.** [Unit 10]({{ site.baseurl }}/learn/10-repetition.html)'s trap, and in three dimensions the symptom is objects sliced flat at cell boundaries.
- **Building the scene function so it does everything at every step.** The marcher calls it a hundred times; anything that can be hoisted out should be.
- **Forgetting the material id** and discovering it after writing the whole scene.

## Exercise

Build a scene function containing at least four primitives with three different materials, using the `vec2` distance-and-id convention, and combining them with at least three different operators.

Then add a `bool` named `exact` which switches one of the primitives between an exact function and a deliberately inexact one, such as a non-uniform scale with no correction.

**Success criterion:** with `exact` on, the scene renders with no holes at a step scale of 1.0. With it off, holes appear in that primitive and nowhere else, and lowering the step scale closes them. If holes appear in a different object, your scene function has a second inexactness you did not know about, which is the more useful outcome.

## Going further

- [Inigo Quilez, 3D distance functions](https://iquilezles.org/articles/distfunctions/), the reference.
- [Inigo Quilez, on interior distances](https://iquilezles.org/articles/interiordistance/), for what exactness costs to preserve.
- [Unit 26]({{ site.baseurl }}/learn/26-lighting.html), next, which turns a hit into a picture.
