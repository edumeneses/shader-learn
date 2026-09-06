---
layout: default
title: "Unit 09: Transforms, and doing them backwards"
description: "You cannot move a shape in a shader. You move the space it is measured in, which means every transform you write is the inverse of the one you see, and scaling needs a correction most people forget."
parent: Units
nav_order: 9
unit: "09"
permalink: /learn/09-transforms.html
reading_time: "12 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 09: Transforms, and doing them backwards

{% include unit_meta.html %}

> **Before this unit** read [Unit 07]({{ site.baseurl }}/learn/07-distance-fields-2d.html).
>
> **You will need** the player below, in its field view.
>
> **You will build** the inverse-transform habit, and the scale correction that keeps a field honest.

## Why this matters

There is no shape to move. Your function is asked, at one point, how far away the boundary is, and it answers with arithmetic. To make the shape appear somewhere else you have to change the question, which means transforming the **point**, not the shape.

The consequence catches everyone at least once: every transform you write is the inverse of the one you see. To move a shape right by 0.3 you subtract 0.3. To rotate it by twenty degrees you rotate the point by minus twenty. To make it twice as big you halve the coordinate. Once that clicks it is easy, and until it does, half your shapes go the wrong way.

Scaling has a second half that is easier to miss and does more damage: dividing the coordinate changes what a unit of distance means, so the number the function returns is no longer a distance. The shape looks right and the field lies, which stays invisible until you offset it, glow around it, or march a ray through it.

## The idea

**Translate by subtracting.** `sdCircle(p - t, r)` puts the circle at `t`. Read it as "where would this pixel be if the circle were at the origin", which is the question the function knows how to answer.

**Rotate by the inverse rotation.**

```glsl
mat2 rot(float a) {
    float c = cos(a), s = sin(a);
    return mat2(c, -s, s, c);
}
```

`shape(rot(-a) * p)` rotates the shape by `+a`. Rotation preserves distance, so nothing needs correcting afterwards. This is the one transform with no catch.

**Scale by dividing, and multiply the result back.**

```glsl
float d = shape(p / s) * s;
```

Dividing the coordinate by `s` shrinks the space, so a step that was one unit is now `1/s` units, and every distance measured in it comes out `s` times too small. Multiplying the result by `s` puts it back. Omit it and the shape is correct and the field is wrong by a constant factor.

**Order matters, and it reads backwards.** `shape(rot(-a) * (p - t) / s)` scales, then rotates, then translates, as seen on screen: the operations apply to the point in the order written, and the effect on the shape is the reverse. Getting a compound transform right is mostly a matter of writing it once carefully and then not touching it.

**Non-uniform scale cannot be corrected by one multiplication.** `shape(p / vec2(sx, sy))` stretches the space differently along different directions, and there is no single number that converts the result back into a distance, because the answer depends on which way you are looking. The conservative fix is to multiply by `min(sx, sy)`, which under-estimates rather than over-estimates. That is exactly what a raymarcher needs, since under-estimating only costs steps while over-estimating steps through surfaces.

If you need a genuinely correct stretched shape, use the exact function for it. An exact ellipse function exists, it is about fifteen lines, and its length is the honest measure of how much a non-uniform scale costs.

**Shear, twist, and bend are the same idea and worse.** Any non-rigid transform breaks the metric, and the standard answer is the same: use it, know the field is approximate, and divide by a bound on how much the transform can stretch space. Quilez's article on the subject is the reference.

## Build it

The player draws a cross, so that rotation is unmistakable, in the field view, with three modes: the correct scale handling, the common mistake, and a non-uniform stretch.

{% include shader.html id="09-transforms" height="420" pointer="origin" caption="Drag the canvas to translate. Watch the isolines, not the outline: in mode 1 the outline is correct at every scale and the isolines are wrong, which is the bug this unit exists for." %}

