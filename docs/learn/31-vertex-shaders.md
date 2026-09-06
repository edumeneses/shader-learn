---
layout: default
title: "Unit 31: Vertex shaders, and Vertex Shader Art"
description: "Invert the pipeline: write the vertex stage and let the fragment stage be generated. Sixty thousand points, each deciding where it goes from nothing but its own index."
parent: Units
nav_order: 34
unit: "31"
permalink: /learn/31-vertex-shaders.html
score_version: "3.8.2"
reading_time: "14 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 31: Vertex shaders, and Vertex Shader Art

{% include unit_meta.html %}

> **Before this unit** read [Unit 28]({{ site.baseurl }}/learn/28-the-pipeline.html).
>
> **You will need** the player below.
>
> **You will build** a shader that scatters, which [Unit 19]({{ site.baseurl }}/learn/19-state-in-a-texture.html) said a fragment shader cannot do.

## Why this matters

[Unit 19]({{ site.baseurl }}/learn/19-state-in-a-texture.html) ran into a wall: a fragment shader can gather and cannot scatter, so a field simulation is natural and a particle system is impossible. This is the way through.

A **vertex shader**'s entire job is to decide where something lands. That is scattering, and it is what the stage is for. Give the pipeline sixty thousand vertices with no data attached but an index, and let a vertex shader decide where each one goes: you have a particle system, drawn in one call, with nothing stored and nothing uploaded.

**Vertex Shader Art** is that idea taken as a discipline. It comes from vertexshaderart.com, and *ossia score* implements it as a process, which is why it is in this course: it is the shape of shader most likely to be useful in a live visual patch, and the least like anything in Phases 1 and 2.

## The idea

**The stage runs once per vertex and writes `gl_Position`.** Clip space: x and y from -1 to 1, with -1 at the left and bottom. There is no `isf_FragNormCoord` here, and no `fwidth` either, because both come from the fragment stage.

**The only input is `vertexId`.** A float, from 0 to `vertexCount - 1`. No mesh, no buffer of positions, no model file. Everything about where a point goes has to be computed from that number, which sounds like a limitation and is the entire creative constraint of the form.

**Split the index.** One division and one modulo turn a flat list into a structure:

```glsl
float perStrand = floor(vertexCount / strands);
float strand = floor(vertexId / perStrand);
float along = mod(vertexId, perStrand) / perStrand;
```

Now you have a strand number and a position along it, which is enough to build almost anything: curves, ribbons, grids, spirals, lattices, point clouds.

**`gl_PointSize` sets a point's size in pixels**, and only for `POINTS`. It is the one output people forget, and its default is implementation-defined, so a shader that does not write it may draw one-pixel points or nothing at all.

**The colour goes out as a varying.** `v_color` in the VSA convention. The generated fragment shader does nothing but pass it through, interpolating between vertices along a line or across a triangle.

**Additive blending is the assumption.** Tens of thousands of overlapping points look like a solid mass under normal blending and like light under additive. It also means each point must be drawn *dim*: at sixty thousand points, a bright one gives a white blob and nothing else. The brightness control in the player below is set at about 0.16 for that reason.

**The primitive mode changes everything.** `POINTS` gives a cloud. `LINES` joins them in pairs. `LINE_STRIP` gives one continuous polyline through every vertex in order, which turns the same shader into a drawing. Changing one header key rewrites the piece.

**In ossia score**, the header is `"MODE": "VERTEX_SHADER_ART"`, with `POINT_COUNT`, `PRIMITIVE_MODE`, and `BACKGROUND_COLOR`. Inputs work exactly as in ISF, so everything from [Unit 29]({{ site.baseurl }}/learn/29-isf.html) applies, and a VSA shader's parameters become inlets in the same way.

**The cost model is completely different.** A fragment shader's cost is per pixel; a vertex shader's is per vertex, and the fragment stage is nearly free because the generated one does nothing. Sixty thousand points at sixty frames per second is 3.6 million vertex invocations per second, which is nothing at all for a modern GPU. This is by far the cheapest kind of shader in the course, and it is why the technique scales to numbers that would be absurd in Module G.

## Build it

