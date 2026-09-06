---
layout: default
title: "Unit 39: Video, camera, and images into a shader"
description: "Everything in Module F, with a real source. What changes when the input is a moving image someone is standing in front of, and what breaks in a venue."
parent: Units
nav_order: 42
unit: "39"
permalink: /learn/39-video-input.html
score_version: "3.8.2"
reading_time: "13 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 39: Video, camera, and images into a shader

{% include unit_meta.html %}

> **Before this unit** read [Unit 20]({{ site.baseurl }}/learn/20-sampling.html) and [Unit 38]({{ site.baseurl }}/learn/38-chaining.html).
>
> **You will need** *score* {{ page.score_version }}, a video file, and a camera if you have one.
>
> **You will build** a filter chain on a live source, and a list of the things that go wrong when the source is real.

## Why this matters

Module F built filters and tested them on a card that never moved and always had the same dimensions. A real source is none of those things: it has an aspect ratio you did not choose, a frame rate that is not yours, a colour space that may not be what you assumed, and, if it is a camera, a person in front of it who moves.

Everything in Module F still applies unchanged. What this unit adds is the list of things that only show up with a real source, most of which are not shader problems at all, and all of which are better met now than at a get-in.

## The idea

**A source is a process with a texture outlet.** Video file, camera, image, screen capture, another shader: from a filter's point of view they are identical. That is the whole reason the abstraction is worth having.

**Sizes almost never match.** A camera might be 1280 by 720, a video 1920 by 1080, the window 3840 by 2160. `IMG_SIZE` gives the input's dimensions and `RENDERSIZE` gives the output's, and [Unit 20]({{ site.baseurl }}/learn/20-sampling.html)'s fit-or-fill decision is now a real decision rather than an exercise. Make it deliberately, per source, and expose it as a parameter if the source might change.

**Frame rates do not match either.** A 24 frame-per-second video into a 60 frame-per-second render means each video frame is used two or three times. Anything that assumes a new frame every render frame, and a naive frame-difference motion detector is the classic case, sees zero motion on the repeats. Work from time, not from frames.

**Video is usually not sRGB the way you think.** Broadcast video carries a limited range, roughly 16 to 235 out of 255, and a transfer function that is not sRGB's. A grade that assumes full-range sRGB will crush the blacks and clip the whites of a file that is perfectly correct. If a source looks contrastier than it should, this is usually why.

**A camera has latency and it accumulates.** The capture, the driver, *score*'s graph, and the projector each add some. For a piece where someone reacts to their own image, total latency is the thing that decides whether it feels alive, and it is measured rather than estimated: point the camera at the screen showing its own output and count.

**Cameras auto-adjust, and it will ruin a cue.** Auto exposure, auto white balance, and auto focus will all change the picture in the middle of a piece in response to something in the room. Turn them off at the driver, before the show, and write down that you did.

**A device cannot be added during playback.** A camera is a device. So is the window. Everything has to be present before you press play, which is *score*'s one documented exception to live editing and the one most likely to catch you.

**Alpha may or may not be there.** A texture from another process may carry alpha; a camera will not. [Unit 23]({{ site.baseurl }}/learn/23-compositing.html)'s premultiplied-alpha question becomes real as soon as you composite one over another.

## Build it

1. **Load a video file** into an interval and address its output at the window. Confirm it plays.
2. **Insert one of your Module F filters** between the video and the window. Everything works, because the filter never cared where its texture came from.
3. **Set the mapping deliberately.** Fit or fill, from [Unit 20]({{ site.baseurl }}/learn/20-sampling.html). Look at the edges: a letterbox and a crop are different decisions and the wrong one is very visible on a projector.
4. **Swap the video for a camera.** Different size, different frame rate, and the filter is unchanged.
5. **Measure the latency.** Point the camera at the output. Wave. Count how far behind your hand the image is. Write the number down; it is a property of the room and the machine, not of the patch.
6. **Turn off the camera's automatic adjustments** and confirm the picture stops drifting when someone walks past a window.
7. **Build a small chain on the live source**: a grade, a blur, a composite over a generator. This is a working VJ patch and it is four processes.
8. **Add a still image as a second input** and composite. Three source types, one chain.

## Motion, difference, and the thing everyone tries first

The obvious way to detect motion is to subtract the previous frame from this one. It works, and it has three problems worth knowing before you spend an evening on it.

**Repeated frames read as no motion**, because of the frame-rate mismatch above. The fix is to difference against a *time-decayed* running average rather than against the last frame, which also smooths the result.

**Noise reads as motion.** A camera in a dim room has a lot of sensor noise, and a raw difference is mostly that. Blur before differencing, from [Unit 21]({{ site.baseurl }}/learn/21-convolution.html), and threshold with a `smoothstep` rather than a `step`.

**Auto exposure reads as enormous motion.** The whole frame changes at once. This is the practical reason to turn it off.

A running average in a persistent buffer, from [Unit 18]({{ site.baseurl }}/learn/18-feedback.html), plus a blur and a soft threshold, is about fifteen lines and is a genuinely usable presence detector.

## What to check before a show

A short list, learned expensively by other people, worth running through in the
room rather than at home.

**Every device exists before playback starts.** Camera, window, MIDI, OSC. This
is the documented exception to live editing and it is the most common way a
patch that worked at home fails in a venue.

**The camera's automatic adjustments are off** and stay off after a replug.
Several drivers reset them.

**The output resolution is the projector's**, not your monitor's. A patch that
holds frame rate at 1920 by 1080 may not at 3840 by 2160, and the projector is
frequently the larger of the two.

**The source is the one you will use.** A patch tested on a file and shown on a
camera differs in size, frame rate, colour range, and latency.

**There is a way back.** A bypass on every link, from [Unit 38]({{ site.baseurl }}/learn/38-chaining.html),
and a known-good document saved separately.

## Common mistakes

- **`RENDERSIZE` where `IMG_SIZE` was meant.** Equal in development, different in a venue.
- **No fit-or-fill decision**, so the source is stretched.
- **Assuming a new frame every render frame.**
- **Grading a limited-range video as though it were full-range sRGB.**
- **Leaving the camera on automatic.**
- **Adding a camera or a window during playback.**
- **Testing on the file and showing on the camera.** They differ in every property this unit lists.
- **Not measuring latency** until someone complains that it feels wrong.

## Exercise

Build a patch that composites a live camera over a generated background, where the camera's brightness drives one parameter of the generator.

Requirements: the mapping is a deliberate fit or fill; a blur and a soft threshold sit between the camera and the analysis; the parameter is smoothed; and the whole thing holds frame rate at your output resolution.

**Success criterion:** waving at the camera visibly changes the background, walking away lets it settle rather than stopping abruptly, and turning the room light on and off does not cause a jump larger than a person moving does. If it does, the analysis is measuring exposure rather than motion.

## Going further

- [Video examples in *score*]({{ site.docs_baseurl }}/examples/video/video-examples.html).
- [Camera input]({{ site.docs_baseurl }}/examples/video/camera.html).
- [Computer vision utilities]({{ site.docs_baseurl }}/processes/computer-vision-utilities.html), for the analysis *score* already has.
