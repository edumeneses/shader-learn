---
layout: default
title: "Unit 20: Reading a texture, and the coordinate spaces that bite"
description: "A shader with an input is a filter. Getting the input on screen the right way up, the right shape, and the right size is four lines that go wrong in five recognisable ways."
parent: Units
nav_order: 22
unit: "20"
permalink: /learn/20-sampling.html
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 20: Reading a texture, and the coordinate spaces that bite

{% include unit_meta.html %}

> **Before this unit** read [Unit 03]({{ site.baseurl }}/learn/03-coordinates.html).
>
> **You will need** the player below. It reads the course's test card, and it will read your webcam if you ask it to.
>
> **You will build** the aspect-correct fit that every filter in the rest of this course starts with.

## Why this matters

Everything up to here has generated. From here on, shaders take an input, and in *ossia score* that input is usually a video, a camera, or the output of another process. That changes what a shader is for: a **generator** makes a picture out of nothing and a **filter** transforms one it was given, and Module F is about the second.

The technique is trivial and the coordinates are not. An input texture has its own size and its own aspect ratio, neither of which matches the output, and the two disagree about which way the y axis runs. The result is five distinct failure modes, all of them recognisable once you have seen them and all of them confusing the first time: an image that is stretched, letterboxed when you wanted it cropped, upside down, collapsed into a single colour, or crawling when it moves.

Getting this right once, in a form you can paste, is worth more than understanding it deeply.

## The idea

**A texture is read at a normalised coordinate.** 0 to 1 across, 0 to 1 up, regardless of the texture's pixel size. `IMG_NORM_PIXEL(inputImage, uv)` is the ISF accessor and it is the one to use.

**`IMG_SIZE` gives the pixel dimensions**, which you need for anything measured in texels: a blur's tap spacing, a sharpen's neighbour offsets, an edge detector's stencil. `1.0 / IMG_SIZE(inputImage)` is one texel in normalised units, and it appears in every shader in the rest of this module.

**Fit and fill are four lines each.** Compare the input's aspect ratio to the output's, and scale the axis that has room to spare:

```glsl
float inA = size.x / size.y, outA = RENDERSIZE.x / RENDERSIZE.y;
vec2 scale = inA > outA ? vec2(1.0, outA / inA) : vec2(inA / outA, 1.0);
vec2 lookup = (uv - 0.5) / scale + 0.5;          // fit, letterboxed
```

Reverse the comparison and you get fill, which crops instead. There is no third option worth having, and a shader that uses `uv` directly is choosing stretch by accident.

**The y flip is the most common bug in this whole field.** Texture space and window space disagree about which way y runs, and different graphics APIs disagree with each other. ISF's answer is to supply `isf_FragNormCoord` and to say, in *ossia score*'s own documentation, that you should not reach for `gl_FragCoord`, precisely because *score* can run on OpenGL, Vulkan, Metal, or Direct3D and they do not agree.

The practical consequence: if your image is upside down, do not add a flip and move on. Find out which stage flipped it, because a flip you did not understand will come back inverted on someone else's machine.

**Filtering happens between texels.** A lookup at a coordinate that falls between texel centres returns a blend of the neighbours, which is bilinear filtering and is nearly free because the hardware does it. Two consequences worth knowing: a magnified image is smooth rather than blocky whether you wanted that or not, and a shader that wants blocky has to snap its lookup to texel centres itself.

**Sampling outside the texture returns the edge.** The default wrap mode clamps, so a lookup at 1.3 returns the last column, stretched outward. That is why a badly built feedback shader smears from its border, and it is why the player below paints outside the image in a flat colour rather than pretending.

**Minification is where aliasing lives.** [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html) applies unchanged: a texture shown smaller than its pixel size is undersampled, and the fix is mipmaps rather than anything you can do per pixel. `textureLod` and `texture` with a bias are how you reach them, and a shader that samples a minified texture without them will crawl.

## Build it

