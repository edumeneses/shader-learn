---
layout: default
title: "Unit 13: Value noise, gradient noise, and how to read them"
description: "Both are a random number on a lattice, interpolated. The difference is what is stored at each lattice point, and it decides everything about how the result looks."
parent: Units
nav_order: 14
unit: "13"
permalink: /learn/13-noise.html
reading_time: "14 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 13: Value noise, gradient noise, and how to read them

{% include unit_meta.html %}

> **Before this unit** read [Unit 12]({{ site.baseurl }}/learn/12-hashes.html).
>
> **You will need** the player below, with the lattice overlay on.
>
> **You will build** both kinds of noise, and the ability to recognise which one a shader you are reading used.

## Why this matters

A hash gives you a different number at every point, which is static. Noise gives you a number that varies **smoothly**, so nearby points are similar and distant ones are not. That single property is what makes it look like something: clouds, marble, terrain, smoke, rust, water.

Every noise function in common use is built the same way. Put a random value on a regular grid, then interpolate between grid points. What varies is what you store at each grid point and how you interpolate, and those two choices produce results that look entirely different despite nearly identical code.

Recognising which one a shader used is a genuinely useful skill, because it tells you what its author was reaching for. Value noise looks blobby and cheap; gradient noise looks like a natural surface; and there are three or four visual tells that separate them at a glance.

## The idea

**Both kinds share a skeleton.**

```glsl
vec2 i = floor(p);   // which cell
vec2 f = fract(p);   // where in the cell
vec2 u = fade(f);    // the interpolation curve
// fetch four corner contributions, then bilinear mix by u
```

`floor` and `fract` are [Unit 10]({{ site.baseurl }}/learn/10-repetition.html)'s fold with the index kept, and the four corners are a 2 by 2 hash lookup. Everything else is the same in both.

**Value noise stores a number at each lattice point** and interpolates the four numbers. It is the simpler of the two, one hash per corner, and it has a characteristic look: the extremes of the field sit exactly on the lattice points, so the result is a grid of blobs. Once you have seen it you cannot unsee it, and at low density the grid is obvious even without an overlay.

**Gradient noise stores a random direction at each lattice point** and, at each corner, takes the dot product of that direction with the offset from the corner to the sample point. Because the offset is zero at the corner, **the noise is exactly zero at every lattice point**. The extremes therefore fall *between* lattice points, and the result has no blobs and a much more organic character. This is Perlin noise, and it is what most people mean by "noise" without saying so.

**The interpolation curve matters more than it looks.** Straight linear interpolation leaves a kink in the derivative at every lattice line, and the eye sees kinks as a grid. Perlin's original curve, `3t² - 2t³`, which is `smoothstep`, has zero slope at both ends and removes the visible grid. His revised quintic, `6t⁵ - 15t⁴ + 10t³`, additionally has zero **second** derivative there, which matters as soon as you differentiate the noise. Fractal Brownian motion in [Unit 14]({{ site.baseurl }}/learn/14-fbm.html) effectively does, and so does any normal mapping, so the quintic is the one to use.

**Simplex noise** replaces the square lattice with a triangular one, which needs three corners instead of four in two dimensions and four instead of eight in three. It scales better with dimension and has less directional bias. It is also more complicated, was patent-encumbered in three dimensions and above until 2022, and for two-dimensional work the gain over gradient noise is small. This course uses gradient noise and says where simplex would be better.

**Noise is not random.** It is a fixed function of position. The whole infinite field exists whether you look at it or not, and moving through it is what animates it: add a constant to the input and the field slides, which is what the Drift control does.

**Noise does not tile by default.** For a texture that must repeat, either hash the lattice index modulo the tile size, which makes the noise periodic, or use a higher-dimensional noise sampled on a closed loop. [Milestone P2]({{ site.baseurl }}/learn/p2-living-surface.html) needs one of these.

## Build it

{% include shader.html id="13-noise" height="420" pointer="none" caption="Value noise on the left, gradient noise on the right, on the same lattice with the same hash. Leave the lattice overlay on for the whole unit: every feature in both pictures is anchored to it, and that is the least obvious and most useful fact about this family of functions." %}

