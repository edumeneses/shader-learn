---
layout: default
title: "Milestone P2: a living surface that loops seamlessly"
description: "The second milestone. One animated texture that repeats exactly, with no crossfade and no cut, because the path through the noise is a closed one."
parent: Units
nav_order: 18
unit: "P2"
permalink: /learn/p2-living-surface.html
reading_time: "12 min"
practice_time: "60 min"
glsl: "GLSL ES 3.00"
---

# Milestone P2: a living surface that loops seamlessly

{% include unit_meta.html %}

> **Before this milestone** finish Units 12 to 16. Nothing here is new.
>
> **You will need** an hour, and a way to play a video file back on repeat.
>
> **You will build** one animated texture, six to twelve seconds long, that repeats with no visible seam.

## Why this matters

Almost every piece of visual work that runs for longer than its content has to loop. A projection that plays for four hours, a texture on a screen in a set, a background behind a talk, a piece in a gallery on the day nobody resets it: all of them are a short clip repeating, and the seam is what gives it away.

The usual answer is a crossfade, and it is a bad answer. A crossfade means that for a second at the end of every loop, the picture is a double exposure of itself: it goes soft, the contrast drops, and once a viewer has noticed it they cannot stop noticing it.

There is a better answer, and it costs nothing at all. Make the *input* periodic. A noise field is a fixed function of position, so if you travel through it on a closed path rather than in a straight line, you arrive back where you started and the picture repeats exactly. No fade, no cut, no stored frames, and the loop is exact to the last bit.

That idea is the whole milestone. It is small, it is not obvious, and it is one of the most useful things in this course.

## The idea, in two lines

```glsl
float phase = TAU * TIME / period;
vec2 loop = travel * vec2(cos(phase), sin(phase));
```

Then use `loop` as an offset into the noise instead of using `TIME` directly. After `period` seconds the phase has gone round once, `loop` is exactly where it started, and every noise lookup in the shader is being asked the same question it was asked at time zero.

Because nothing in the shader has any memory, asking the same question means getting the same answer, so frame zero and frame `period` are the same image. Not similar: the same.

**Where to apply the offset matters.** Adding it to the coordinate slides the whole field past the window, which looks like a pan. Adding it at the **warp** stage of [Unit 15]({{ site.baseurl }}/learn/15-domain-warping.html) makes the field churn in place, which looks like flow. The second is almost always what you want.

**Two dimensions is one circle; three is better.** The circle trick uses two of the noise's dimensions for the loop. If you have three-dimensional noise, you can use a circle in the third and fourth dimensions and leave both spatial ones free, which avoids the slight rotational bias a two-dimensional circle imposes. This course stays in two dimensions and the bias is not visible at these settings; the checks note for this milestone says so honestly.

## The brief

**One shader. One animated texture. It must loop exactly.**

Requirements:

1. **It repeats with no crossfade**, by a periodic input rather than by blending frames.
2. **The loop length is a named input in seconds.** Not a constant.
3. **It uses at least two structures from Module D**, chosen from fbm, ridged or turbulent fbm, domain warping, and Voronoi.
4. **It reads as a material**, not as a pattern. Someone should be able to say what it is made of.
5. **At least eight named inputs**, none named after a device, all with sensible ranges.
6. **It is shaded**, using the field's own gradient, so it has relief rather than only colour.
7. **It survives being rendered to a six-second 1080p clip** and played on repeat without you being able to see where the join is.

## The reference solution

{% include shader.html id="p2-living-surface" height="440" pointer="none" caption="Two levels of domain warping and a Voronoi, both travelling on the same closed path. Set Loop length to 4 and watch it repeat; the join is not findable because there is no join." %}

And here is the same shader rendered to an eight-second clip and played on repeat, which is the actual deliverable. Watch it for a while and try to find the join.

{% include figure.html unit="p2" name="p2-01" video=true alt="A churning, cracked, rust-coloured surface, animating and repeating every eight seconds with no visible seam" caption="Eight seconds, looping. Rendered with scripts/render.py at 1280 by 720; the render spec is figures/p2.json. There is no crossfade anywhere in it: frame 240 is bit-for-bit frame 0." %}

Read its source and note three things:

