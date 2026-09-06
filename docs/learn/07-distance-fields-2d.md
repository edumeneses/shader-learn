---
layout: default
title: "Unit 07: Signed distance fields in two dimensions"
description: "A field that answers how far, not just whether, and what that extra information buys: offsets, outlines, glows, and everything in Module G."
parent: Units
nav_order: 7
unit: "07"
permalink: /learn/07-distance-fields-2d.html
reading_time: "14 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 07: Signed distance fields in two dimensions

{% include unit_meta.html %}

> **Before this unit** read [Unit 06]({{ site.baseurl }}/learn/06-step-and-smoothstep.html).
>
> **You will need** the player below, and about half an hour to build shapes rather than read about them.
>
> **You will build** six distance functions, and the habit of looking at the whole field rather than only at where it crosses zero.

## Why this matters

Up to now a shape has been a test: this pixel is inside, that one is not. A signed distance field answers a larger question, "how far is this pixel from the boundary, and which side is it on", and the extra information turns out to be worth almost everything in shader art.

With a distance you can grow a shape by subtracting a constant, outline it by taking the absolute value, glow around it by exponentiating it, blend two shapes with no seam, repeat it infinitely for free, and, in three dimensions, march a ray towards it in steps you know are safe. None of those is possible from a yes-or-no answer. All of them are one or two lines from a distance.

This is the single most productive idea in the field, and it is why Module C is the longest module in Phase 1.

## The idea

**A signed distance function takes a point and returns a number.** Negative inside the shape, zero exactly on the boundary, positive outside, and its magnitude is the distance to the nearest boundary point. A circle of radius `r` centred on the origin is:

```glsl
float sdCircle(vec2 p, float r) {
    return length(p) - r;
}
```

Read it as: the distance from the origin, minus how far the boundary is. At the boundary `length(p)` equals `r` and the result is zero. That is the whole idea, and every other function in this unit is a more careful version of it.

**Exactness matters.** A field is *exact* when its value really is the distance, and *approximate* or *bounded* when it only has the right sign and never over-estimates. You can threshold either one, so approximate fields look fine at first. Everything else needs exactness: an offset by a constant only rounds correctly if the field is exact, and a raymarcher that over-estimates will step through a surface. This course notes which functions are exact and which are not.

**`abs` folds the plane.** A shape symmetric about both axes only has to be reasoned about in one quadrant, so `p = abs(p)` reduces the problem by a factor of four before you start. The box is the canonical example:

```glsl
float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}
```

`d` is how far the point is past the box's half-extent in each axis, negative when it is inside that axis's slab. Outside, at least one component is positive; `max(d, 0.0)` keeps only the axes the point is genuinely past, and `length` measures the corner correctly. Inside, both components are negative, the first term is zero, and `max(d.x, d.y)` is the distance to the nearest face, which is negative. Two terms, one for each region, and only one is ever non-zero.

**Rounding is subtraction.** `sdBox(p, b) - r` is a box grown by `r` in every direction, which is a box with rounded corners. This is not an approximation; it is exactly what an offset means, and it costs one subtraction. Subtracting from any exact field rounds it, which is why almost nothing in this course draws a shape with a corner unless it wants one.

**Annulus is absolute value.** `abs(d) - w` turns a filled shape into a ring of half-width `w`, because the absolute value makes the boundary itself the new zero crossing. One operation, and it works on any field.

**A segment is a projection.** The distance to a line segment is the distance to the nearest point on it, found by projecting and clamping:

```glsl
float sdSegment(vec2 p, vec2 a, vec2 b, float r) {
    vec2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - r;
}
```

The `clamp` is what makes the ends round caps rather than an infinite line, and swapping it for a different range is how you get a capsule with different-sized ends.

**Do not derive these yourself.** Inigo Quilez has published exact functions for about seventy 2D shapes and about sixty 3D ones, each with a derivation. Deriving a new one is a genuine piece of geometry and takes a while; using his is what everyone in the field actually does. Read them, understand two or three, and look the rest up.

## Build it

The player draws six shapes and can show either the shape or the whole field. The field view is Quilez's: warm outside, cool inside, fading with distance, with isolines at equal distance and a white line exactly on zero.

{% include shader.html id="07-sdf-2d" height="420" pointer="origin" caption="Start on the field view. The isolines are a contour map of the distance, and every technique in this module is a way of reading them. Switch to the shape view to see how much of that information a threshold throws away." %}

