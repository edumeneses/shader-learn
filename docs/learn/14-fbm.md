---
layout: default
title: "Unit 14: Fractal Brownian motion: octaves, lacunarity, gain"
description: "One noise function has one feature size, which is why it looks fake. Sum copies of it at falling scales and rising frequencies and you get something that looks like a surface."
parent: Units
nav_order: 15
unit: "14"
permalink: /learn/14-fbm.html
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 14: Fractal Brownian motion: octaves, lacunarity, gain

{% include unit_meta.html %}

> **Before this unit** read [Unit 13]({{ site.baseurl }}/learn/13-noise.html).
>
> **You will need** the player below, on its octave view.
>
> **You will build** the eight-line loop that turns noise into terrain, cloud, rust, and smoke.

## Why this matters

A single noise function produces blobs of one size. It is smooth, it is random, and it does not look like anything, because nothing in the world has features at exactly one scale. A mountain has ridges, and boulders on the ridges, and stones on the boulders, and grit on the stones, all the way down until you stop looking.

Fractal Brownian motion is the loop that reproduces that: sum several noise functions, each one finer and quieter than the last. It is eight lines, it is the single most-used technique in procedural texturing, and its two parameters, **lacunarity** and **gain**, control the entire character of the result.

Almost every natural-looking procedural texture you have ever seen is this loop with different numbers.

## The idea

**The loop.**

```glsl
float fbm(vec2 p) {
    float sum = 0.0, amp = 0.5, norm = 0.0;
    for (int i = 0; i < OCTAVES; i++) {
        sum += amp * noise(p);
        norm += amp;
        p *= lacunarity;
        amp *= gain;
    }
    return sum / norm;
}
```

Each pass through, the coordinate is multiplied so the noise gets finer, and the amplitude is multiplied so its contribution gets quieter. `norm` accumulates the total amplitude so the result stays in a predictable range whatever the settings.

**An octave is one pass.** The word comes from music, and it means the same thing: with `lacunarity` at 2.0, each pass doubles the frequency, which is one octave up. Four to six octaves covers most needs. Each one costs a full noise evaluation, so eight octaves is eight times the price of one.

**Lacunarity is how much finer each octave is.** Two is the default and produces a natural progression. Below two, octaves crowd together and the result is soft and mushy. Above three, they separate into visibly distinct layers of detail, which looks artificial and is occasionally exactly what you want.

**Gain is how much quieter each octave is.** Half is the default. Gain and lacunarity together decide the roughness: `gain = 1 / lacunarity` gives amplitude inversely proportional to frequency, which is what most natural surfaces measure and is why 2.0 and 0.5 are the pair everyone uses. Raise the gain and the fine detail dominates, giving a harsh, sandy surface; lower it and only the largest octave survives, giving smooth hills.

**The first octave decides the composition and never moves.** This is the most useful practical fact in the unit. Later octaves add detail to an outline that the first one already fixed, so if the overall shape is wrong, changing the octave count will not fix it. Change the base scale or the seed.

**Two variants worth having as switches.** `abs(n)` folds the field at zero, creating a crease wherever the noise used to pass through the middle; summed, that gives **turbulence**, and it looks like smoke and flame. `1 - abs(n)` inverts the fold so the creases become sharp peaks, giving **ridged** noise, which looks like mountain ridges and is where the name comes from. Both are one line inside the loop.

**Centre the noise before summing.** If your noise returns 0 to 1, every octave adds a positive bias and the sum drifts upward as octaves are added. Return it centred on zero and the sum stays put, which is why the shader in this unit does.

**Do not use exactly 2.0 for lacunarity if you can help it.** At exactly 2.0 every octave's lattice lines up with every other octave's, so the grid from [Unit 13]({{ site.baseurl }}/learn/13-noise.html) can reinforce itself into a visible artefact. 2.02 costs nothing and breaks the alignment.

## Build it

{% include shader.html id="14-fbm" height="440" pointer="none" caption="Three views. The sum is the result; octaves side by side shows each one alone, at its real scale and loudness; the running sum shows what the picture looks like after one, two, three octaves and so on." %}

