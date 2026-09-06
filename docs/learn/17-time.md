---
layout: default
title: "Unit 17: Time, phase, easing, and loops that actually loop"
description: "Never use TIME directly. Convert it to a phase that repeats, shape the phase with an easing curve, and offset the phase per object. Three habits, and most animation problems disappear."
parent: Units
nav_order: 19
unit: "17"
permalink: /learn/17-time.html
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 17: Time, phase, easing, and loops that actually loop

{% include unit_meta.html %}

> **Before this unit** read [Milestone P2]({{ site.baseurl }}/learn/p2-living-surface.html).
>
> **You will need** the player below.
>
> **You will build** three habits about time that will save you from most of the animation bugs in this field.

## Why this matters

`TIME` is a number that grows. Almost everything people do with it directly is a mistake, and the mistakes share a shape: a value that grows without bound loses precision, cannot repeat, cannot be scrubbed, and cannot be offset per object without arithmetic that gets confusing fast.

Three habits fix all of it. Convert time into a **phase**, a number from 0 to 1 that repeats. Shape the phase with an **easing curve**, a function from 0 to 1 to 0 to 1. Offset the phase per object to make a **cascade**. None of these is difficult and all three are the difference between animation that looks designed and animation that looks like a variable being multiplied.

There is also a practical reason, and it is *ossia score*. In a score, a process runs inside an interval with a known duration, and the natural driver is not the wall clock but the position within that interval. A shader written against a phase drops straight into that; a shader written against `TIME` has to be rewritten. [Unit 37]({{ site.baseurl }}/learn/37-parameters-as-inlets.html) is where that arrives, and it arrives painlessly if the habit is already there.

## The idea

**Phase, not clock.**

```glsl
float phase = fract(TIME / period);
```

`period` is in seconds and is a named input. `phase` runs 0 to 1 and repeats. It never grows, so it never loses precision; a shader driven this way is as accurate after eight hours as after eight seconds, which a shader multiplying a growing `TIME` by a large number is not.

**Ping-pong, so the motion returns.** A raw phase jumps from 1 back to 0, which reads as a glitch. Folding it makes the motion travel back the way it came:

```glsl
float pingPong = 1.0 - abs(phase * 2.0 - 1.0);
```

**An easing curve is a function from 0 to 1 to 0 to 1.** That is the whole definition, and once it lands you can write your own rather than looking one up. `smoothstep`'s cubic is the default. Its quintic relative is smoother still. `t * t` starts slow and accelerates; `1 - (1-t)²` does the reverse.

**A curve may leave its own range, and the good ones do.** `back` overshoots past 1 and settles; `elastic` oscillates around it. Motion that overshoots slightly reads as having mass, and motion that arrives exactly and stops reads as computed. This is the single most effective thing you can do to make an animation feel deliberate.

**Cascade by offsetting the phase.** The same animation, started at slightly different times, is most of what makes a sequence look choreographed:

```glsl
float phase = fract(TIME / period - index * spread);
```

One subtraction. A row of objects with a cascade reads as a wave passing through them, and a row without one reads as a row.

**Separate the easing from the thing being eased.** Compute a phase, ease it, and then use the eased value to interpolate position, colour, size, or anything else. Once they are separate, changing the feel of a movement is one dropdown and does not touch the geometry.

**Do not ease inside a loop you also want to loop.** An easing curve that does not start and end at the same value will jump when the phase wraps. `smoothstep` goes 0 to 1, so it jumps; ping-pong first, or use a curve that returns.

## Build it

{% include shader.html id="17-time" height="440" pointer="none" caption="The curve is plotted above the motion it produces, with a marker riding it at the current phase. The faint diagonal is linear, for reference: anything above it is running ahead of time and anything below is running behind." %}