- **The closed path is applied at the warp stage**, not to `p`. The surface churns rather than slides.
- **The Voronoi travels on the same path**, scaled by 1.6. Any constant works; what matters is that it is the *same* path, or the two structures would have different periods and the combination would only repeat at their common multiple.
- **The lacunarity is 2.02, not 2.0**, for the reason [Unit 14]({{ site.baseurl }}/learn/14-fbm.html) gave.

## Build it

1. **Get the still right first.** Turn Loop length up to its maximum so the motion is very slow, and tune the surface as though it were a still image. Everything in [Milestone P1]({{ site.baseurl }}/learn/p1-poster.html) applies.
2. **Then add the loop.** Replace whatever `TIME` offset you were using with the two lines above.
3. **Check the loop arithmetically before you check it by eye.** From this repository:

   ```bash
   python3 scripts/render.py library/shaders/p2/living-surface.fs \
       --out /tmp/a --formats png --size 320x180 --time 0
   python3 scripts/render.py library/shaders/p2/living-surface.fs \
       --out /tmp/b --formats png --size 320x180 --time 8
   ```

   with the shader's `period` at 8. The two files should be identical to within rounding. The reference solution differs by at most one value out of 255 per channel, which is float precision and nothing else. If yours differs by more, something in the shader is reading `TIME` directly.

4. **Now render the clip.**

   ```bash
   python3 scripts/render.py library/shaders/p2/living-surface.fs \
       --out docs/learn/assets/p2/loop --formats mp4,gif \
       --size 1920x1080 --duration 8 --fps 30
   ```

   The duration must equal the period exactly, or the clip will contain part of a second lap and the join will jump.

5. **Play it on repeat and watch for two minutes.** Not ten seconds. A seam you cannot see in one loop becomes obvious in twelve, because your eye learns the cycle.
6. **Tune the period.** Too short and the repetition itself is visible even though the join is not; too long and the file is large. Six to twelve seconds is the useful range for a texture, and the tell is whether a viewer starts recognising individual features.

## What loops and what cannot

It is worth being precise about the limit of this method, because the next module runs straight into it.

The trick works because a fragment shader has **no memory**. Every frame is computed from scratch from its inputs, so identical inputs give an identical frame, and making the inputs periodic is therefore sufficient. Nothing else is required and nothing can go wrong.

The moment a shader keeps state between frames, which is what a persistent buffer in [Unit 18]({{ site.baseurl }}/learn/18-feedback.html) is for, that guarantee is gone. A feedback trail at time `period` depends on every frame before it, not only on the current inputs, so the field does not return to where it started merely because its inputs did. Such a shader can be made to loop, and doing it requires either running it long enough that the state settles into a periodic orbit of its own, or accepting a crossfade after all.

That is the real reason this milestone comes before Module E rather than after it. A loop built on a memoryless shader is exact and free; a loop built on a stateful one is a negotiation.

## Success criteria

- **Frames at 0 and at `period` are identical**, verified by rendering both and comparing, not by looking.
- **The clip's duration equals the period.**
- **Two minutes of repeated playback does not reveal the join.**
- **You can name the material.** "Rusted iron", "oil on water", "lichen". If the answer is "noise", requirement 4 is not met.
- **Every parameter is named for its role.** No `mouseX`, no `audioLevel`, no `time2`.

## Common ways this goes wrong

- **A `TIME` left somewhere.** One forgotten direct use of `TIME` breaks the loop completely, and the symptom is a hard jump at the join. Search the file.
- **Two structures with different periods.** Both must travel the same closed path, or the true period is the least common multiple and your clip is a fragment of it.
- **Clip duration not equal to period.** The commonest mistake, and it looks exactly like a broken loop.
- **Travelling too far.** A large travel radius means the loop passes through very different regions of the field, so the motion is a tour rather than a churn. Keep it under about one lattice cell.
- **Crossfading anyway, out of habit.** If you are blending frames, you have not done the milestone.
- **Judging the loop in the player.** The player's `loop` attribute wraps `TIME`, which proves the shader is periodic but not that your encoded clip is. Check the file.

## Going further

- [Inigo Quilez, on looping noise](https://iquilezles.org/articles/), for the higher-dimensional version.
- [Unit 17]({{ site.baseurl }}/learn/17-time.html), next, which takes loops, phase, and easing seriously.
- [Unit 18]({{ site.baseurl }}/learn/18-feedback.html), which introduces the one thing this milestone forbids: memory between frames, and therefore motion that cannot loop this way.