{% include shader.html id="20-sampling" height="440" pointer="centre" caption="Step through the five mappings. Drag the canvas to move the zoom centre. Then change the source to your camera, which is a better test image than any card because you know what it should look like." %}

1. **Start on fit.** The whole card is visible with a gap where the shapes do not match. The red bracket is at the top left, which is how you know nothing has flipped.
2. **Switch to stretch.** The card fills the frame and the aspect is wrong: the colour bars are no longer the same width and the registration cross is no longer square. This is what using `uv` directly does.
3. **Switch to fill and crop.** No gap, and the top and bottom of the card are gone. Neither this nor fit is more correct; they answer different questions.
4. **Switch to flipped Y.** The bracket moves to the bottom left. That asymmetric marker is on the test card for exactly this reason: a flip on a symmetric image is invisible.
5. **Switch to pixel coordinates, unnormalised.** The whole frame is one colour, because a lookup coordinate of 900 is far outside the 0 to 1 range a sampler expects, and the clamp returns the corner texel. Worth causing once, because the symptom looks nothing like the cause.
6. **Go back to fit and raise Zoom.** Around 8 the texels become visible as soft squares: that is bilinear filtering, magnifying and interpolating.
7. **Raise Sample grid with the zoom still high.** The lookup snaps to a coarse grid, so each cell reads one point and holds it. That is nearest-neighbour sampling, drawn large enough to see.
8. **Set the source to your camera.** Everything above still applies, and the aspect ratio is now genuinely different from the output, which is the case that matters.

## Look at these

{% include toy.html id="XsfGDn" title="Texture filtering" by="Inigo Quilez" note="Bilinear against a smoothed lookup, magnified enough that the difference is visible." %}
{% include toy.html id="MdBGzG" title="Filtered checkerboard" by="Inigo Quilez" note="What minification does without mipmaps, and how to fix it analytically." %}

## Reading a texture in *ossia score*

A shader's `image` input becomes a **texture inlet** on the process. Anything that produces a texture can be connected to it: a video file, a camera, a screen capture, another shader, a 3D scene. That is the whole of *score*'s graphics model, and it is why a filter written here drops into a patch without modification.

The one thing to know now is that *score* decides the texture's size, and it is not necessarily the output size. Always derive from `IMG_SIZE`, never from `RENDERSIZE`, when you mean the input. [Unit 39]({{ site.baseurl }}/learn/39-video-input.html) covers the rest.

## Common mistakes

- **Using `uv` directly and getting stretch.** Correct once, in a function, and reuse it.
- **Adding a y flip to fix an upside-down image** without finding out which stage flipped it.
- **Using `RENDERSIZE` where you meant `IMG_SIZE`.** The two are often equal while you are developing and never equal in production.
- **Forgetting that a lookup outside the texture clamps.** Check the range and paint something deliberate outside it.
- **Sampling a minified texture without mipmaps**, giving a crawling image.
- **Assuming the input is opaque.** A texture from another process may have alpha, and ignoring it makes a composite wrong at every soft edge. [Unit 23]({{ site.baseurl }}/learn/23-compositing.html).

## Exercise

Write a reusable `fit` function that takes the output coordinate, the input size, and a `long` mode input choosing fit, fill, or stretch, and returns the lookup coordinate.

Then build a shader that uses it and adds a `float` named `rotate` which rotates the image about its centre without changing which mapping mode is in effect.

**Success criterion:** at every mode and every rotation, the registration cross stays square and the red bracket stays at the corner it started at. If rotating changes the shape of the cross, the rotation is being applied in a space that is not square, which is [Unit 03]({{ site.baseurl }}/learn/03-coordinates.html) returning.

## Going further

- [ISF's image accessors]({{ site.isf_baseurl }}/), for `IMG_PIXEL`, `IMG_NORM_PIXEL`, and `IMG_SIZE`.
- [*ossia score*'s video pipeline]({{ site.docs_baseurl }}/common-practices/11-video-mixing.html), for where the texture comes from.
- [Inigo Quilez, on texture filtering](https://iquilezles.org/articles/), for the cases the hardware does not handle.
