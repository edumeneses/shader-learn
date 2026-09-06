---
layout: default
title: "Unit 18: Feedback, persistent buffers, and multi-pass shaders"
description: "Give a shader a buffer it can read next frame and it acquires memory. Trails, tunnels, spirals, and the whole vocabulary of video feedback follow from one flag in the header."
parent: Units
nav_order: 20
unit: "18"
permalink: /learn/18-feedback.html
reading_time: "14 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 18: Feedback, persistent buffers, and multi-pass shaders

{% include unit_meta.html %}

> **Before this unit** read [Unit 17]({{ site.baseurl }}/learn/17-time.html).
>
> **You will need** the player below. Leave it running; this is the first unit whose subject takes a few seconds to appear.
>
> **You will build** a feedback loop, and the pass structure that every remaining unit in Phase 2 depends on.

## Why this matters

Everything so far has been memoryless. Each frame is computed from scratch, which is what made [Milestone P2]({{ site.baseurl }}/learn/p2-living-surface.html)'s exact loop possible and is also a hard ceiling: nothing can accumulate, nothing can decay, nothing can depend on what happened before.

A **persistent buffer** removes the ceiling. Declare a render target that survives to the next frame, and a shader can read what it wrote. That single change unlocks a whole family: trails behind a moving object, video feedback tunnels, blurs that build over time, simulations, and everything in [Unit 19]({{ site.baseurl }}/learn/19-state-in-a-texture.html).

It is also the point at which a shader stops being a pure function and becomes a system with state. That is a real trade: you gain memory and you lose reproducibility, because the picture now depends on how long it has been running.

Anyone who has pointed a camera at the monitor it is feeding knows what this looks like. It is the same phenomenon, and it is worth noticing that the analogue version has the same controls: how much the image is zoomed and rotated between iterations, and how much it fades.

## The idea

**A pass is one full-screen draw.** A shader can declare several, and they run in order within one frame. Each pass can write to a named target instead of to the screen, and later passes can read those targets.

```
"PASSES": [
  { "TARGET": "history", "PERSISTENT": true, "FLOAT": true },
  { }
]
```

The first pass writes `history`. The second has no target, so it draws to the output. `PASSINDEX` tells the shader which pass it is in, and a single `main` handles both with an `if`.

**`PERSISTENT` is the whole unit.** Without it, a target is scratch space cleared each frame. With it, the target survives, and the pass that writes it can also read it: `IMG_NORM_PIXEL(history, uv)` in the pass that targets `history` returns what was there last frame.

Implementations do this with two buffers and a swap, which is why reading and writing the same target in one pass is safe. You are not reading what you are writing; you are reading the other copy.

**`FLOAT` matters more than it sounds.** An 8-bit buffer holds 256 levels, and a feedback loop multiplies by a decay factor every frame. At 8 bits, a value of 3 multiplied by 0.98 rounds back to 3 and never decays; trails get stuck at low levels and leave permanent smears. A float buffer decays smoothly and can also hold values above 1, which lets the accumulation stay linear and the tone mapping happen once at the end.

**The transform is the whole art.** Read the previous frame at a *transformed* coordinate rather than at the same one, and the character of the feedback is entirely determined by that transform. Zoom alone gives a tunnel. Rotation alone gives a spiral. A translation gives a smear. Combinations give the shapes everyone recognises.

**And the transform is inverted**, exactly as in [Unit 09]({{ site.baseurl }}/learn/09-transforms.html). To make the image appear to zoom outward, the lookup zooms inward. This catches people in a place they are not expecting the rule.

**Decay decides whether it is a trail or an accumulation.** Multiply the previous frame by slightly less than 1 and old material fades; at exactly 1 nothing ever leaves and the buffer saturates to white within seconds. Both are useful and the second needs the source to be dim.

**Edges need thought.** A lookup outside the buffer returns the clamped edge texel, which smears the border inward and is one of the most recognisable artefacts of a badly built feedback shader. Fade to nothing near the edge instead.

**The cost is real.** A persistent float target at full resolution is 16 megabytes at 1080p, doubled for the swap, and it is written and read every frame. That is bandwidth rather than arithmetic, and it is the kind of cost that does not show up in an instruction count.

## Build it

{% include shader.html id="18-feedback" height="440" pointer="drag" caption="Give it a few seconds: unlike every previous player, this one has to build its picture. Drag the canvas to push the accumulated image around. Take Decay to 1.0 and watch it saturate; take it to 0.85 and watch the trail become short." %}

