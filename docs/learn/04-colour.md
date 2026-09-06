---
layout: default
title: "Unit 04: Colour, linear light, and mixing that does not go muddy"
description: "Why a blue-to-yellow gradient passes through grey, what sRGB actually stores, and the two functions that fix it. Also banding, and the dither that costs one line."
parent: Units
nav_order: 4
unit: "04"
permalink: /learn/04-colour.html
reading_time: "13 min"
practice_time: "20 min"
glsl: "GLSL ES 3.00"
---

# Unit 04: Colour, linear light, and mixing that does not go muddy

{% include unit_meta.html %}

> **Before this unit** read [Unit 02]({{ site.baseurl }}/learn/02-first-fragment-shader.html).
>
> **You will need** the player below, and a screen you are willing to look at closely.
>
> **You will build** two functions you will paste into shaders for the rest of your life, and an understanding of when not to use them.

## Why this matters

Blend blue into yellow in almost any graphics tool and the middle goes grey. Blend two saturated colours and the result is duller than either. Fade an image to black and it appears to fall off a cliff near the end. All three are the same bug, and it is not in your shader: it is in the assumption that the number stored in a colour is a brightness.

It is not. The number is a code, spaced so that the values you can store are distributed the way an eye notices differences, which is very unevenly. The middle code, 0.5, is about 21 percent of the light of the top code. Interpolating codes therefore interpolates something that is not light, and the result is a colour that no lamp could produce.

Fixing it costs two function calls and about ten instructions. Knowing when to skip it matters just as much, because a shader that converts to linear light and back six times in one frame is paying for nothing.

## The idea

**sRGB is a transfer function, not a colour.** The standard defines a curve, close to raising a number to the power 2.2 but not identical, that maps a stored code to a fraction of maximum light. Every image file, every colour picker, and every `vec4` you type is in that curve's space unless you have deliberately left it.

**Light adds linearly, and codes do not.** Two lamps at half brightness make one lamp's worth of light. Two pixels at code 0.5 do not make code 1.0; they make code 0.5. If your arithmetic is meant to model light, and blending is, it has to happen after the curve is undone.

**The two functions.** These are the exact piecewise definitions, not the `pow(x, 2.2)` approximation:

```glsl
vec3 srgbToLinear(vec3 c) {
    vec3 lo = c / 12.92;
    vec3 hi = pow((c + 0.055) / 1.055, vec3(2.4));
    return mix(lo, hi, step(vec3(0.04045), c));
}

vec3 linearToSrgb(vec3 c) {
    vec3 lo = c * 12.92;
    vec3 hi = 1.055 * pow(max(c, 0.0), vec3(1.0 / 2.4)) - 0.055;
    return mix(lo, hi, step(vec3(0.0031308), c));
}
```

The approximation is wrong near black, which is exactly where banding lives, so this course uses the real one. The `step` and `mix` pair is how you write a branch without a branch; both sides are evaluated, which on a GPU is usually cheaper than diverging.

**When to convert, and when not to.** Convert when you are **combining** colours: blending, averaging, blurring, adding light, compositing. Do not convert when you are **generating**: a palette, a gradient of hue, a pattern of your own invention. Those numbers were never photographs of anything, and forcing them through a physical model just makes them darker. Most of Phase 1 generates, so most of Phase 1 stays in code space and says so.

**Do it once, at the ends.** The pattern is: convert every input to linear at the top of the shader, do all the arithmetic, convert once at the bottom. Converting inside a loop is the most common way to make a shader four times more expensive for a result nobody can tell apart.

**Banding is a separate problem.** An eight-bit channel has 256 codes. A gradient across 1920 pixels has at most 256 distinct values in it, so it must repeat each one about seven times, and the eye finds the boundaries. The fix is not more precision in the shader; the shader is already at 32 bits. The fix is **dither**: add well under one code's worth of noise before the value is truncated, and the boundary becomes a stipple the eye integrates back into a smooth ramp. One line, and it is the difference between a professional-looking gradient and an amateur one.

**Luminance is not the average of the channels.** The eye is roughly twice as sensitive to green as to red and about ten times as sensitive to green as to blue. `dot(linearColour, vec3(0.2126, 0.7152, 0.0722))` is the correct weighting, on linear values. Averaging the three channels instead is why a naive greyscale makes red and blue look equally dark when they do not.

## Build it

The player shows one gradient twice: above the split, the two colours are blended as stored codes; below it, they are blended as light. The two swatches are the exact midpoint of each.

