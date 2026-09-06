---
layout: default
title: "Unit 10: Repetition, tiling, and polar space"
description: "Fold the plane into one cell and a shape written once appears everywhere, for free, forever. Then the trap: the fold is only correct while the shape fits in its cell."
parent: Units
nav_order: 10
unit: "10"
permalink: /learn/10-repetition.html
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 10: Repetition, tiling, and polar space

{% include unit_meta.html %}

> **Before this unit** read [Unit 09]({{ site.baseurl }}/learn/09-transforms.html).
>
> **You will need** the player below.
>
> **You will build** three kinds of repetition, and a clear idea of when each one lies to you.

## Why this matters

This is the cheapest technique in shader art and it is close to free. Fold the coordinate into a single cell before you measure, and one shape appears in every cell of an infinite grid at the cost of one modulo. A thousand objects, one distance function, no loop, no memory, no per-object work at all.

It is also where beginners produce their first genuinely impressive picture, because a simple shape repeated with a little variation reads as complexity. And it has one trap, which is worth understanding rather than working around: the fold assumes the nearest copy of the shape is the one in your own cell, and when a shape is larger than its cell that assumption is false, so the field lies and the shapes get cut off at the cell boundaries.

## The idea

**Infinite repetition folds the coordinate.**

```glsl
vec2 q = mod(p + 0.5 * c, c) - 0.5 * c;
float d = shape(q);
```

`mod(p, c)` maps everything into a cell running 0 to `c`; the two half-cell offsets recentre it so the cell runs from `-c/2` to `+c/2`, which is where a shape centred on the origin expects to be. Forgetting them puts the shape in the corner of every cell, which is a very recognisable bug.

**The trap.** The fold measures the distance to the copy in this cell. If the shape is wider than the cell, a neighbouring copy is nearer, and the field returns the wrong, larger value. Visually the shapes are chopped along the cell boundaries. The rule is simple: **the shape must fit inside its cell, with room for whatever you do to the field afterwards**. A `smin` with a large `k`, or a glow, needs the cell to be bigger still.

The general fix is to test the neighbouring cells as well, taking the minimum over a 3 by 3 block, which costs nine evaluations instead of one and is what Voronoi in [Unit 16]({{ site.baseurl }}/learn/16-voronoi.html) has to do. For most work, making the cell large enough is the right answer.

**Limited repetition clamps the index.**

```glsl
vec2 id = clamp(round(p / c), -limit, limit);
float d = shape(p - c * id);
```

`round` rather than `mod`, because rounding gives you the cell **index**, and an index can be clamped. Beyond the limit, every point maps to the same edge cell, so the grid stops rather than continuing. Clamping the position instead of the index gives you a stretched last cell, which is the usual failed attempt.

**Polar repetition folds the angle.**

```glsl
float r = length(p);
float a = atan(p.y, p.x);
float sector = TAU / n;
a = mod(a + 0.5 * sector, sector) - 0.5 * sector;
vec2 q = vec2(cos(a), sin(a)) * r;
```

Everything that radiates is this: petals, spokes, gears, mandalas. The same trap applies, and it is worse here because the sector's width in real distance grows with radius: a shape that fits near the rim is cut near the centre. When `n` is not an integer the fold does not close and you get a seam, which is occasionally what you want.

**Repetition composes with everything.** Fold, then transform inside the cell. Fold twice at different scales. Fold, then use the cell index to vary the shape, which is where this stops looking mechanical: `id` from the limited form, or `floor(p / c)` from the infinite one, is a per-cell number you can hash in [Unit 12]({{ site.baseurl }}/learn/12-hashes.html) to give every copy its own size, rotation, and colour.

**Mirrored repetition is `abs` after the fold.** It halves the visible seam count and produces a kaleidoscope. Doing it before the fold gives a very different and also useful result, and trying both is quicker than reasoning about it.

## Build it

{% include shader.html id="10-repetition" height="420" pointer="origin" caption="Four modes. Drag the canvas to move the whole lattice, which is a good way to check that a fold is centred: an off-centre fold makes the shapes slide within their cells as you drag." %}

