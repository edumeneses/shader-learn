---
layout: default
title: "Unit 21: Convolution, separable kernels, and a big blur that is cheap"
description: "A weighted sum of neighbours is a blur, a sharpen, or an edge detector depending only on the weights. Doing it as two one-dimensional passes turns n squared samples into 2n."
parent: Units
nav_order: 23
unit: "21"
permalink: /learn/21-convolution.html
reading_time: "14 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 21: Convolution, separable kernels, and a big blur that is cheap

{% include unit_meta.html %}

> **Before this unit** read [Unit 20]({{ site.baseurl }}/learn/20-sampling.html) and [Unit 18]({{ site.baseurl }}/learn/18-feedback.html).
>
> **You will need** the player below, with the compare wipe on.
>
> **You will build** a separable Gaussian, which is the most-used image operation in real-time graphics and the one whose cost people most often get wrong.
>
> **Reference:** the shader is `library/shaders/21/convolution.fs`.

## Why this matters

Convolution is one idea: the output at a pixel is a weighted sum of the input around it. Change the weights and you get a blur, a sharpen, an edge detector, an emboss, or a motion smear. It is the operation behind bloom, depth of field, glow, soft shadows, ambient occlusion, and most of what a compositor does.

It is also the first operation in this course whose **cost** is a design constraint rather than a footnote. A 25 by 25 blur is 625 texture reads per pixel, which at 1080p is 1.3 billion reads per frame, and no GPU will do that at 60 frames per second. The same blur done as two one-dimensional passes is 50 reads: **twelve times cheaper, with an identical result**. That is not an optimisation, it is the difference between a technique that ships and one that does not.

## The idea

**A kernel is a grid of weights.** Slide it over the image, multiply, sum, and normalise if the weights do not already add to 1. A 3 by 3 with a 5 in the middle and -1 on the sides is a sharpen; with 8 in the middle and -1 everywhere it is an edge detector.

**Some weights produce negative results.** An edge detector and an emboss both output signed values, and negative light is not visible. Add a bias, usually 0.5, or take the absolute value, and say which you did; a filter whose output is half black is usually this rather than a bug.

**Separability is the whole unit.** A kernel is separable when the two-dimensional weight is the product of two one-dimensional weights: `w(x, y) = w(x) * w(y)`. A Gaussian is, and so are a box blur and a Sobel. When it is, you can blur horizontally into a buffer, then blur that vertically, and get exactly the same answer for `2n` samples instead of `n²`.

This needs two passes, which is what [Unit 18]({{ site.baseurl }}/learn/18-feedback.html)'s machinery was for. The first pass writes an intermediate target; the second reads it. The target does not need to be persistent, because it is written and read within one frame.

**A Gaussian's sigma and its radius are different numbers.** Sigma is the width of the distribution; the radius is where you stop sampling. Stopping at about three sigma captures 99 percent of the weight, and stopping much earlier gives a blur with a visible flat edge to it. Computing the weights rather than baking them means the radius can be a control, which is why so many published blurs have a fixed radius: theirs are baked.

**Bilinear sampling gives you two taps for one.** Sample exactly between two texels with the right offset and the hardware returns their weighted average for a single read, which halves a Gaussian's cost again. The offsets are a short derivation and the technique is standard; it is the reason a well-written blur is cheaper still than the arithmetic above suggests.

**Downsample first, for large blurs.** A blur at half resolution costs a quarter, and blurring is low-pass by definition, so almost nothing is lost. Every bloom in every game does this, usually across several resolutions at once.

**Convolution is not the only way to blur.** Repeated box blurs converge on a Gaussian and are cheaper still; a feedback buffer accumulating a jittered image blurs over time; and for very large radii, working in the frequency domain wins, though not in a fragment shader.

## Build it

{% include shader.html id="21-convolution" height="440" pointer="none" caption="Leave Compare at about 0.5: a filter is best judged against what it replaced. Watch the frequency wedges on the test card, which is where a blur's loss of detail is measurable rather than a matter of opinion." %}

