---
layout: default
title: "Unit 37: Parameters as inlets, and driving them from anything"
description: "The payoff for every naming decision this course has made. An input becomes an inlet, and an inlet takes an automation curve, an OSC message, a MIDI control, or another process's output."
parent: Units
nav_order: 40
unit: "37"
permalink: /learn/37-parameters-as-inlets.html
score_version: "3.8.2"
reading_time: "14 min"
practice_time: "35 min"
glsl: "GLSL ES 3.00"
---

# Unit 37: Parameters as inlets, and driving them from anything

{% include unit_meta.html %}

> **Before this unit** read [Unit 29]({{ site.baseurl }}/learn/29-isf.html) and [Unit 35]({{ site.baseurl }}/learn/35-score-pipeline.html).
>
> **You will need** *score* {{ page.score_version }} and a shader with at least six inputs.
>
> **You will build** the same shader driven four different ways, with nothing renamed between them.

## Why this matters

This is the unit the whole course has been arranged around. Every time a shader here declared `focus` instead of `mouse`, or `drive` instead of `audioLevel`, it was for this.

An ISF input becomes an inlet on a *score* process. An inlet accepts a value from anything in the patch: an automation curve, an OSC message from a phone, a MIDI fader, an envelope follower on an audio track, a value computed by a JavaScript process, or the output of another shader's analysis. The shader does not know and cannot know which of those it is receiving. It receives a number in the range its header declared.

That is the entire mechanism, and it is why a parameter named after a device is a parameter that has been prematurely committed. Rename `mouse` to `focus` and the same shader is playable from a fader, a phone, a curve, and a face tracker. It costs one word.

## The idea

**An input's type decides its inlet.** A `float` becomes a value inlet with the header's range. A `point2D` becomes a two-value inlet. A `color` becomes a colour inlet. A `bool` becomes a toggle, and an `image` becomes a texture inlet. The `LABEL` is what appears; the `NAME` stays in the code.

**Ranges are contracts, and this is verifiable rather than a claim.** *score* stores each value inlet with a **domain** taken directly from the header's `MIN` and `MAX`, and an initial value taken from `DEFAULT`. Open any `.score` document that uses a shader, which is JSON, and the inlets are there with their domains beside the header that produced them.

The consequence is the one to internalise: *score* uses the `MIN` and `MAX` to scale whatever arrives. An automation curve runs 0 to 1 in its own space and is mapped onto the inlet's range, so a well-chosen range means a curve drawn without thinking produces a sensible picture. A range that goes somewhere useless means every curve has to be drawn carefully. **Choosing ranges well is the single highest-leverage thing you can do for the person driving your shader**, including yourself in six months.

**An automation curve is the default driver.** Place an automation in the interval, address it at the inlet, draw a shape. This is the timeline's native way of changing something over time, and it is deterministic, repeatable, and editable, which live coding is not.

**An inlet's OSC name is the input's, lower-cased.** An input declared as `blurAmount` is exposed as `bluramount`. Worth knowing before you write an OSC layout against a shader, and worth avoiding names that differ only in case.

**OSC makes it playable from anywhere.** Declare an OSC device, and any address on it can drive an inlet. A phone running a TouchOSC layout, a Max patch, a Python script, a sensor rig: all the same to the shader.

**MIDI makes it playable from hardware.** A controller's faders and knobs become addresses, and those addresses drive inlets. For a live visual set this is usually what you actually want at your hands.

**Audio can drive a parameter directly.** An envelope follower or an analysis process produces a value, and that value goes to an inlet like any other. This is a different technique from an ISF `audioFFT` input and often the better one: analysis in the patch is visible, adjustable, and shared between processes, where analysis inside a shader is hidden in one. [Unit 40]({{ site.baseurl }}/learn/40-audio-reactive.html) compares them properly.