1. **Start in mode 0 and drag the canvas.** The shape follows the pointer. Look at the source: the code subtracts. That subtraction is the whole of translation.
2. **Turn Rotate.** The shape turns one way; the code rotates by `-turn`. Change the sign in your head and confirm you can predict which way it will go.
3. **Stay in mode 0 and sweep Scale from 0.2 to 3.** The isolines stay evenly spaced at their original density. The field is still a distance.
4. **Switch to mode 1 and sweep Scale again.** The shape is identical, and the isolines bunch up or spread out. At scale 3 the field says a point is three times nearer than it is.
5. **Ask why that matters.** Everything in [Unit 07]({{ site.baseurl }}/learn/07-distance-fields-2d.html)'s list of one-line techniques reads the magnitude: an outline of width 0.02 becomes an outline of width 0.006, a glow tightens, and in Module G a ray marches three times further than it should and lands inside the object.
6. **Switch to mode 2 and sweep Stretch.** The shape stretches, as asked. The isolines are now wrong by a factor that depends on direction: dense along one axis, sparse along the other, with no single number that could fix it.
7. **In mode 2, look at the corners.** The `min(sx, sy)` correction makes the field safe rather than correct, and the corners are where the difference is largest.

## Look at these

{% include toy.html id="Xtd3z7" title="Distance function transformations" by="Inigo Quilez" note="Rotation, elongation, rounding, and onioning, each in one line." %}
{% include toy.html id="3syGzz" title="Twisted torus" by="Shadertoy community" note="A domain twist, and what it does to the field; note the step size divisor." %}

## Elongation, and the transform that is exact

There is one non-uniform operation that keeps the field exact, and it is worth knowing because it does most of what people reach for a stretch to do. **Elongation** slides a shape apart along an axis and fills the gap, rather than scaling it:

```glsl
vec2 q = p - clamp(p, -h, h);
float d = shape(q);
```

A circle elongated becomes a capsule with round ends and straight sides, and the field stays exact. A circle scaled becomes an ellipse with an approximate field. When you want a longer shape rather than a distorted one, elongation is the right tool and it is cheaper.

## Common mistakes

- **Adding instead of subtracting for translation**, so the shape moves away from the pointer. If your shape moves the wrong way, this is why.
- **Rotating by `+a` and wondering why the rotation is mirrored.** Use `-a` in the matrix, or transpose it, which for a rotation is the same thing.
- **Forgetting the `* s` after a scale.** The most common bug in this unit, and the most invisible.
- **Scaling `k` in a `smin` when the scene scales.** `k` is a distance. If everything in the scene is divided by `s`, `k` has to be too, or a fillet that was subtle becomes enormous.
- **Applying a transform to a field instead of to a point.** `sdCircle(p, r) * 2.0` is not a bigger circle; it is the same circle with a field that lies.
- **Building a transform stack with matrices out of habit.** Two dimensions and a handful of operations means the arithmetic is usually clearer written out. Save the matrices for Module G, where they earn their keep.

## Exercise

Build a shader with one shape and four inputs: a `point2D` for position, a `float` for rotation in turns, a `float` for uniform scale, and a `float` named `outline` that draws the shape as a ring of that width.

Requirements: the outline must be exactly `outline` wide, in screen terms, at every scale from 0.2 to 3.

**Success criterion:** with `outline` at 0.02, measure the ring's width on screen at scale 0.2 and at scale 3. They must match. If the ring gets thinner as the shape gets bigger, you have found the missing multiplication, which is the point of the exercise. Then add a second `float` that stretches one axis, and write down in a comment what happens to the ring's width and why nothing you can multiply by will fix it.

## Going further

- [Inigo Quilez, distance function transformations](https://iquilezles.org/articles/distfunctions/), including elongation, rounding, and onioning.
- [Inigo Quilez, ellipse distance](https://iquilezles.org/articles/ellipsedist/), for what an exact non-uniform shape actually costs.
- [Inigo Quilez, on Lipschitz bounds](https://iquilezles.org/articles/), for the general rule about dividing by how much a transform can stretch space.
