---
layout: default
title: "Unit 16: Voronoi and cellular noise"
description: "Scatter points, and ask each pixel which one is nearest. That single question gives cells, cracks, scales, cobbles, and the only structure in Module D that is not smooth."
parent: Units
nav_order: 17
unit: "16"
permalink: /learn/16-voronoi.html
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 16: Voronoi and cellular noise

{% include unit_meta.html %}

> **Before this unit** read [Unit 12]({{ site.baseurl }}/learn/12-hashes.html) and [Unit 10]({{ site.baseurl }}/learn/10-repetition.html).
>
> **You will need** the player below, with the sites shown.
>
> **You will build** a Voronoi with correct borders, which is one more pass than most implementations bother with and is worth it.

## Why this matters

Everything in Module D so far has been smooth. Noise, fbm, and warped fbm are all continuous fields with no edges anywhere in them, and no amount of contrast will give them a genuine boundary; it will only give them a steep gradient.

Voronoi is the structure with edges. Scatter points across the plane, and colour each pixel by which point is nearest. The result is a tessellation of the plane into cells, and cells are everywhere in the physical world: cracked mud, giraffe hide, foam, cobblestones, reptile scales, crystal grains, dried paint. It is also the natural way to get *discrete identity* into a procedural texture, because every pixel can name the cell it belongs to, and that name can drive anything.

## The idea

**One site per cell of a grid.** Scattering points genuinely at random would mean searching all of them. Instead, put exactly one site in each cell of a regular grid and let it wander inside its cell. The search is then local: the nearest site must be in this cell or in one of its eight neighbours.

**Nine cells, not one.** This is the case [Unit 10]({{ site.baseurl }}/learn/10-repetition.html) said it would eventually have to check neighbours for, and here it is unavoidable. A site near a cell boundary is genuinely closer to points in the neighbouring cell than that cell's own site is, so folding into one cell gives the wrong answer. The loop is 3 by 3, always.

The invariant that makes nine enough is that a site never leaves its own cell. Let a site wander further and you need a wider search, which is why the jitter in the player is capped at half a cell.

**F1 and F2.** `F1` is the distance to the nearest site, `F2` to the second nearest. `F1` alone gives you cells with a dark point at each site, which reads as bubbles or as a metallic texture. The **cell id**, meaning which site won, gives you flat regions you can colour independently, which is where the identity comes from.

**Borders, and why F2 minus F1 is not good enough.** The usual way to draw the cell walls is `F2 - F1`, which goes to zero on a boundary. It is one line and it is wrong at corners: where three cells meet, the second and third nearest sites are both about equally far, so the difference collapses and the border thickens into a blob. Every Voronoi you have seen with fat corners did this.

The correct distance to the nearest **edge** needs a second pass. For each neighbouring site, measure how far the point is from the perpendicular bisector between that site and the winner; the smallest of those is the true edge distance. It costs a second loop, it gives borders of genuinely uniform width, and it is Quilez's.

**Distance metric changes everything.** Euclidean distance gives the round-ish cells everyone expects. Manhattan distance, `abs(r.x) + abs(r.y)`, gives axis-aligned cells that look like a city plan. Chebyshev, `max(abs(r.x), abs(r.y))`, gives squares. Swapping the metric is one line and produces a completely different material.

**It composes with everything.** Warp the coordinate before the Voronoi, from [Unit 15]({{ site.baseurl }}/learn/15-domain-warping.html), and the cells become organic. Sum Voronoi at several scales, as in [Unit 14]({{ site.baseurl }}/learn/14-fbm.html), and you get cells within cells. Use the cell id to hash a per-cell rotation and you have Truchet tiling.

## Build it

{% include shader.html id="16-voronoi" height="440" pointer="none" caption="Leave Show the sites on. Every feature in every view is anchored to those points, and the whole algorithm is easier to believe once you can see them." %}