1. **Start on octaves side by side.** Each column is one octave on its own. Read them left to right: each is `lacunarity` times finer and `gain` times quieter than the one before. Everything about the two parameters is in that picture.
2. **Change Lacunarity and watch the columns.** At 1.2 they are nearly the same picture. At 4 they are visibly unrelated scales with a gap between them. The default 2 is the value at which they read as one family.
3. **Change Gain and watch the columns.** The scale is unchanged and the contrast falls off faster or slower. Gain is loudness, lacunarity is size, and the octave view is the only place that is obvious.
4. **Switch to the running sum.** Column one is the first octave alone; column two is the first two summed, and so on. Watch the outline: it is settled by the end of the first column and never changes again. Everything after adds texture to a shape that was already decided.
5. **Go back to the sum and raise Octaves from 1 to 8.** Note where you stop being able to see a difference. That point is where you should stop paying, and it is usually around five or six.
6. **Turn Ridged on.** The creases become peaks and the field starts to look like terrain seen from above. Turn Turbulent on instead and it looks like smoke. One `abs` separates the two.
7. **Set Lacunarity to exactly 2.0 and look for a faint grid** at high octave counts and low base scale. Then set it to 2.02. This is a small thing that has cost people whole afternoons.

## Look at these

{% include toy.html id="4ttSWf" title="Rainforest" by="Inigo Quilez" note="fbm doing everything: terrain, trees, mist, and the variation between them. The reference for the technique's ceiling." %}
{% include toy.html id="MdX3Rr" title="Elevated" by="Inigo Quilez" note="One ridged fbm as a heightfield, raymarched. Read it after Module G." %}
{% include toy.html id="XdfGRn" title="fbm with analytic derivatives" by="Inigo Quilez" note="The version that returns the gradient too, which makes lighting free and warping cheaper." %}

Quilez's [fbm article](https://iquilezles.org/articles/fbm/) covers the derivative-aware variants.

## The derivative trick

A noise function can return its own gradient alongside its value for very little extra cost, and summing the gradients alongside the values gives you the fbm's gradient for free. Two things become much better:

**Lighting.** The gradient of a heightfield is its normal. With it, a texture can be lit without any extra samples, and the relief in the player above uses a screen-space approximation of exactly this.

**Erosion-like detail.** If each octave's contribution is divided by one plus the accumulated gradient so far, steep regions get less fine detail than flat ones. That single change makes fbm terrain look eroded rather than uniformly rough, and it is the difference between Quilez's terrain shaders and everyone else's.

Both need noise that returns a `vec3` rather than a `float`, which is the version in his article.

## Common mistakes

- **Summing noise that is not centred on zero**, so the field drifts brighter with every octave.
- **Adding octaves to fix a composition.** The composition is the first octave. Change the base scale instead.
- **Using more octaves than the resolution can show.** An octave whose features are smaller than a pixel is pure cost and pure aliasing. [Unit 11]({{ site.baseurl }}/learn/11-antialiasing.html) applies here, and the fix is to stop the loop when the octave's period drops below a pixel.
- **Exactly 2.0 lacunarity** with a visible lattice.
- **Not normalising by the summed amplitude**, so the output range changes whenever you change gain and every downstream threshold has to be retuned.
- **Assuming turbulence and ridged noise are different functions.** They are one `abs` in different places.

## Exercise

Build a heightfield shader that renders fbm as a shaded surface, using the field's screen-space derivative for the normal, and add a `bool` input named `erode`.

When `erode` is on, divide each octave's contribution by one plus the length of the accumulated gradient, so that steep regions receive less fine detail.

**Success criterion:** with `erode` off, the surface is uniformly rough everywhere. With it on, the flat regions keep their fine detail and the steep slopes become smoother, and the result reads as a landscape that water has run down. If nothing changes, your accumulated gradient is being reset each octave rather than carried.

## Going further

- [Inigo Quilez, fbm](https://iquilezles.org/articles/fbm/), including the derivative-aware form.
- [Inigo Quilez, on terrain rendering](https://iquilezles.org/articles/morenoise/), for the erosion trick in context.
- [Ken Musgrave's work on multifractals](https://en.wikipedia.org/wiki/Fractal_landscape), for the version where gain varies with altitude.
