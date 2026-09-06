---
layout: default
title: "Unit 08: Union, subtraction, and the smooth minimum"
description: "min, max, and negation give you constructive solid geometry in one line each. The smooth minimum gives you the thing they cannot: a join with no crease."
parent: Units
nav_order: 8
unit: "08"
permalink: /learn/08-combining-fields.html
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 08: Union, subtraction, and the smooth minimum

{% include unit_meta.html %}

> **Before this unit** read [Unit 07]({{ site.baseurl }}/learn/07-distance-fields-2d.html).
>
> **You will need** the player below, in its field view.
>
> **You will build** the four operators that combine shapes, and one function, `smin`, that you will use more than any other in this course.

## Why this matters

A single distance function draws one shape. Everything interesting is several shapes, and the operators that combine them are so short that it is easy to miss how much they do: `min` is a union, `max` is an intersection, and `max(a, -b)` is a subtraction. Three lines give you the whole of constructive solid geometry, in two dimensions and, unchanged, in three.

Then there is the operator that has no equivalent in a modelling package, and it is the reason distance fields look the way they do. `min` produces a hard crease where two shapes meet, because the minimum of two smooth functions is not smooth. Replace it with a **smooth minimum** and the two shapes merge like two drops of water, with a fillet whose radius you control. That single substitution is responsible for most of the organic look in raymarched work, and it costs about four instructions.

## The idea

**Union is `min`.** Take the nearer of the two surfaces. If a point is 0.3 from a circle and 0.5 from a box, it is 0.3 from their union.

**Intersection is `max`.** A point is inside the intersection only if it is inside both, so take the larger distance, which is the one that is still positive when the other has gone negative.

**Subtraction is `max(a, -b)`.** Negating a field turns it inside out, so `-b` is the shape "everything except b". Intersecting that with `a` removes `b` from `a`. This is worth reading twice: the negation is the whole trick, and it works on any field.

**They stop being exact, and it usually does not matter.** `min` is exact everywhere except in the region between the two shapes, where the true distance is to a point on the join and the formula returns the distance to one of the originals. `max` is worse. In both cases the result is still a valid **bound**: it never over-estimates, so a threshold is correct and a raymarcher is safe. It is only when you offset the result, or rely on the isolines for a glow, that the error shows.

**The smooth minimum.** Quilez's polynomial form:

```glsl
float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}
```

`h` is how far through the transition the point is: 0 when `b` is much nearer, 1 when `a` is, and a linear ramp over a band of width `k` in between. `mix(b, a, h)` is the blended distance, and the last term is the correction that pulls the result below both inputs near the join, which is what actually creates the fillet rather than merely rounding the graph.

**Everything else is that function with signs flipped.** `smax(a, b, k)` is `-smin(-a, -b, k)`, and smooth subtraction is `smax(a, -b, k)`. One function to get right, three operators for free. Writing them as separate polynomials is a common way to introduce three subtly different `k` behaviours into one shader.

**`k` is a distance, not a fraction.** It is the width of the blend in the same units as the field, so it scales with the scene. A `k` tuned at one size is wrong at another, which is why a shader that lets a reader zoom should derive `k` from the scene scale rather than hard-code it.

**The order of operations is the model.** `smin(smin(a, b, k), c, k)` is not the same shape as `smin(a, smin(b, c, k), k)`, in the same way that a chain of blends in a modelling package depends on order. Neither is wrong; they are different objects, and knowing which one you are building saves a lot of confusion when a third shape appears.

## Build it

The player combines two circles with all six operators, in the field view, so you can see what each one does to the space between the shapes rather than only to their outlines.

{% include shader.html id="08-combine" height="420" pointer="left" caption="Drag the canvas to move the left circle; the Right shape control moves the other. Watch the isolines in the gap between them, which is where every one of these operators does its real work." %}