1. **Start on F1.** Each site is a dark point with brightness rising away from it. The boundaries are where two rising fields meet, forming a visible crease.
2. **Switch to cell id.** Flat regions, each its own colour, each colour a hash of the cell's index. This is the identity the smooth functions cannot give you.
3. **Switch to F2 minus F1** and set Edge width fairly large. Look at the corners where three cells meet: the border swells. That is the approximation failing.
4. **Switch to true edge distance** with the same edge width. The corners are clean and every border is the same thing wide. Flip between the two views a few times.
5. **Take Jitter to zero.** The sites go to the centres of their cells and the Voronoi becomes a regular grid, which is a useful sanity check: if it does not, the site function is escaping its cell.
6. **Take Jitter to one and turn Drift up.** The cells move and reorganise. Watch a boundary as two sites pass each other: cells appear and disappear, and no cell ever crosses another. This is what makes Voronoi feel alive rather than merely textured.
7. **Switch to id with F1 shading**, which is the combination worth taking away: per-cell colour, shaded by distance from the site, with a true edge. Cracked earth in three lines of colour code.

## Look at these

{% include toy.html id="ldl3W8" title="Voronoi distances" by="Inigo Quilez" note="The true-edge second pass, with the derivation. This unit's implementation is this." %}
{% include toy.html id="4tX3DN" title="Voronoi edges" by="Inigo Quilez" note="F2 - F1 against the correct version, side by side, so the corner problem is undeniable." %}
{% include toy.html id="Xd23Dh" title="Voronoise" by="Inigo Quilez" note="One function that morphs continuously between value noise, gradient noise, and Voronoi. Now you have met all three." %}

Quilez's [Voronoi articles](https://iquilezles.org/articles/voronoilines/) cover the edge distance and the smooth variant.

## Smooth Voronoi, and what it is for

There is a version that replaces the hard `min` over sites with a smooth minimum, exactly as [Unit 08]({{ site.baseurl }}/learn/08-combining-fields.html) replaced `min` on distance fields. The cells then blend into each other instead of meeting at a crease, and the field becomes differentiable, which matters if you want to light it or use it as a displacement.

It is the right tool when Voronoi is being used as a *texture* rather than as a *tessellation*: metal, hammered surfaces, water caustics. When you want visible cells, the hard version is the point.

## Common mistakes

- **Searching one cell instead of nine.** The result looks almost right and has a discontinuity at every cell boundary. This is the single most common Voronoi bug.
- **Letting a site leave its cell.** Jitter above half a cell breaks the nine-cell guarantee and reintroduces the same discontinuity.
- **Drawing borders with F2 minus F1** and living with fat corners. It is one extra loop.
- **Forgetting that F1 is not a distance field.** It is the distance to a *point*, not to the cell boundary, so it cannot be offset or antialiased as if it were a signed distance. The true edge distance can.
- **Hashing the site position instead of the cell index** for per-cell colour, so the colour changes as the site moves.
- **Using `sqrt` inside the loop.** Compare squared distances and take the root once at the end. Nine square roots per pixel is a real cost for nothing.

## Exercise

Build a cracked-surface shader: Voronoi with true edges, where the crack width varies per cell, and the whole coordinate is warped by one level of fbm from [Unit 15]({{ site.baseurl }}/learn/15-domain-warping.html) before the Voronoi is evaluated.

Requirements: a `float` input named `drying` takes the surface from unbroken to fully cracked by widening the cracks; a `float` named `irregularity` controls the warp; and the cracks must stay the same width at corners.

**Success criterion:** at `drying` zero the surface has no visible cracks, at maximum it is a network of them, and no junction where three cells meet is wider than the cracks leading into it. If the junctions are blobs, you are still using F2 minus F1. If the cells look mechanical at high `irregularity`, the warp is being applied after the fold rather than before it.

## Going further

- [Inigo Quilez, Voronoi edges](https://iquilezles.org/articles/voronoilines/), the correct border distance.
- [Inigo Quilez, smooth Voronoi](https://iquilezles.org/articles/smoothvoronoi/), for the differentiable version.
- [Steven Worley, *A Cellular Texture Basis Function*](https://dl.acm.org/doi/10.1145/237170.237267), the 1996 paper that introduced this to graphics.
