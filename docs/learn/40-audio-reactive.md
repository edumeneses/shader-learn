---
layout: default
title: "Unit 40: Audio-reactive shaders, done properly"
description: "Bands rather than levels, separate attack and release, onset apart from envelope, and every mapping exposed. The difference between a shader that pumps and one that reads as music."
parent: Units
nav_order: 43
unit: "40"
permalink: /learn/40-audio-reactive.html
score_version: "3.8.2"
reading_time: "14 min"
practice_time: "35 min"
glsl: "GLSL ES 3.00"
---

# Unit 40: Audio-reactive shaders, done properly

{% include unit_meta.html %}

> **Before this unit** read [Unit 17]({{ site.baseurl }}/learn/17-time.html) and [Unit 37]({{ site.baseurl }}/learn/37-parameters-as-inlets.html).
>
> **You will need** the player below, and *score* with an audio source.
>
> **You will build** a signal chain from sound to picture that does not pump.

## Why this matters

Audio-reactive visuals are the most common thing anyone does with shaders in a live context, and most of them are bad in the same way. One number, usually an overall level, drives everything at once. The whole image breathes together on every kick. It reads as a meter with a texture on it.

What separates that from work that reads as *music* is almost entirely signal processing, and almost none of it is in the shader. Different bands driving different properties, with different time constants, so that the picture has a bass layer that swells slowly and a treble layer that flickers. An onset that is a separate signal from an envelope. A noise floor so that room tone does not animate anything. Smoothing that behaves the same at any frame rate.

None of that is difficult. All of it is skipped.

## The idea

**Bands, not a level.** Split the spectrum into at least three: bass, mids, treble. An FFT texture's x axis is linear in frequency, so the bass is a small slice near zero and the treble is most of the width. Sum a range of bins rather than reading one: a single bin is noisy, and it moves whenever the pitch does, which makes the whole patch depend on what key the music is in.

**A noise floor before anything else.** Subtract a threshold and rescale. Without it, room tone and preamp hiss drive the visuals and the piece never sits still between cues. This is the single most useful control on the shader below.

**Attack and release are different numbers.** A visual envelope should rise fast and fall slowly, because that is what an eye reads as a hit. One smoothing constant gives you either a sluggish response or a jittery one.

**Smoothing must be frame-rate independent.** `mix(current, target, 0.1)` per frame is twice as fast at 120 frames per second as at 60. Use `1 - exp(-dt / tau)`, where `tau` is a time in seconds. This is [Unit 17]({{ site.baseurl }}/learn/17-time.html)'s rule, and audio-reactive work is where breaking it is most obvious, and where it only shows up on the venue's machine.

**Onset is not envelope.** An envelope says how loud it is now; an onset says something just started. They drive different things: an envelope drives a size or a brightness, an onset drives a flash or a cut. Deriving onset from the *rise* of a band, held with a fast decay, is enough for visual work.

**Different bands on different properties, with different time constants.** This is the whole aesthetic argument. Bass on scale with a slow release, mids on a warp with a medium one, treble on fine detail with a fast one, and the picture acquires layers. Everything on one signal and it pumps.

**Analysis in the patch or analysis in the shader?** Both work and they are different tools.

*In the shader*, with an `audioFFT` input: self-contained, travels as one file, and the analysis is invisible to the rest of the patch.

*In the patch*, with *score*'s analysis processes feeding inlets: visible, adjustable without recompiling, shareable between several shaders, and it can be smoothed, scaled, and curved with processes you can see. **For performance work this is usually the better answer**, and it is why [Unit 37]({{ site.baseurl }}/learn/37-parameters-as-inlets.html) came first.

The shader below does it internally so that the technique is readable in one file. In a real patch, split it.

## Build it

{% include shader.html id="40-audio-reactive" height="460" pointer="none" caption="Three bands with separate envelopes in a persistent buffer, an onset signal, and meters. Switch the player's audio source to your microphone and talk, clap, and hum; the three meters should move differently." %}

1. **Watch the three meters** on the test signal. They move at different rates because they have different jobs, not because the signal is different.
2. **Take the noise floor to zero.** Everything is always slightly on, and the picture never rests. Raise it until the visuals stop at silence. That setting is per room.
3. **Take Release to its minimum.** Everything snaps and the picture flickers. Take it to maximum and everything smears. The useful range is narrow and it is a musical decision.
4. **Take Attack to its maximum.** Hits arrive late and the picture feels detached from the sound, which is the most common complaint about audio-reactive work and is almost always this.
5. **Set the three band gains so the meters use their full range** on your material. This is a calibration, it is per source, and it is why they are controls.
6. **Take the three drive amounts to zero one at a time.** Each band does one thing. Notice how much duller the result is when only one is connected.
7. **Watch the onset lamp against the low meter.** They are related and they are not the same signal, which is the point.
8. **Switch to your microphone and clap.** Onset fires, low swells, and the two decay differently.

## In score

Build it the other way, in the patch.

1. **Add an audio source** and an analysis process producing a spectrum or band levels.
2. **Smooth each band separately**, with its own attack and release. Now they are visible and adjustable during a rehearsal.
3. **Address each smoothed value at a shader inlet** whose name says what it does in the picture: `swell`, `warp`, `detail`. Not `bass`, `mid`, `treble`; the shader should not know where its numbers come from, so that the same shader can be driven by a curve during a section with no music.
4. **Put the mapping in the patch.** Scaling, curves, and clamps go between the analysis and the inlet.
5. **Automate the gains.** A quiet section and a loud section need different calibration, and an automation curve on the gain is how a piece stays legible across both.

## Common mistakes

- **One level driving everything.** The pumping problem.
- **Reading a single FFT bin.** Noisy and key-dependent.
- **No noise floor.** The room animates the piece.
- **One smoothing constant** for both directions.
- **Frame-rate-dependent smoothing.** Correct at home, wrong in the venue.
- **Onset derived from a threshold on the envelope.** It fires late and it double-triggers on a sustained note. Use the rise.
- **Naming inlets after the analysis.** `bass` is a source; `swell` is a role.
- **Calibrating on one track.** Check on the quietest and loudest material in the set.
- **No manual override.** Something in the signal chain will fail. A control that lets you drive the parameter by hand is the difference between a wobble and a stop.

## Exercise

Build an audio-reactive patch in *score* where the analysis is entirely in the patch and the shader takes four role-named parameters.

Requirements: three bands with individually adjustable attack and release; a separate onset signal; a noise floor per band; every gain automatable; and a manual override on at least one parameter so it can be driven by hand if the analysis misbehaves.

**Success criterion:** the patch works on both the quietest and the loudest material in a set without editing the shader, and switching the audio source to silence leaves the picture still. Then, harder: hand the controls to someone else and watch which one they reach for first. Whatever it is should have been the easiest one to find.

## Going further

- [Audio-reactive visuals in *score*]({{ site.docs_baseurl }}/examples/video/audioreactive.html), which makes the same points about smoothing and multiple bands.
- [Analysis processes]({{ site.docs_baseurl }}/processes/analysis.html), for RMS, FFT, pitch, and onset in the patch.
- [Milestone P4]({{ site.baseurl }}/learn/p4-visual-instrument.html), next.