1. **Stay on the field view for the whole unit.** The shape view is there to remind you what you would normally see.
2. **Look at the isolines around the circle.** They are concentric and evenly spaced, because the field is exact: one isoline per unit of distance, everywhere.
3. **Switch to the box.** Outside a face the isolines are parallel to it; outside a corner they are circular arcs centred on the corner. That is the two-term structure of `sdBox` made visible, and it is the fastest way to understand the function.
4. **Switch to the rounded box and take Round to zero and back.** Watch the isolines: they do not change shape at all, only which one is drawn as the boundary. Rounding really is just picking a different contour.
5. **Switch to the segment and drag both points.** The Handle control moves the far end. Note the caps, and note that the isolines around them are circular while those along the middle are parallel.
6. **Turn Isolines to zero, then to 300.** At zero you see only the smooth falloff, which is what a glow is. At 300 the lines get finer than a pixel near the shape and start to alias, which is [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html) arriving early.
7. **Look at the triangle and the hexagon.** Both use a fold, and both are worth reading once without expecting to be able to write them. The hexagon's `k` constant is the direction of one of its edges; the triangle's is the square root of three.

## Look at these

{% include toy.html id="4dfXDn" title="2D distance functions" by="Inigo Quilez" note="The reference. Every function in this unit is from here, and there are sixty more." %}
{% include toy.html id="3ltSW2" title="Distance to a circle, derived" by="Inigo Quilez" note="Fifteen lines with the derivation as comments; the best single page on why these work." %}

Quilez's [2D distance functions article](https://iquilezles.org/articles/distfunctions2d/) is the page to keep open for the rest of this course.

## What the extra information buys

Each of these is one line on top of a field `d`, and none of them is available from a boolean:

- **Outline**: `abs(d) - w`.
- **Grow or shrink**: `d - r`, which also rounds corners.
- **Glow**: `exp(-k * max(d, 0.0))`, a falloff with no shape of its own.
- **Bands**: `cos(d * n)`, which is what the isolines in the player are.
- **Antialiasing**: `smoothstep(fwidth(d), -fwidth(d), d)`, from [Unit 06]({{ site.baseurl }}/learn/06-step-and-smoothstep.html).
- **Snap to the surface**: step by `d` along the field's gradient, which is what the magnifier in Unit 06 did and what a whole raymarcher is.

## Common mistakes

- **Writing a field that is not exact and then offsetting it.** `length(p * vec2(2.0, 1.0)) - r` looks like an ellipse and is not a distance to one; subtracting from it produces a shape nobody intended. There is an exact ellipse function, and it is long, which tells you something.
- **Scaling the coordinate without scaling the result.** `sdCircle(p / s, r)` is the right shape with the wrong distances. [Unit 09]({{ site.baseurl }}/learn/09-transforms.html) is about this.
- **Testing `d == 0.0`.** Floating point almost never lands exactly on a boundary. Test a band: `abs(d) < w`.
- **Using `min` on two fields and expecting the result to stay exact.** It stays a valid bound and stops being exact in the region between the shapes. [Unit 08]({{ site.baseurl }}/learn/08-combining-fields.html) says when that matters.
- **Deriving a new shape when one exists.** Look it up first. Always.

## Exercise

Build a shader with a `long` input that selects between three shapes you write yourself, and a `float` input named `outline` which, when it is greater than zero, draws the shape as a ring of that width instead of filled.

Requirements: use the field view from the player, so you can see what you are doing; the outline must work identically for all three shapes without any per-shape code; and the shapes must be exact, so that a second `float` input named `grow` rounds their corners as it rises.

**Success criterion:** setting `outline` to 0.02 gives a ring of the same width everywhere on all three shapes, including at corners, and raising `grow` rounds the corners of the box without changing the ring's width. If the ring gets thicker at corners, your field is not exact there; if `grow` moves the shape rather than growing it, you subtracted from the coordinate instead of from the distance.

## Going further

- [Inigo Quilez, 2D distance functions](https://iquilezles.org/articles/distfunctions2d/), the reference.
- [Inigo Quilez, distance functions to interiors](https://iquilezles.org/articles/interiordistance/), for what "signed" costs to compute properly.
- [Ronja's shader tutorials, 2D SDF](https://www.ronja-tutorials.com/), for a slower walk through the same material.
