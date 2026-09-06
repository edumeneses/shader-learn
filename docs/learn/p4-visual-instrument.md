---
layout: default
title: "Milestone P4: a playable visual instrument"
description: "The fourth milestone. Not a piece and not a patch: an instrument, which is something a person can play, with the failure modes worked out in advance."
parent: Units
nav_order: 45
unit: "P4"
permalink: /learn/p4-visual-instrument.html
score_version: "3.8.2"
reading_time: "14 min"
practice_time: "90 min"
glsl: "GLSL ES 3.00"
---

# Milestone P4: a playable visual instrument

{% include unit_meta.html %}

> **Before this milestone** finish Units 35 to 41. Nothing here is new.
>
> **You will need** an hour and a half, *ossia score* {{ page.score_version }}, and something to play it with.
>
> **You will build** an instrument: a patch someone can pick up and perform, including you, tired, in a room.

## Why this matters

The previous milestones asked for a picture, a loop, and a scene that fits a budget. This one asks for something a person can *play*, and that is a different discipline again.

An instrument is not a piece. A piece has a shape you decided; an instrument has a range of behaviours somebody chooses between while it is running. It is not a patch either. A patch is a set of connections that works; an instrument is a set of connections that works *and* recovers, that says what it is doing, and whose controls are where a hand expects them.

The test is not whether it makes a good picture. It is whether it makes a good picture in someone else's hands, an hour into a set, when a cable has been unplugged.

## The brief

**One patch. Playable. Recoverable.**

Requirements:

1. **At least three of your own shaders**, chained, from at least two different modules of this course.
2. **A physical control surface**: MIDI, OSC from a phone, or a keyboard mapping. At least six controls a hand can reach without looking.
3. **Every control is role-named** and none is named after its driver. This is the course's central convention and P4 is where it is graded.
4. **At least one automated parameter** on a curve, so the instrument does something on its own between gestures.
5. **An audio input driving at least two parameters**, with bands, envelopes, and a noise floor from [Unit 40]({{ site.baseurl }}/learn/40-audio-reactive.html).
6. **Every link bypassable**, and a manual override for every automatic driver.
7. **It holds 60 frames per second at your output resolution** and you have measured it, with [Unit 34]({{ site.baseurl }}/learn/34-cost.html)'s method.
8. **A one-page written description**: what each control does, what to do if the audio fails, what to do if the camera fails, and what the safe state is.

## What makes something playable

**A range that is good everywhere.** A control whose useful region is a third of its travel is a control nobody can find under pressure. Set ranges so that anywhere is usable and the extremes are interesting rather than broken.

**Response that matches the gesture.** A fader that does nothing for half its travel then jumps is worse than one with less range. Curve the mapping in the patch, not in the shader.

**Controls that are independent.** If moving one requires compensating with another, you have made a puzzle. Test by moving each one alone through its full range.

**Something happening when nobody touches it.** An instrument that is static between gestures feels dead. One slow automation is usually enough.

**A safe state.** One gesture that returns everything to something you are willing to project. Write down what it is.

**Visible state.** You cannot see the projector. Meters, an on-screen readout, or the inspector's texture preview: something tells you what the patch thinks is happening.

## Build it

1. **Decide what it is for** before you connect anything. "Slow, dark, responds to bass." "Fast, graphic, mostly manual." An instrument without an intention becomes a pile of controls.
2. **Get one shader playable first.** One shader, one control surface, nothing else. Play it for ten minutes. Most of what you learn will be about ranges.
3. **Then build the chain.** [Unit 38]({{ site.baseurl }}/learn/38-chaining.html): generators, filters, a mixer, a grade last, every link bypassable.
4. **Add the audio.** [Unit 40]({{ site.baseurl }}/learn/40-audio-reactive.html): bands in the patch, not in the shader, smoothed separately, with a noise floor and a manual override.
5. **Add one automation.** Slow. It is the thing that keeps the instrument alive between gestures.
6. **Now map the surface.** Six controls minimum. Put the ones you reach for most where your hand naturally lands, which is a physical question about your controller and not a logical one about your patch.
7. **Measure.** [Unit 34]({{ site.baseurl }}/learn/34-cost.html), at the output resolution you will actually use.
8. **Break it on purpose.** Unplug the audio interface while it is running. Unplug the camera. Send a wildly out-of-range OSC value. Fix whatever failed badly.
9. **Write the page.** What each control does, what to do when each input fails, and what the safe state is. This is a real deliverable and it is the one most often skipped.
10. **Hand it to someone else** and watch without speaking. This is the whole milestone.

## What to expose, and what to hide

The hardest design decision in an instrument is which of the fifty parameters in
your chain reach the surface. A useful way to sort them.

**On the surface**, under a hand: the three or four things you will move
continuously while playing. Usually a brightness or density, a speed, a colour
position, and one structural control that changes what kind of picture it is.

**On a second layer**, a shift key or a bank switch: the things you change
between sections. Blend modes, which generator is running, which filter is in
the chain.

**Automated**: anything that should move on its own, and anything whose exact
value matters more than your ability to find it. A curve is more accurate than a
hand and it does not get tired.

**Set and left alone**: calibration. Noise floors, band gains, ranges, output
mapping. These belong in the patch and not on the surface, and putting them on
the surface is how a control gets knocked in the dark and nobody knows which.

The test for whether something belongs on the surface: **would you reach for it
while looking at the projection rather than at the controller?** If not, it is a
setting.

## Success criteria

- **Someone who has not seen it can make three visibly different pictures** in five minutes, using only the surface.
- **Unplugging the audio does not stop the picture.**
- **Every control does something across its whole range.**
- **You measured the frame rate** at output resolution, and wrote the number in the description.
- **The safe state is one gesture away** and you can perform it without looking.
- **The one-page description is written**, and someone else could run the patch from it.

## Common ways this goes wrong

- **Too many controls.** Six you can find beats twenty you cannot.
- **Ranges tuned while watching a monitor**, and used on a projector at a different brightness.
- **Everything driven by audio.** When the audio is quiet the instrument disappears, which is exactly when you needed it.
- **No manual override.** Something in the signal chain will fail during a set.
- **A chain with no bypass.**
- **Testing only on the music you like.** Check on the quietest and loudest material in the set.
- **No written description.** In six months this is someone else's patch, including when that someone is you.
- **Confusing an instrument with a piece.** If it only does one thing well, it is a piece; make it a good one and call it that.

## Going further

- [Live coding in *score*]({{ site.docs_baseurl }}/common-practices/8-live-coding.html), for the parts you will still want to change live.
- [Custom interfaces]({{ site.docs_baseurl }}/in-depth/custom-ui.html), for building a control surface inside *score* itself.
- [Unit 42]({{ site.baseurl }}/learn/42-capstone.html), which asks you to finish something.