{% include shader.html id="04-mixing" height="360" pointer="none" caption="Blue to yellow. Above the line, the middle is a grey nobody chose. Below it, the middle is the colour two lamps would actually make. Turn Quantise up to see banding appear, then turn Dither on." %}

1. **Look at the two midpoint swatches.** They are the same two colours mixed by the same fraction. That is the whole unit in one picture.
2. **Try blue against orange, then red against green.** The effect is largest between colours that are far apart in hue and similar in code value, and nearly invisible between two colours that differ mostly in brightness. That is why the bug survives: it does not show up in the test everyone does first.
3. **Move the split to 0 and to 1** so you can see each gradient full height without the other beside it. The naive one has a visible dark waist; the linear one does not.
4. **Turn Quantise up to about 16.** Bands appear in both. This is the eight-bit problem made obvious, and it is worth seeing on purpose, because at 256 levels it is subtle enough that you will blame your monitor.
5. **Turn Dither on with Quantise still at 16.** The bands become texture. Look closely and you can see the dither pattern; look normally and you see a smooth ramp. Then set Quantise to 64 and turn Dither on and off: at that point the dither is invisible and the improvement is not.
6. **Read the `bayer` function.** It is a 4 by 4 ordered dither computed from the pixel coordinate, with no texture and no random number. Ordered dither has a visible structure, which is a real cost; blue noise is better and needs a texture, which [Unit 12]({{ site.baseurl }}/learn/12-hashes.html) gives you the tools to fake.

## Look at these

{% include toy.html id="lsdGzN" title="Gradient banding and dithering" by="Shadertoy community" note="The same experiment with the noise floor exposed; compare the ordered pattern to hash-based noise." %}
{% include toy.html id="WdjfDy" title="Colour space comparison" by="Shadertoy community" note="sRGB against OKLab, which is the next step past this unit." %}

## Beyond linear: perceptual spaces

Linear light fixes the physics. It does not fix perception: a linear blend from blue to yellow still passes through a lightness dip, because the two colours are not equally bright to an eye. Spaces designed around that, of which **OKLab** is the current best answer, blend at constant perceived lightness and produce gradients that look designed rather than computed.

This course does not use OKLab, for one reason: it is thirty lines of matrices and cube roots, and every one of them would be in the way of the technique each unit is actually about. When a gradient in your own work looks wrong after you have made it linear, that is the moment to reach for it, and Björn Ottosson's article is the place to go.

## Common mistakes

- **Converting a palette to linear.** A cosine palette's numbers are not measurements of light. Converting them just darkens the result and loses the shape you tuned.
- **Converting twice.** Two `srgbToLinear` calls on the same value is a very dark picture, and it looks like an exposure problem rather than a logic one.
- **Using `pow(c, 2.2)` and then wondering about the shadows.** The approximation diverges from the standard below about 0.04, which is the whole shadow range.
- **Averaging the channels for luminance.** Use the weighted dot product. This one is worth fixing even in code space.
- **Dithering after quantisation instead of before it.** Adding noise to an already-banded image gives you a noisy banded image.
- **Assuming the display is sRGB.** On a wide-gamut or HDR screen it is not, and your carefully corrected blend is being re-mapped by something downstream. Nothing in a fragment shader can fix that; it is the host's job, and in *ossia score* it belongs to the output window rather than the process.

## Exercise

Take the vertical gradient you built for [Unit 02]({{ site.baseurl }}/learn/02-first-fragment-shader.html) and give it a `bool` input named `linearBlend`.

When it is off, blend the two colour inputs directly. When it is on, blend them in linear light. Then add a `float` input named `dither` that scales the amount of ordered dither added before output, from none to about two codes' worth.

**Success criterion:** with two colours far apart in hue, toggling `linearBlend` visibly changes the middle of the gradient and does not change either end. With `dither` at zero and a photograph of your screen zoomed in, you can count bands; with `dither` at one you cannot. If toggling changes the ends as well, you converted one colour and not the other.

## Going further

- [Björn Ottosson on OKLab](https://bottosson.github.io/posts/oklab/), the article that made the space widely used.
- [Inigo Quilez, on gamma correction](https://iquilezles.org/articles/), for the version aimed at shader writers.
- [Alan Wolfe on dithering and blue noise](https://blog.demofox.org/), for what to use instead of Bayer.
- [The sRGB standard's transfer function](https://www.w3.org/Graphics/Color/srgb), if you want the constants from the source.
