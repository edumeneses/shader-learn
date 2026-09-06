---
layout: default
title: "Milestone P1: a poster from one shader"
description: "The first milestone. No new technique: assemble Units 03 to 11 into one still image you would be willing to print, and export it at print resolution."
parent: Units
nav_order: 12
unit: "P1"
permalink: /learn/p1-poster.html
reading_time: "12 min"
practice_time: "60 min"
glsl: "GLSL ES 3.00"
---

# Milestone P1: a poster from one shader

{% include unit_meta.html %}

> **Before this milestone** finish Units 03 to 11. Nothing here is new.
>
> **You will need** an hour, and a decision about what the picture is of.
>
> **You will build** one still image, from one shader, with no input texture, that holds up at A2.

## Why this matters

A milestone introduces nothing. Its job is to make you assemble what you already have, at a size where the gaps show, and the gaps that show at this point in the course are almost never technical.

You can write a distance function, combine two of them, repeat them, colour them, and antialias them. What you have not done is decide what a picture should look like and then get there on purpose. That is a different skill, it is the one that separates shader art from shader exercises, and the only way to practise it is to finish something.

A still image is the right first target, for two reasons. It removes time, which is a whole extra axis to fail on. And it can be printed, which is a genuinely useful constraint: at print resolution, an edge you guessed the width of will be visibly wrong, and a gradient you did not dither will band.

## The brief

**One shader. One still image. No input.**

Requirements:

1. **Everything comes from a distance field.** No textures, no images, no noise yet; noise is Module D and this milestone deliberately comes first, so that you find out how far shape and colour alone will take you.
2. **At least three primitives**, combined with at least two different operators, one of which is smooth.
3. **At least one use of repetition**, from [Unit 10]({{ site.baseurl }}/learn/10-repetition.html).
4. **Colour from a palette function**, from [Unit 05]({{ site.baseurl }}/learn/05-palettes.html), driven by the field rather than by the coordinate. This is the requirement that most changes how the result looks.
5. **Every edge antialiased with `fwidth`**, from [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html), and no edge width written as a constant.
6. **At least six named inputs**, each with a label and a sensible range, and none named after a device. A reader who has never seen your shader should be able to find something interesting by moving one slider.
7. **It must survive being rendered at 4960 by 7016**, which is A2 at 300 dots per inch. Nothing may be expressed in pixels.

## The reference solution

This is one answer to the brief, and it is deliberately not a very ambitious one: a sun behind a slatted screen, over a ridge line, in three colours.

{% include shader.html id="p1-poster" height="440" pointer="sun" caption="Drag the canvas to move the sun. Everything here is Units 05 to 11 and nothing else: distance fields, a smooth union, a taper in the repetition, a cosine palette driven by the field, fwidth on the boundary, and a grain to defeat banding." %}

Read its source and note four decisions that are compositional rather than technical:

- **The palette is driven by the signed distance**, not by the coordinate, so the interior has depth and the exterior has a glow from one expression. Driving a palette by `uv` gives you a gradient behind a shape; driving it by `d` gives you a shape made of light.
- **The slats taper.** The repetition's cell size is not constant; it widens towards the bottom. A regular grid reads as a fence, and this reads as an object.
- **The ridge line is five boxes smooth-unioned with a falling height profile**, jittered by a sine rather than by a random number. Regular reads as a fence, random reads as noise, and neither reads as terrain.
- **There is a grain**, at about three percent. It is there entirely to break up the banding an eight-bit display gives a smooth palette, which is [Unit 04]({{ site.baseurl }}/learn/04-colour.html)'s point arriving in practice.

Your answer should not look like this one. If it does, change the palette first; that is the fastest way to make a picture yours.

## Build it

1. **Decide what the picture is before you open an editor.** A sentence is enough. "A horizon with something behind it." "One object, lit from one side, on a flat field." A shader without a subject becomes a demonstration of operators, which is what this milestone is trying to get you past.
2. **Block it in with hard edges and flat colours.** Get the composition right while it is cheap to change. Resist tuning the palette at this stage; you will retune it anyway.
3. **Work in the field view** from [Unit 07]({{ site.baseurl }}/learn/07-distance-fields-2d.html) while you build the shape. It is the difference between debugging a picture and debugging a function.
4. **Add the palette.** Drive it by the field. Try at least four sets of constants before you keep one, and look at the whole strip, not just the part you can see.
5. **Now do the edges.** Replace every `step` with the `fwidth` form. If any edge width is a constant, it is wrong.
6. **Add grain last**, at two to four percent. More than that is a look; less than that does not fix banding.
7. **Expose the inputs.** Go through the file and promote every number a viewer might want to move. Six is the minimum; a dozen is normal. Give each a real label and a range that cannot produce a broken picture.
8. **Render it large.** From this repository:

   ```bash
   python3 scripts/render.py library/shaders/p1/poster.fs \
       --out /tmp/poster --formats png --size 4960x7016 --supersample 2
   ```

   That is A2 at 300 dots per inch, supersampled, and it takes a few seconds on a modern GPU. Look at it at 100 percent. Everything you got away with is now visible.

9. **Fix what the large render showed**, which is usually one of three things: an edge that is soft because a `smin` had too large a `k`, a gradient that bands because the grain is too subtle at that resolution, or a repetition whose cell is slightly too small and is chopping.

## Success criteria

You are finished when all five are true.

- **It renders at 4960 by 7016 with nothing broken**, and the composition at that size is the same one you designed at 800 by 450.
- **No number in the shader is expressed in pixels**, and no edge width is a constant.
- **Moving any single input produces a picture that is still worth looking at.** If one slider at one end destroys the image, its range is wrong.
- **You can name the three things that make the composition work**, and none of them is an operator.
- **Someone who has not read this course can tell you what the picture is of.**

## Common ways this goes wrong

- **Too many primitives.** Six carefully placed shapes beat thirty. If the picture is not working, the answer is almost never another shape.
- **Palette chosen first.** Get the composition in greyscale first. A good composition survives a bad palette and the reverse is not true.
- **A `k` tuned at preview size.** Smoothing is a distance, and at A2 you are looking at it ten times closer.
- **Repetition used because it is cheap.** A repeated element needs a reason to be repeated. The slats in the reference are a screen; without that idea they are stripes.
- **Grain added to hide a problem.** Grain fixes banding. It does not fix a soft edge or a muddy blend, and at print resolution it will not hide them either.
- **Stopping at "it works".** This milestone is the first one where "it works" is not the criterion.

## Going further

- [Inigo Quilez's Shadertoy profile](https://www.shadertoy.com/user/iq), for what this technique looks like when someone has done it for twenty years.
- [Unit 12]({{ site.baseurl }}/learn/12-hashes.html), next, which gives you the randomness this milestone deliberately withheld.
- [Inigo Quilez, on painting with maths](https://iquilezles.org/articles/), for the compositional half of the problem rather than the technical one.
