---
layout: default
title: "Unit 05: Palettes, and gradients that look designed"
description: "Four vec3 constants and three cosines give you a whole family of gradients for less than the cost of a texture lookup. This is how to read them, tune them, and drive them from a field."
parent: Units
nav_order: 5
unit: "05"
permalink: /learn/05-palettes.html
reading_time: "12 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 05: Palettes, and gradients that look designed

{% include unit_meta.html %}

> **Before this unit** read [Unit 04]({{ site.baseurl }}/learn/04-colour.html).
>
> **You will need** the player below, and a willingness to move sliders until something looks right.
>
> **You will build** a palette function you will use in every remaining unit, and four sets of constants you like.

## Why this matters

The difference between a shader that looks like an exercise and one that looks like work is almost always colour, and almost never technique. A perfect raymarcher in a bad palette looks like a technical demo. A trivial pattern in a good one looks intentional.

The obstacle is that choosing colours procedurally is genuinely awkward. You cannot open a swatch panel inside a fragment shader, and hard-coding six `vec3`s and interpolating between them is verbose, hard to tune, and breaks the moment you want a fifth stop. What you want is a small number of parameters that move a whole gradient coherently, so that tuning is a matter of nudging four things rather than editing sixteen.

Inigo Quilez's cosine palette is that. It is one line, it costs three cosines, and it is smooth by construction, which matters more than it sounds: it can be driven by a distance rather than sampled at one, so it composes with everything in Modules C and D.

## The idea

**The form.**

```glsl
vec3 palette(float t, vec3 a, vec3 b, vec3 c, vec3 d) {
    return a + b * cos(6.28318 * (c * t + d));
}
```

Read it as three independent cosines, one per channel, all driven by the same `t`.

- **`a` is the bias**, the average colour. Raise it and everything gets lighter; tint it and everything drifts towards that tint. It is where you set the overall key of the palette.
- **`b` is the amplitude**, how far each channel swings around its bias. Small `b` gives a subtle, tonal gradient; `b` equal to `a` makes each channel reach exactly 0 and exactly twice `a`. Larger than that and the channels clip, which is sometimes what you want and is always a decision.
- **`c` is the frequency**, how many full cycles each channel completes as `t` runs from 0 to 1. All ones gives one cycle, which is the usual choice. Making one channel's frequency different from the others is how you get palettes that do not simply loop.
- **`d` is the phase**, where each channel starts. This is the control that matters most. If the three components of `d` are equal, all three channels move together and you get a single hue changing in brightness. Separate them by about a third each and you get a rainbow, because the three cosines peak at three different points.

**The palette is continuous, and that is the point.** A stop-based gradient has to be sampled at a position. This one is a function of a real number, so you can hand it a distance field, an angle, a noise value, or an accumulated ray length, and it will answer smoothly. Every unit after this one drives it with something other than `x`.

**Wrapping is free.** `cos` is periodic, so `palette(t)` and `palette(t + 1.0)` are the same colour whenever `c` is a vector of integers. A palette driven by `fract(something)` therefore has no seam, which is why this course reaches for `fract` and this palette in the same breath.

**Clipping is the one thing to watch.** `a + b` above 1 or `a - b` below 0 means channels flatten at the ends of their swing. That is a legitimate look, and it is also how a palette that seemed fine at one `t` range turns into three flat bands at another. Clamp at the end, and if you are clamping a lot, lower `b`.

**Stay in code space.** As [Unit 04]({{ site.baseurl }}/learn/04-colour.html) said, a palette's numbers are not measurements of light. Do not convert them to linear. If you are compositing a palette against something photographic, convert the photograph, not the palette.

## Build it

The player has the four constants on colour pickers, four presets, and two views: the gradient as a strip with its three channels plotted underneath, and the same palette driven by a radial field.

{% include shader.html id="05-palette" height="420" pointer="none" caption="Set Preset to custom to reach the four pickers. The curves under the strip are the three cosines; watch them move as you change the phase picker, and the relationship between the numbers and the gradient stops being mysterious." %}

