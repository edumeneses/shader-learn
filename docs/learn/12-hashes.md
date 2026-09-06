---
layout: default
title: "Unit 12: Deterministic randomness without a texture"
description: "A shader cannot call random(). It hashes its coordinate instead, which gives repeatable randomness for free, and the popular one-line hash has failure modes worth knowing before you rely on it."
parent: Units
nav_order: 13
unit: "12"
permalink: /learn/12-hashes.html
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 12: Deterministic randomness without a texture

{% include unit_meta.html %}

> **Before this unit** read [Unit 10]({{ site.baseurl }}/learn/10-repetition.html).
>
> **You will need** the player below.
>
> **You will build** four hash functions, and a clear view of which one to reach for.

## Why this matters

A fragment shader has no random number generator. There is no state to hold a seed, no way to advance one, and no order in which two million invocations could take turns. What it has instead is better: a **hash**, a function that turns a coordinate into a number that looks random and is the same every time.

Same-every-time is the useful half. A shader that hashed differently each frame would produce television static. A shader that hashes its coordinate produces a fixed pattern that you can move through, zoom into, and animate deliberately, and that renders identically on every machine and at every resolution. Every procedural texture in Module D is built on this, and so is the grain in [Milestone P1]({{ site.baseurl }}/learn/p1-poster.html).

The catch is that the hash everybody uses, the one-line `fract(sin(...) * 43758.5453)`, is folklore rather than engineering. It is fine for most work, it has real failure modes, and knowing which is which saves a very confusing afternoon.

## The idea

**A hash is a pure function from a coordinate to a number in 0 to 1.** Given the same input it gives the same output, always, everywhere. Given nearby inputs it should give unrelated outputs, which is the property that makes it look random.

**The sine hash.**

```glsl
float hash21(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453123);
}
```

Project the input onto a direction with a dot product, take a sine to fold the result unpredictably, multiply by something large to spread it, keep the fractional part. The constants are not magic and are not derived from anything; they were chosen by someone around 2008 and copied ever since.

Its problems, in order of how likely you are to meet them:

- **`sin` is not specified precisely.** GLSL leaves the accuracy of transcendental functions to the implementation, so this hash returns *different numbers on different drivers*. A shader that looks right on your machine can look different on a phone, and there is no bug to find.
- **At large inputs it degenerates.** `sin` of a large number loses precision, so at high zoom or far from the origin the hash starts returning banded values instead of random ones. On `mediump` hardware this happens much sooner.
- **It has a grain.** The dot product projects onto one direction first, so points along the perpendicular line hash similarly. It is subtle and it is there.

**The fract-multiply hash**, Dave Hoskins's family, stays in the fractional part and stirs with multiplies and dot products. No transcendental, so no precision surprise, and it is cheaper. This is the right default for a shader that has to look the same everywhere without much thought.

**The integer hash** is what a hash function is supposed to be. Convert the coordinate to integers, mix the bits with shifts and multiplies until every input bit affects every output bit, convert back to a float. GLSL ES 3.00 has integer types and bitwise operators, so this is available and it is exact: identical on every conforming device, no grain, no bands, no correlation. It costs a handful of integer operations, which is nothing on any GPU made this decade.

Use it when the hash is doing real work: per-cell variation, jittered sampling, anything where a visible pattern would be a bug.

**Dimension in, dimension out.** You will want `hash11`, `hash21`, `hash22`, `hash31`, and `hash33` at various points. Naming them by the shape of their input and output, as Hoskins does, is the convention worth adopting; guessing which overload you have is not.

**Hashing an index, not a position.** In a repetition from [Unit 10]({{ site.baseurl }}/learn/10-repetition.html), hash the **cell index**, not the folded position. The index is constant across a cell, so every point in that cell gets the same random number, which is what makes each copy of the shape differ from its neighbours rather than every pixel differ from the one beside it.

## Build it

{% include shader.html id="12-hashes" height="440" pointer="none" caption="Three views. Per pixel is what a hash looks like raw; per cell is how it is actually used; and the correlation plot draws each cell's value against its neighbour's, where a bad hash draws lines instead of a cloud." %}