1. **Let it run for ten seconds.** A source travels on a closed path and leaves a trail; the trail is being zoomed and twisted a little every frame, which turns it into a structure rather than a line.
2. **Take Zoom to exactly 1.0 and Twist to 0.** The structure collapses to a plain trail. Everything else was the transform.
3. **Raise Zoom to about 1.03.** A tunnel. Take it below 1 and the material flows inward instead. Note that the source stays put while everything else moves, which is the tell for feedback as opposed to a moving camera.
4. **Set Zoom back to 1 and raise Twist.** A spiral. Then use both, which is the classic.
5. **Take Decay to 1.0.** Nothing fades, and within a few seconds most of the frame is white. Take it to 0.85 and the trail is barely a smear. The useful range is narrow and very sensitive; 0.97 to 0.99 is where most of this lives.
6. **Turn Hue drift up.** The colour of a region now depends on how long ago it was written, which is something no memoryless shader can do. This is the effect worth taking away from the unit.
7. **Press Clear, then release it.** The buffer resets and the picture rebuilds. Note how long it takes to settle; that duration is exactly what makes a feedback shader awkward to render deterministically, and `scripts/render.py` has a `--settle` option for it.
8. **Read the source and find the two passes.** `PASSINDEX == 0` accumulates and `PASSINDEX == 1` tone-maps. Note that the tone mapping is in the display pass, not in the accumulation: keeping the buffer linear is why the bright regions build smoothly rather than clipping.

## Look at these

{% include toy.html id="Xsf3Rn" title="Video feedback" by="Shadertoy community" note="A minimal buffer-A feedback with a rotation and a zoom; the same structure as this unit's shader." %}
{% include toy.html id="MdlXz8" title="Buffer-based blur" by="Shadertoy community" note="Feedback used for accumulation rather than for trails, which is Unit 21's other route to a large blur." %}

## Feedback in *ossia score*

*score*'s graphics pipeline is a render graph, so a shader's output can be routed back into another process's input. That is feedback at the patch level rather than inside one shader, and it has a different feel: the transform between iterations is a whole other process, which can be anything the library offers.

Both approaches are worth having. Inside one shader is precise and self-contained, which is what makes it portable. At the patch level it is reconfigurable during a performance, which is what makes it playable. [Unit 38]({{ site.baseurl }}/learn/38-chaining.html) builds the second.

## Common mistakes

- **Forgetting `FLOAT`**, so trails stick at low values and leave permanent smears. This is the most common feedback bug and it looks like a shader problem rather than a format one.
- **Decay at exactly 1.0** with a bright source, giving a white frame within seconds.
- **Reading the clamped edge**, smearing the border inward.
- **Tone-mapping in the accumulation pass**, so the buffer is no longer linear and bright regions clip instead of building.
- **Expecting it to loop.** A feedback shader depends on its whole history, so [Milestone P2]({{ site.baseurl }}/learn/p2-living-surface.html)'s periodic-input trick does not apply. Getting a feedback shader to loop means letting it settle into a periodic orbit, or accepting a crossfade.
- **Expecting a still to be reproducible.** A single frame of a feedback shader depends on how long it has been running. Render it by stepping from frame zero, which is what this repository's renderer does automatically for any shader with a persistent pass.
- **Full resolution when half would do.** A feedback buffer is often the most bandwidth-hungry thing in a patch, and halving its size quarters the cost for a difference nobody sees through a trail.

## Exercise

Build a feedback shader in which the transform applied between frames is *not* uniform across the image: the zoom or rotation should vary with position, so that different regions of the trail flow differently.

Requirements: the varying transform must come from a field you already know how to write, from Module C or D; a `float` named `flow` scales its strength; and there must be no edge smear at any setting.

**Success criterion:** at `flow` zero the shader behaves like an ordinary uniform feedback, and as `flow` rises the trail develops eddies and currents rather than simply spiralling. If the picture fills with a smear from one edge, the lookup is leaving the buffer and being clamped.

## Going further

- [The ISF specification on passes]({{ site.isf_baseurl }}/), for `PERSISTENT`, `FLOAT`, and the sizing keys.
- [*ossia score*'s video mixing]({{ site.docs_baseurl }}/common-practices/11-video-mixing.html), for the patch-level version.
- [Unit 19]({{ site.baseurl }}/learn/19-state-in-a-texture.html), next, where the buffer stops holding a picture and starts holding a simulation.