1. **Start on gaussian blur with Separable on.** Move the Compare wipe across and watch the wedges lose their fine end first. That point, where the pattern turns grey, is exactly where the filter stopped resolving it.
2. **Turn Separable off.** The picture is the same, and it should be, because a separable kernel applied in two passes is mathematically identical. Note the cap in the source: the naive branch is limited to 12 taps, because without it the maximum radius would be 2401 samples per pixel.
3. **Raise Radius with Separable on**, then with it off. The separable version stays smooth; the naive version is where a modest GPU begins to struggle. On a fast machine you may see no difference at all, which is worth noticing: the cost is real and your hardware is hiding it, and a reader on a laptop will not be so lucky.
4. **Switch to sharpen.** Look at the wedges: the mid frequencies get a visible halo. That halo is what sharpening is, and it is why over-sharpened images look crunchy rather than detailed.
5. **Switch to edge detect.** Flat regions go black and boundaries light up. Note that the colour bars produce edges only at their joins, which is the point of a test card.
6. **Switch to emboss** and note the 0.5 bias in the source. Without it, half the output would be negative and invisible.
7. **Switch to Sobel magnitude.** Two kernels, one for each axis, combined by length. This is the operator behind most edge detection everywhere, and it is nine samples.
8. **Take Amount above 1** on sharpen. The filter overshoots, which is a real look and is also how a mild sharpen becomes an obvious one.

## Look at these

{% include toy.html id="XdfGDH" title="Separable Gaussian blur" by="Shadertoy community" note="Two passes, with the weights computed rather than baked." %}
{% include toy.html id="lsXGWn" title="Sobel edge detection" by="Shadertoy community" note="Nine samples, and the version that keeps the gradient direction as well as its magnitude." %}
{% include toy.html id="4dfGDH" title="Bloom" by="Shadertoy community" note="Threshold, downsample, blur, add back: the whole pipeline this unit's technique exists to serve." %}

## Common mistakes

- **A two-dimensional Gaussian when a separable one would do.** The most expensive mistake in this module, and it will not show up on the machine you wrote it on.
- **Not normalising the weights.** The image gets brighter or darker as the radius changes, which reads as an exposure bug.
- **Offsets in normalised units where texels were meant.** `uv + vec2(1.0, 0.0)` is the whole width of the image. The unit is `1.0 / IMG_SIZE(inputImage)`.
- **Forgetting the bias on a signed kernel**, giving an output that is half black.
- **Sampling outside the image and getting the clamped edge**, which darkens or smears the border. Every blur has this at its boundary and every good one handles it deliberately.
- **A variable loop bound.** Some WebGL drivers require a compile-time bound, so this unit's shader loops to a fixed maximum and `continue`s past the taps it does not want. [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html) said the same thing about supersampling and Module G will say it again.
- **Blurring in code space and expecting it to look like light.** A blur is an average, and [Unit 04]({{ site.baseurl }}/learn/04-colour.html)'s rule applies: averaging codes darkens. This unit's shader does not convert, which is the common shortcut, and it is worth turning into an experiment.

## Exercise

Build a bloom: threshold the image to keep only what is brighter than a control, blur that with a separable Gaussian at reduced resolution, and add the result back over the original.

Requirements: at least three passes; the blur target must be smaller than the output, using the `WIDTH` and `HEIGHT` keys in the `PASSES` block; a `float` named `threshold` sets what glows; and a `float` named `intensity` sets how much.

**Success criterion:** the white region of the test card glows and the mid greys do not, the glow is smooth with no visible steps, and halving the blur target's resolution again does not visibly change the result. If the glow has hard edges, the threshold is a `step` where it should be a `smoothstep`.

## Going further

- [Sigg and Hadwiger, on fast filtering with bilinear taps](https://developer.nvidia.com/gpugems/gpugems2/part-iii-high-quality-rendering/chapter-20-fast-third-order-texture-filtering), for the two-taps-for-one trick.
- [Jorge Jimenez's next-generation post processing](https://advances.realtimerendering.com/), for how bloom is done in production.
- [Unit 34]({{ site.baseurl }}/learn/34-cost.html), which returns to the arithmetic in this unit with tools to measure it.