1. **Start on union, with the circles apart.** Two independent sets of isolines. Bring them together until they touch, and watch the isolines in the gap form a sharp V. That V is the crease.
2. **Switch to smooth union with the same positions.** The V becomes a smooth curve. Raise Smoothing and watch the two shapes begin to reach for each other before they touch; that is not an illusion, the field really has changed at a distance.
3. **Set Smoothing very small on smooth union.** It converges on plain `min`. The operators are a family, and `min` is the limit of `smin` as `k` goes to zero.
4. **Switch to intersection with the circles overlapping.** You get a lens. Now pull them apart and watch what happens: the shape vanishes but the field does not go away, and the isolines outside are wrong in an obvious way. That is the exactness cost, visible.
5. **Switch to subtraction.** The right circle bites a hole in the left one. Swap which one is bigger and note that subtraction is not commutative, unlike the other two.
6. **Switch to smooth subtraction and raise Smoothing.** The bite gets a fillet on the inside of the cut. This is the operator that makes a carved shape look moulded rather than machined.
7. **Turn Isolines off and set the view to the shape.** This is what the reader of your finished piece sees. Everything you have been looking at is still there, doing the work.

## Look at these

{% include toy.html id="lt3BW2" title="3D distance operators" by="Inigo Quilez" note="The same operators in three dimensions, which is where they matter most; identical code." %}
{% include toy.html id="Mt3GWs" title="Smooth minimum comparison" by="Inigo Quilez" note="Polynomial, exponential, and power forms side by side, with their costs." %}

Quilez's [article on smooth minimum](https://iquilezles.org/articles/smin/) covers the three forms and when each is worth its cost.

## The other smooth minimums

The polynomial form above is the default because it is cheap and has no surprises. Two others are worth knowing:

**Exponential**, `-log(exp(-k*a) + exp(-k*b)) / k`, blends more than two shapes at once correctly, because the sum inside generalises to any number of terms. It costs an exponential per shape and a logarithm, and it never quite reaches either input, so a shape blended this way is always slightly larger than it should be.

**Power**, which is smoother still and more expensive again.

There is also a version that returns **which shape won** alongside the distance, by carrying `h` out of the function. That is how a blended object gets two materials with a smooth transition between them, and Module G uses it.

## Common mistakes

- **Blending with `mix` instead of `smin`.** `mix(a, b, 0.5)` is the average of two distances, which is not the distance to anything. It looks vaguely right and behaves badly everywhere.
- **Using a `k` that is large relative to the shapes.** The shapes stop being recognisable and the field stops being a useful bound. If a raymarcher starts missing surfaces, a large `k` is the first thing to check.
- **Expecting subtraction to be commutative.** `max(a, -b)` removes `b` from `a`. The other order removes `a` from `b`.
- **Chaining `smin` over many shapes with the polynomial form** and expecting the result to be independent of order. Use the exponential form if you need that.
- **Offsetting after a `max`.** The field is a poor bound there, so `combine(a, b) - r` gives a shape with a wrong-looking rounding at the seam.
- **Forgetting that `k` has units.** A `k` of 0.1 is enormous on a shape of radius 0.05.

## Exercise

Build a shader that draws a single object made from at least four primitives, using smooth union for two of them, subtraction for one, and smooth subtraction for one.

Requirements: exactly one `float` input named `blend` controls every smooth operator in the shader; a `point2D` input moves one primitive; and the object must still read as one object when `blend` is at its minimum and at its maximum.

**Success criterion:** at `blend` near zero the object has visible creases and at `blend` at its maximum it is a single smooth mass, with nothing popping or disappearing in between. If a primitive vanishes at high `blend`, it is smaller than `k` and the shape needs rebalancing rather than the code needing fixing.

## Going further

- [Inigo Quilez, smooth minimum](https://iquilezles.org/articles/smin/), for the three forms and their costs.
- [Inigo Quilez, distance function operators](https://iquilezles.org/articles/distfunctions/), for elongation, twisting, and the rest of the family.
- [Media Molecule's Dreams talk](https://www.mediamolecule.com/blog), for what happens when this idea becomes a whole authoring tool.
