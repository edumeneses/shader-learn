---
layout: default
title: "Unit 03: Coordinates, aspect ratio, and why the circle was an ellipse"
description: "The first bug everyone hits, its two fixes, and the coordinate convention the rest of the course uses. Also polar coordinates, which are one line and change what is easy."
parent: Units
nav_order: 3
unit: "03"
permalink: /learn/03-coordinates.html
reading_time: "12 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 03: Coordinates, aspect ratio, and why the circle was an ellipse

{% include unit_meta.html %}

> **Before this unit** read [Unit 02]({{ site.baseurl }}/learn/02-first-fragment-shader.html).
>
> **You will need** the shader you wrote for Unit 02's exercise, or the player below.
>
> **You will build** the coordinate setup the whole rest of the course uses, in one line, plus its polar variant.

## Why this matters

You will write your first circle and it will be an ellipse. Everybody does. The cause is that `isf_FragNormCoord` runs from 0 to 1 in both directions on a screen that is not square, so a step of 0.1 in `x` covers more of the screen than a step of 0.1 in `y`, and a set of points at equal distance from a centre is therefore stretched.

The fix is one line, and the reason to spend a whole unit on it is that the line is a decision, not a workaround. Which coordinate space you choose determines whether a shape is resolution independent, whether it survives being shown on a different screen, and whether your numbers mean anything you can reason about. A shader written in a badly chosen space works on your monitor and nowhere else, which in this field means it works until the show.

## The idea

**Normalised device coordinates run 0 to 1, and are not square.** That is the raw input. It is the right space for reading a texture, because a texture's own coordinates run 0 to 1 too, and it is the wrong space for measuring a distance.

**A square space is centred and divided by one number.** The line is:

```glsl
vec2 p = (isf_FragNormCoord - 0.5) * RENDERSIZE / RENDERSIZE.y;
```

Subtracting 0.5 puts the origin in the middle, which is what you want, because every shape you are about to write is easiest to describe around a centre. Multiplying by `RENDERSIZE` and dividing by `RENDERSIZE.y` divides **both** axes by the height, so one unit means the same distance in both directions. After it, `y` runs from -0.5 to 0.5 and `x` runs further on a wide screen, which is correct: the screen really is wider than it is tall, and a shape near the edge of a 16:9 frame is genuinely further out than a shape at the same number on a square one.

**The alternative is to apply the aspect ratio only where you measure.** `p = (uv - 0.5) * vec2(RENDERSIZE.x / RENDERSIZE.y, 1.0)` keeps the picture in roughly a 0-to-1 range and is common in published shaders. It is correct, and it is easier to forget: the moment you measure a distance in an expression that skipped the correction, the ellipse comes back. This course uses the first form everywhere.

**Dividing by height rather than by width is a convention, not a rule.** Divide by height and a shape's vertical size is stable while the frame gets wider; divide by width and the opposite. Vertical stability is the useful one for composition, and it is what Shadertoy examples almost always use, so it is what this course uses too. Pick one and never mix them in a project.

**Polar coordinates are one line and change what is easy.**

```glsl
float r = length(p);          // distance from the origin
float a = atan(p.y, p.x);     // angle, in radians, from -PI to PI
```

Anything that radiates, spirals, or repeats around a centre is trivial in polar and awkward in Cartesian. `a` has a seam where it wraps from `PI` to `-PI`, along the negative `x` axis, and that seam is visible in any pattern that uses the angle without dividing by `TAU` and taking `fract`. Unit 10 makes both of these into tools.

**Resolution independence has a precise meaning.** A shader is resolution independent when doubling `RENDERSIZE` produces the same picture with more detail, rather than a different picture. Working in the centred, height-divided space gets you most of the way there; the remaining trap is any constant expressed in pixels, and the only correct one in this course is the antialiasing width in [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html), which is measured with `fwidth` rather than guessed.

## Build it