1. **Start on smoothstep with Ping-pong on.** Watch the marker travel the curve while the dots travel the screen. Slow and fast on the plot correspond to slow and fast in the motion, and seeing the two together is the point of the figure.
2. **Switch to linear.** The plot becomes the diagonal and the motion becomes mechanical. This is what a shader that multiplies `TIME` by a speed looks like.
3. **Switch to ease in, then ease out.** One starts slow and arrives fast; the other does the reverse. Both are asymmetric, and asymmetry is what makes a movement have a direction.
4. **Switch to back.** The dots overshoot and settle, and the plot shows the curve going above 1 with a red band marking where. This is worth staring at: an easing curve is not required to stay in its range, and the ones that leave are the ones that feel physical.
5. **Switch to elastic.** More of the same, further. Note how much more expensive it is; a `pow` and a `sin` per invocation is not free, and [Unit 34]({{ site.baseurl }}/learn/34-cost.html) will say so.
6. **Turn Phase spread from 0 to 1** with a few followers. At zero they move as one block. At anything above about 0.2 a wave passes through them. That control is one subtraction in the source.
7. **Turn Ping-pong off.** The dots jump back to the start each cycle. Now turn Period down to about 0.6 and watch how much worse the jump looks when it happens often.
8. **Read `phaseAt`.** Four lines, and the `fract` of a division is the whole habit.

## Look at these

{% include toy.html id="4dS3Rd" title="Useful easing functions" by="Inigo Quilez" note="Impulse, parabola, power curve, and gain; a set that overlaps very little with the web animation canon and is more useful in shaders." %}
{% include toy.html id="MsSSWV" title="Easing comparison" by="Shadertoy community" note="The standard easing catalogue, plotted, for when you want the named ones." %}

Quilez's [useful functions article](https://iquilezles.org/articles/functions/) is the reference for the shader-specific curves, which are mostly not the ones a web animation library ships.

## Curves worth knowing that are not eases

**Impulse**, `k * x * exp(1 - k * x)`, rises sharply to 1 and decays. It is the shape of a hit, and it is what to reach for when something should react to an event rather than travel between two states. [Unit 40]({{ site.baseurl }}/learn/40-audio-reactive.html) uses it heavily.

**Exponential decay**, `exp(-k * t)`, is what a physical thing does when you stop pushing it. Approaching a target by `mix(current, target, 1 - exp(-k * dt))` rather than by a fixed fraction is the frame-rate-independent version, and it is the correct way to smooth a control.

**Parabola**, `4 * x * (1 - x)`, is 0 at both ends and 1 in the middle. Useful for anything that should appear and disappear within one cycle.

**Gain**, which bends a curve towards its ends or its middle with one parameter, is the closest thing to a general-purpose shaping knob and is worth having in every project.

## Common mistakes

- **Multiplying `TIME` by a large number.** After a few hours the float has lost enough precision that motion becomes visibly steppy. This is a real bug in installations and it takes a day to reproduce.
- **A raw `fract` phase with a hard reset**, giving a jump every cycle.
- **Easing a value that then wraps.** `smoothstep` ends at 1 and starts at 0, so the wrap jumps. Ping-pong first.
- **Baking the easing into the geometry**, so changing the feel means editing the shape.
- **Using a frame-fraction for smoothing.** `mix(a, b, 0.1)` per frame is frame-rate dependent, so a shader that feels right at 60 frames per second is twice as fast at 120. Use `1 - exp(-k * TIMEDELTA)`.
- **Assuming `TIME` starts at zero.** In *ossia score* it follows the transport, and in a browser it starts when the player did.

## Exercise

Build a shader with a row of shapes that animate on a cascade, and expose the easing curve as a `long` input with at least four choices.

Requirements: the animation must loop with no jump at any setting; a `float` named `period` sets the cycle length in seconds; a `float` named `spread` sets the cascade; and a `bool` named `hold` freezes the phase where it is without stopping the shader, so a still can be taken at any point in the cycle.

**Success criterion:** at every easing setting, watching for a minute reveals no discontinuity. `hold` freezes the picture exactly, with no drift. If `hold` slowly drifts, you froze the speed rather than the phase, which is a different thing and a good bug to have found.

## Going further

- [Inigo Quilez, useful functions](https://iquilezles.org/articles/functions/), the shader-specific set.
- [Easings.net](https://easings.net/), for the named web-animation curves and their formulas.
- [Unit 37]({{ site.baseurl }}/learn/37-parameters-as-inlets.html), where a phase becomes an automation curve in *ossia score* and the habit pays off.