1. **Start on none**, to see the single shape you are about to multiply.
2. **Switch to the infinite grid.** One shape, everywhere, at no extra cost. Drag the canvas and note that the whole lattice moves rigidly; if the shapes slid inside their cells instead, the fold would be missing its recentring.
3. **Now find the trap.** Raise Shape size, or lower Cell size, until the shape is bigger than its cell. The shapes are chopped square. Switch to the field view and watch the isolines: they become discontinuous at every cell boundary, which is the field lying in the most visible possible way.
4. **Switch to limited repetition and sweep Limit.** The grid grows and stops. Set Limit to zero for exactly one shape, which is a useful sanity check that the index arithmetic is right.
5. **Switch to polar and sweep Arms.** Note that at non-integer values the fold does not close: there is a seam along one direction where the last sector is the wrong width.
6. **In polar, take Cell size down towards zero.** The shapes converge on the centre and get cut, because the sector is narrower than the shape there. This is the same trap with a radial cause.
7. **Turn Drift up in any mode.** The shape rotates inside its cell rather than the lattice rotating, because the rotation is applied after the fold. Move it before the fold in your own copy and see the difference; both are useful and they are not the same.

## Look at these

{% include toy.html id="MsSGRh" title="Domain repetition" by="Inigo Quilez" note="The reference implementation, including the corrected version that checks neighbouring cells." %}
{% include toy.html id="4sX3Rn" title="Mandelbulb" by="Inigo Quilez" note="Repetition taken to its conclusion: a fold applied recursively is a fractal." %}
{% include toy.html id="Ml2GWy" title="Truchet tiles" by="Inigo Quilez" note="A fold plus a per-cell hash, which is the technique from step 7 of this unit and Unit 12 together." %}

Quilez's [domain repetition article](https://iquilezles.org/articles/sdfrepetition/) covers the correct neighbour-checking version in full.

## Repetition as a fractal

Fold, scale, fold again, scale again. Each round adds detail at half the size for the same cost as the last, and after five or six rounds you have a fractal. That is essentially all a Mandelbulb is, and it is why fractals are so common in raymarched work: they are not expensive, they are just a loop around a fold.

The catch is the field. Each fold degrades the distance estimate a little, and each scale multiplies the error, so a deep fractal's field is a bound rather than a distance and a raymarcher through it needs smaller steps. Module G returns to this.

## Common mistakes

- **Forgetting the half-cell offsets**, so every shape sits in the corner of its cell.
- **A shape larger than its cell**, giving chopped shapes and a discontinuous field. Check this first whenever a repeated pattern looks wrong.
- **Using `mod` when you need the cell index.** `mod` throws the index away. Use `round` or `floor` and keep it; you will want it for variation.
- **Clamping the position rather than the index** in limited repetition, which stretches the outermost cell instead of stopping the grid.
- **Non-integer `n` in polar repetition** producing a seam you did not ask for.
- **Applying a rotation before the fold when you meant after**, or the reverse. Both are legitimate and they look completely different.
- **Assuming `mod` handles negatives the way you expect.** GLSL's `mod` returns a result with the sign of the divisor, which is what you want here, unlike C's `%`. It is worth knowing that they differ.

## Exercise

Build a shader with an infinite grid of shapes in which each cell's shape is rotated by an amount that depends on its position in the grid, so the pattern reads as a wave passing through a field of objects rather than as a rigid lattice.

Requirements: a `float` input named `wave` controls how fast the rotation changes across cells; a `float` named `cell` controls the cell size; and there must be no chopping at any combination of the two, which means the shape has to be derived from the cell size rather than set independently.

**Success criterion:** with `wave` at zero every shape has the same orientation; as `wave` rises, a diagonal pattern of orientations appears; and at every cell size from smallest to largest, no shape is cut by a cell boundary. If shapes are chopped at small cell sizes, the shape size is not derived from the cell size.

## Going further

- [Inigo Quilez, domain repetition](https://iquilezles.org/articles/sdfrepetition/), including the neighbour-checking form.
- [Inigo Quilez, on Truchet patterns](https://iquilezles.org/articles/), for repetition plus per-cell variation.
- [The Book of Shaders, chapter 9](https://thebookofshaders.com/09/), on patterns and tiling from the other direction.