The player runs the same circle in all three spaces, over a grid drawn in the same space, so the distortion is measurable rather than a matter of opinion. Step **Coordinate space** from 0 to 2.

{% include shader.html id="03-aspect" height="360" pointer="focus" caption="Space 0 is the mistake: the circle is an ellipse and the grid cells are rectangles. Spaces 1 and 2 are the two fixes. Drag the canvas to move the focus, and note that in space 0 the focus also moves at different speeds in x and y." %}

1. **Start at space 0.** The circle is an ellipse and the grid cells are wider than they are tall. The grid is the honest part: it shows that the space itself is stretched, so the shape is not the thing that is wrong.
2. **Go to space 1.** Circle, square cells. Note what happened to the extent: the grid now runs off both sides, because `x` covers more than one unit on a wide frame. That is the correct behaviour and it is the thing people mistake for a bug.
3. **Go to space 2.** The same circle by a different route. Look at the source and count how many expressions had to know about the aspect ratio; that count is why this course prefers space 1.
4. **Resize the window.** In space 0 the shape changes; in spaces 1 and 2 it does not. If you are reading on a phone, rotate it.
5. **Take the grid to zero and back.** The grid is a debugging overlay, and it is worth keeping one in your own shaders while you build them. It costs three lines and it turns "that looks wrong" into "that cell is 1.3 times as wide as it is tall".
6. **Now convert your Unit 02 exercise.** Replace its `uv` with the centred space and see what happens to the `tilt` control. It should now rotate the gradient about the centre of the picture rather than about the bottom left corner, without you changing the rotation arithmetic at all.

## Look at these

{% include toy.html id="4dfGzs" title="Voxel Edges" by="Inigo Quilez" note="The first four lines are the coordinate setup, in the form almost every Shadertoy uses." %}
{% include toy.html id="Xds3zN" title="Raymarching primitives" by="Inigo Quilez" note="Same setup, then everything Module G will teach. Read only the first block for now." %}

## Common mistakes

- **Correcting the aspect in the shape but not in the centre.** The circle is round and moves diagonally when you drag one axis. Both the point and the shape must live in the same space, which is why the player converts `focus` alongside `p`.
- **Dividing by `RENDERSIZE.x` in one shader and `RENDERSIZE.y` in another** in the same project. Both are correct; mixing them means a shape that is stable vertically in one process and horizontally in the next, and the two will never composite predictably.
- **Baking pixel constants.** `p * 400.0` looks fine at the resolution you wrote it and is wrong everywhere else. If a number needs to be in pixels, derive it from `RENDERSIZE`.
- **Forgetting the seam in `atan`.** A pattern built on the raw angle has a hard line along the negative `x` axis. Divide by `TAU` first, and the seam becomes a `fract` boundary you can control.
- **Using `atan(y/x)` instead of `atan(y, x)`.** The one-argument form loses the quadrant, so half the screen is wrong, and it is wrong in a way that looks like a mirroring bug rather than a maths one.

## Exercise

Build a shader with a `point2D` input named `origin` and a `float` input named `arms`, which draws a set of straight rays radiating from `origin`, evenly spaced, with `arms` controlling how many.

Requirements: it must use the centred, height-divided space; the rays must stay the same width in the middle of the picture at any resolution; and there must be no visible seam along any single direction.

**Success criterion:** at `arms` = 6 you count six rays, at `arms` = 7 you count seven, and rotating the picture by driving `origin` in a circle produces no jump anywhere. If you see a seam, you used the angle without wrapping it; if the rays flicker at the edges, you have met aliasing, which is [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html).

## Going further

- [Inigo Quilez, on the coordinate conventions](https://iquilezles.org/articles/), used across all of his articles.
- [The Book of Shaders, chapter 5](https://thebookofshaders.com/05/), for shaping functions in this space.
- [ISF's `isf_FragNormCoord`]({{ site.isf_baseurl }}/), and why the format supplies it rather than letting you read `gl_FragCoord`.