1. **Start on both, split, with the lattice on and Density low.** Left is value noise, right is gradient noise, and the difference is immediate: the left has bright and dark patches centred on lattice intersections, the right has features straddling them.
2. **Look for zero on the right.** Gradient noise is exactly the mid grey at every lattice point, always. Once you can see that, you can identify gradient noise in any shader on Shadertoy from a screenshot.
3. **Set Interpolation to linear.** Both sides develop a visible grid: hard creases along every lattice line. This is why nobody uses linear, and it is worth seeing so that when a shader mysteriously shows a grid you check the curve first.
4. **Compare smoothstep and quintic.** At this density they look nearly identical, which is honest: the difference is in the second derivative, and it shows up in [Unit 14]({{ site.baseurl }}/learn/14-fbm.html) when octaves are summed, not here.
5. **Raise Density.** More lattice cells in the same space, so finer features. Note that this is not "more detail": it is the *same* function at a different scale, with one feature size. Real surfaces have many, which is the entire point of the next unit.
6. **Raise Contrast.** A `smoothstep` on the noise value pushes it towards its extremes without clipping. This is how most shapes get cut out of noise: not by thresholding, which aliases, but by increasing contrast until the mid tones are thin.
7. **Turn Drift on and watch what moves.** The field slides past the window. Nothing is being generated per frame; you are panning across a function that was always there.
8. **Turn the lattice off and look at both sides again.** Value noise still reads as a grid of blobs. That tell survives the overlay being removed, and it is the reason gradient noise is worth its extra cost.

## Look at these

{% include toy.html id="XslGRr" title="Noise, 3D" by="Inigo Quilez" note="Gradient noise in three dimensions with the derivative computed analytically, which Unit 15 will want." %}
{% include toy.html id="4sfGzS" title="Value noise" by="Inigo Quilez" note="The cheap one, done properly, with the tell clearly visible at low density." %}
{% include toy.html id="Xd23Dh" title="Voronoise" by="Inigo Quilez" note="One function that morphs continuously between value noise, gradient noise, and Voronoi. Read it after Unit 16." %}

Quilez's [noise articles](https://iquilezles.org/articles/) cover the derivatives and the tiling variants.

## Which one to use

**Gradient noise** for anything that should look like a natural surface, which is most things. It is the default.

**Value noise** when you need many octaves and the cost matters, or when the blobbiness is the look you want. It is roughly half the instructions.

**Simplex noise** in three or four dimensions, where the corner count grows as 2ⁿ for a lattice and only as n+1 for a simplex. In two dimensions it is rarely worth the extra code.

**A texture** when the noise is expensive and static. A single texture read beats any of these, and a shader that samples a pre-generated noise texture is not cheating; it is what production renderers do. The reason this course computes it is that a computed field is infinite, resolution independent, and needs no asset, which matters when the shader has to travel as one file.

## Common mistakes

- **Linear interpolation**, giving a visible grid that looks like a bug in the hash.
- **Thresholding noise with `step`.** It aliases immediately. Raise the contrast with `smoothstep` and let the edge be soft, or use `fwidth` as in [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html).
- **Expecting noise to be centred on zero.** Value noise runs 0 to 1; gradient noise runs about -0.7 to 0.7 before remapping. Mixing the two conventions in one shader produces washed-out results that look like a contrast problem.
- **Animating by reseeding the hash.** That is static, not motion. Move through the field instead, or use a third dimension as time.
- **Assuming noise tiles.** It does not, and finding out at the point where the texture wraps is a bad moment.
- **Using a different hash for the two axes.** The lattice must be hashed consistently or neighbouring cells disagree about their shared corner, and the field tears.

## Exercise

Build a shader that renders gradient noise and gives it a `bool` input named `tile`. When `tile` is on, the noise must repeat exactly over a period set by a `float` input named `period`.

The technique: hash the lattice index modulo the period, so that the corner at index `period` is the same corner as the one at index 0.

**Success criterion:** with `tile` on and `period` at 4, panning the field with Drift shows the same pattern returning every four lattice cells, with no seam at the join. If there is a visible line at the wrap, your modulo is applied after the hash rather than before it.

## Going further

- [Inigo Quilez, on noise and its derivatives](https://iquilezles.org/articles/morenoise/), which [Unit 15]({{ site.baseurl }}/learn/15-domain-warping.html) builds on.
- [Ken Perlin, *Improving Noise*](https://mrl.cs.nyu.edu/~perlin/paper445.pdf), the two-page paper that introduced the quintic curve.
- [Stefan Gustavson, *Simplex noise demystified*](https://weber.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf), for the triangular-lattice version in full.