{% include shader.html id="31-lissajous" height="460" pointer="pointer" caption="Sixty thousand points. Each one knows only its own index and works out the rest. Take Strands to 1 and it becomes a single curve; take it to 300 and it becomes a surface." %}

1. **Take Strands to 1.** One Lissajous curve, sixty thousand points along it. This is the simplest thing the form does and it is worth seeing alone.
2. **Raise Strands.** The single curve becomes many, phase-shifted and offset, and the eye reads the family as a surface. Nothing three-dimensional is happening; it is the density that does it.
3. **Change Ratio a and Ratio b.** The figure changes completely. Non-integer ratios never close, which is why the curve fills in rather than repeating.
4. **Raise Twist.** The whole object bends, because the transform is applied to the *position* rather than to a lookup, which is the reverse of everything in Module C.
5. **Take Brightness up to 0.5.** It blows out to white. Additive blending accumulates, so brightness is a budget shared between however many points overlap.
6. **Take Point size up.** The cloud becomes a mass. Small points with many of them almost always beat large points with few.
7. **Open the source and note what is missing.** There is no fragment shader. The one that runs is generated and does one thing: pass `v_color` through.
8. **Note the aspect-ratio correction.** `gl_Position.x` is divided by the aspect ratio, by hand. There is no `isf_FragNormCoord` here to have done it, which is [Unit 03]({{ site.baseurl }}/learn/03-coordinates.html) arriving in a stage that has no help to offer.

## Look at these

[vertexshaderart.com](https://www.vertexshaderart.com/) is the whole form in one place, with the source of every piece visible. It is the single best source of material for this unit, and the shaders there run in *ossia score* with only a header added.

{% include toy.html id="MtdSWS" title="Vertex displacement" by="Shadertoy community" note="The other use of a vertex shader: moving real geometry rather than inventing it." %}

## The other use of a vertex shader

Vertex Shader Art invents geometry. The ordinary use is to **move geometry that already exists**: a mesh arrives as vertices, and the vertex shader displaces, animates, skins, or projects it. That is what the stage does in every game engine, and in *ossia score* it is what happens when a shader is applied to a loaded model rather than to a full-screen quad.

The techniques transfer directly. Everything from Module D is available: displace a vertex along its normal by fbm and you have terrain; by a sine of time and position and you have cloth or a flag. The one thing to watch is that **displacing a vertex does not update its normal**, so a displaced mesh lit with its original normals looks flat. Recomputing the normal from the displacement is the standard fix and it is the same central-difference idea as [Unit 26]({{ site.baseurl }}/learn/26-lighting.html).

## Common mistakes

- **Not writing `gl_PointSize`**, giving invisible or one-pixel points.
- **Points too bright.** With additive blending and tens of thousands of overlaps, brightness is shared.
- **Forgetting the aspect ratio.** The fragment stage's helpers do not exist here.
- **Expecting `fwidth`.** It is a fragment-stage feature and there is no equivalent.
- **A `vertexCount` that does not divide by the strand count**, leaving a ragged last strand. `floor` the division and accept a few unused points.
- **Assuming a vertex shader is slow because there are a lot of vertices.** It is the cheapest stage in this course.
- **Displacing geometry and keeping the original normals**, giving a bumpy shape that is lit flat.

## Exercise

Build a VSA shader that draws a three-dimensional point cloud: a shape defined in three dimensions, projected to clip space by hand, with depth used for both point size and brightness so it reads as having volume.

Requirements: at least twenty thousand points; a `point2D` that orbits the shape; a `float` that controls the perspective strength from orthographic to strongly perspective; and points must be smaller and dimmer with distance.

**Success criterion:** the cloud reads as a solid object rotating in space, and turning perspective to zero makes it visibly flatten into an orthographic projection. If the cloud looks like a flat disc at every setting, the depth is not reaching either the point size or the colour.

## Going further

- [vertexshaderart.com](https://www.vertexshaderart.com/), the form and its archive.
- [*ossia score*'s VSA process]({{ site.docs_baseurl }}/processes/vertex-shader-art.html).
- [Unit 41]({{ site.baseurl }}/learn/41-geometry-and-compute.html), where this becomes part of a patch.