**Mapping belongs in the patch, not in the shader.** A shader should take a `warp` from 0 to 1 and not care what produced it. Curves, scaling, smoothing, and clamping go between the source and the inlet, where they can be adjusted without recompiling. A shader with a `midiCC7Scaled` input has done its own mapping and cannot be reused.

**Smoothing is almost always needed.** A raw controller value steps; a raw audio value jitters. A smoothing process between the source and the inlet costs nothing and is the difference between a parameter that feels connected and one that feels noisy. Frame-rate-independent smoothing, from [Unit 17]({{ site.baseurl }}/learn/17-time.html), is the right kind.

## Build it

Take any shader from this course with several inputs. The Module D and Module G shaders are good candidates because their parameters do large, visible things.

1. **Load it and look at the inlets.** Count them against the `INPUTS` array. Read the labels; they are what you gave them.
2. **Drive one from an automation curve.** Place an automation, address it at a `float` inlet, and draw a curve. Play. The parameter follows the curve.
3. **Notice the range.** The curve runs 0 to 1 in its own space and lands in the inlet's declared range. If your range was well chosen, a straight ramp already looks like something.
4. **Drive a `point2D` from two curves**, one per component, and watch the difference between moving a point and moving two numbers. Both are useful; they feel different.
5. **Add an OSC device and drive the same inlet from it.** Send from anything, including a two-line script. The shader is unchanged.
6. **Add a MIDI device and put a fader on it.** Again unchanged.
7. **Now put a smoothing process between the fader and the inlet**, and compare. The parameter stops stepping.
8. **Finally, drive one inlet from an audio envelope follower.** Four drivers, one shader, no renaming anywhere. That is the unit.

## Naming, one more time

The convention this course has kept for thirty-seven units, stated in full now that its payoff is visible:

**Name a parameter for its role in the picture, never for the thing that drives it.**

- `focus`, `origin`, `target`, `anchor`, not `mouse` or `touch`.
- `drive`, `intensity`, `swell`, not `audioLevel` or `micGain`.
- `warp`, `density`, `tilt`, `spread`, not `slider3` or `midiCC7`.
- `phase`, `period`, not `time` or `frameCount`.

The test is simple: if you cannot name a parameter without referring to a device, ask what that value *does* in the picture. Whatever the answer is, that is its name. A parameter that survives that question is a parameter that can be driven by something you have not thought of yet, and in live work something you have not thought of yet is most of the job.

## Common mistakes

- **No range at all on a float.** Its domain becomes 0 to 0 and every curve on it produces zero. This is in a shipped example, so it is not a rare mistake.
- **A range that produces a broken picture at one end.** Every curve then has to avoid it.
- **A range so wide the useful region is a sliver.** The reverse problem, and just as common.
- **Mapping inside the shader.** It hides the mapping and prevents reuse.
- **No smoothing on a hardware control.** It steps, and it reads as a bug in the shader.
- **Naming after a device.** The whole subject of this unit.
- **Exposing everything.** Forty inlets is not a control surface, it is a wall. Expose what someone would actually move.
- **Forgetting that removing an input removes its connections.** Settle the header before wiring the patch.

## Exercise

Take one shader and make it playable four ways at once: an automation curve on one parameter, an OSC address on another, a MIDI control on a third, and an audio-derived value on a fourth.

Requirements: no parameter is named after its driver; every hardware or audio source passes through smoothing; and every range is chosen so that a straight 0-to-1 ramp on it produces something worth looking at.

**Success criterion:** you can swap which driver feeds which parameter without touching the shader, and a straight ramp on any inlet gives a usable result. If swapping requires a shader edit, something is named after a device or a mapping is in the wrong place.

## Going further

- [*ossia score*'s device model]({{ site.docs_baseurl }}/reference-manual/references/protocols.html), for every protocol an inlet can be driven from.
- [Automations in depth]({{ site.docs_baseurl }}/in-depth/automations.html).
- [Milestone P4]({{ site.baseurl }}/learn/p4-visual-instrument.html), which is this unit as a whole instrument.