1. **Start on per cell, with the sine hash.** Every cell has its own value and neighbouring cells are unrelated. This is the picture you want.
2. **Switch to per pixel and wind Zoom up.** At zoom 1 it is white noise. Push it towards 400 and watch the sine hashes: the noise develops structure and eventually bands, because `sin` of a very large number is no longer accurate. The integer hash does not change at any zoom.
3. **Raise Offset with the sine hash selected, on the per-pixel view.** Same effect from the other direction: the hash degrades as the input gets large, which in practice means far from the origin.
4. **Switch to the correlation plot.** Each point is one cell's value against the next cell's. A good hash fills the square as an even cloud. Step through the four methods and look for structure: lines, clusters, or empty regions all mean the output is predictable from the input in a way you can see.
5. **Note what the plot does and does not prove.** On a desktop with 32-bit floats, the sine hashes scatter perfectly well here. Their real failure is device dependent, and a plot on your machine cannot show you what a phone will do. That is itself the argument for the integer hash: it is the only one whose behaviour you can predict without testing.
6. **Turn Reseed over time on.** The whole field changes twice a second, because the seed is part of the input. Nothing about the hash changed; you asked it a different question.
7. **Read `hashUint`.** Three xor-shifts and two multiplies. Each step spreads the influence of one bit across more of the word, and after five of them every input bit affects every output bit. That is the entire design principle.

## Look at these

{% include toy.html id="4djSRW" title="Hash without sine" by="Dave Hoskins" note="The reference collection: every input and output dimension, with costs. Bookmark it." %}
{% include toy.html id="XlGcRh" title="Integer hash comparison" by="Mark Jarzynski and Marc Olano" note="Accompanies their paper on hash functions for GPU rendering, which measured these properly rather than by eye." %}

## Blue noise, and why white noise is often wrong

The hashes here produce **white noise**: every value independent, all frequencies equally present. It is the right thing for a per-cell random number and the wrong thing for sampling.

When you use random numbers to choose *where* to sample, white noise clumps: some regions get several samples and others get none, and the clumping is visible as a mottled texture in the result. **Blue noise** has the same distribution with the clumps removed, and it looks dramatically better for the same number of samples. It cannot be computed cheaply from a coordinate, so in practice it comes from a small tiled texture, and Unit 21's blur and Unit 24's raymarcher both benefit from one.

For dithering, as in [Unit 04]({{ site.baseurl }}/learn/04-colour.html), the same argument applies: the Bayer matrix there is an ordered pattern, white noise would be worse, and blue noise would be better.

## Common mistakes

- **Hashing the folded position instead of the cell index**, so every pixel in a cell gets a different value and the pattern is static rather than variation.
- **Assuming the sine hash is portable.** It is not, and the failure is silent.
- **Using the same hash for two purposes with the same input.** Two calls with the same argument return the same number, so a size and a rotation driven by one hash are correlated. Offset the input, or use a hash with a wider output.
- **Trying to make a hash "more random" with bigger constants.** The problem is never the size of the constants.
- **Expecting `fract(sin(x))` to be uniform.** It is close and it is not, and near the extremes of `sin` it is measurably not.
- **Reaching for white noise where blue noise is wanted.** If a result looks mottled rather than grainy, this is why.

## Exercise

Take the infinite grid from [Unit 10]({{ site.baseurl }}/learn/10-repetition.html)'s exercise and give every cell its own size, its own rotation, and its own colour, all from hashes of the cell index.

Requirements: use one hash function; make the three values genuinely independent, which means you cannot call it three times with the same argument; and a `float` input named `variation` must scale all three from "identical to their neighbours" to "as different as the ranges allow".

**Success criterion:** at `variation` zero the grid is uniform, at maximum no two cells look alike, and there is no visible correlation between a cell's size and its colour. If large cells are consistently one colour, your three values came from the same number.

## Going further

- [Dave Hoskins, hash without sine](https://www.shadertoy.com/view/4djSRW), the collection to copy from.
- [Jarzynski and Olano, *Hash Functions for GPU Rendering*](https://jcgt.org/published/0009/03/02/), which tested these instead of guessing.
- [Alan Wolfe on blue noise](https://blog.demofox.org/), for what to use when a hash is choosing sample positions.