1. **Start at a preset and switch to custom.** The pickers hold whatever they held; the preset does not write into them. That is deliberate, so you can compare a preset against your own settings by flipping between them.
2. **Move `d` phase, one channel at a time.** Drag the red component of the phase picker and watch only the red curve slide. This is the single most useful thing to internalise about the form.
3. **Set all three components of `d` equal.** The rainbow collapses into one hue. Every monochrome palette in this course is that setting.
4. **Take `b` amplitude to nearly zero.** Everything becomes flat, the colour of `a`. Then raise `b` past `a` and watch the channels clip at the ends of the strip.
5. **Change `c` frequency on one channel only.** Try 2 in red and 1 in the others. The palette stops looping cleanly and starts producing colours it did not have before, which is where most of the interesting presets come from.
6. **Switch the view to the field.** The same function, driven by a radius and an angle instead of by `x`. Nothing about the palette changed; it is being asked about different numbers. That is the whole reason for using this form rather than a gradient of stops.
7. **Turn Drift up.** Adding `TIME` to `t` slides the palette through the picture. Because the palette wraps, there is no seam anywhere in the loop, which is the trick behind most animated colour in shader art.
8. **Write down four sets of constants you like.** You will need them. Copy them out of the pickers as `vec3` literals and keep them in a file.

## Look at these

{% include toy.html id="ll2GD3" title="Palettes" by="Inigo Quilez" note="The original. Seven sets of constants, each labelled; the fastest way to get a feel for the parameter space." %}
{% include toy.html id="XdBSzd" title="Palette in use" by="Shadertoy community" note="A fractal coloured entirely by driving this function with the escape distance." %}

Quilez's [article on palettes](https://iquilezles.org/articles/palettes/) is two pages and is the reference for this unit.

## Other palettes worth knowing

**Sampled gradients.** A one-dimensional texture, or an array of stops with a `mix` chain, is the right answer when a designer hands you exact colours and they are not negotiable. It is not smooth in the derivative, so it can band when driven by a field, and it costs a texture read.

**Scientific colour maps.** Viridis, magma, and their relatives are designed to be perceptually uniform and to survive being printed in greyscale. They are the correct choice when the colour is carrying data rather than mood, which in this course means Module F's debug views. Polynomial fits to them are widely published and are seven multiply-adds.

**Hue rotation in HSV.** Cheap, tempting, and consistently disappointing: HSV's hue axis moves through yellow and cyan far too fast, so a linear sweep spends most of its time in green. Use it to nudge an existing colour, not to build a gradient.

## Common mistakes

- **Tuning the palette at one `t` and shipping it.** Always look at the whole strip. A palette that is beautiful in the middle can be two flat clipped bands at the ends.
- **Forgetting to clamp.** Values outside 0 to 1 are not an error in a `float`, and they will do something unpredictable when they reach the display or the next process in a chain.
- **Driving it with an unbounded value.** If `t` can be any number, the palette cycles, which may be lovely or may be strobing. `fract` or a `smoothstep` to bound it is the fix.
- **Converting to linear because Unit 04 said so.** Unit 04 said to convert when combining, not when generating.
- **Building the palette from a `long` preset with an `if` chain and no custom path.** Presets are for the reader; the pickers are for you. A shader with only presets cannot be tuned in performance, which is exactly when you want to tune it.

## Exercise

Build a generator with a `float` input `t` driven by nothing but the distance from a `point2D` input `origin`, coloured by a cosine palette whose four constants are colour inputs.

Then add a `float` input named `bands`, which multiplies `t` before the palette sees it, and a `bool` named `wrap`, which chooses between `fract(t)` and `clamp(t, 0.0, 1.0)`.

**Success criterion:** with `wrap` on and `bands` at 4 you see four complete, seamless repetitions of the palette as you move outward from `origin`, with no visible line between them. With `wrap` off you see one palette and then a flat colour. If the repetitions have a hard seam, one component of your `c` frequency is not an integer.

## Going further

- [Inigo Quilez, palettes](https://iquilezles.org/articles/palettes/), the two-page original.
- [Matplotlib's perceptually uniform colour maps](https://matplotlib.org/stable/tutorials/colors/colormaps.html), for when colour carries data.
- [Björn Ottosson on OKLab](https://bottosson.github.io/posts/oklab/), for building a palette in a perceptual space instead.
