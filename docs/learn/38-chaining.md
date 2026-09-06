---
layout: default
title: "Unit 38: Chaining generators, filters, and mixers"
description: "A patch is a graph of textures. Order matters, resolution matters, and the biggest cost in a long chain is not arithmetic but the trips through memory."
parent: Units
nav_order: 41
unit: "38"
permalink: /learn/38-chaining.html
score_version: "3.8.2"
reading_time: "13 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 38: Chaining generators, filters, and mixers

{% include unit_meta.html %}

> **Before this unit** read [Unit 23]({{ site.baseurl }}/learn/23-compositing.html) and [Unit 35]({{ site.baseurl }}/learn/35-score-pipeline.html).
>
> **You will need** *score* {{ page.score_version }} and three or four shaders from this course.
>
> **You will build** a chain, and an understanding of why a long one gets expensive for reasons that have nothing to do with the shaders in it.

## Why this matters

One shader is a picture. A patch is several, connected, and that is where the work starts looking like a piece rather than a demo. It is also where a set of individually cheap shaders becomes a patch that will not hold frame rate, for a reason that is invisible if you only ever think about arithmetic.

The chain is the unit of composition in *score*, and it is what makes the library worth having: a generator you wrote, through a filter someone else wrote, into a mixer that ships with the application, is a real working method and it is faster than writing one shader that does all three.

## The idea

**Three shapes, decided by the header.** A **generator** declares no `image` input and sits at the head. A **filter** declares one and sits in the middle. A **mixer** declares several and sits where chains meet. Nothing else about a shader decides this.

**A chain is a sequence of render targets.** Each process renders into a texture, and the next reads it. That is what makes the chain work, and it is also the cost: every link is a full-resolution write and a full-resolution read.

**That memory traffic is usually the real cost of a long chain.** Six filters at 1920 by 1080 is six writes and six reads of eight megabytes each per frame, which is a hundred megabytes a frame, six gigabytes a second, before any shader has done any arithmetic. A GPU has the bandwidth, and it is not free, and it is the thing people do not count.

**Which is the argument for fusing.** Two filters that always run together can be one shader with both operations inside it, saving a round trip. The trade is flexibility: a fused shader cannot be reordered or bypassed in a performance. **Fuse what is settled, keep separate what you might want to change.**

**Order matters, and not only aesthetically.** A blur then a threshold is not a threshold then a blur. A grade before a composite grades one layer; after it, the whole picture. [Unit 22]({{ site.baseurl }}/learn/22-grading.html)'s rule, that grading belongs at the end, is a statement about chain order.

**Resolution can change along a chain.** A blur can run at half and cost a quarter. In *score* this is why [Unit 20]({{ site.baseurl }}/learn/20-sampling.html) insisted on `IMG_SIZE` rather than `RENDERSIZE`: in a chain they are genuinely different, and a shader that assumes otherwise breaks the moment someone puts a downsample in front of it.

**The eight-channel video mixer ships with score.** It is in the user library under Visuals, ISF Shader, Utility, with opacity and blend mode per input. It is itself an ISF shader, which is worth opening: everything in [Unit 23]({{ site.baseurl }}/learn/23-compositing.html) is inside it, written by someone else, and reading it is a good hour.

**A four-point video mapping object is there too**, for getting output onto a surface that is not a rectangle. It is the last link in most installation chains.

**Bypass is a performance feature.** A process you can switch out of the chain is a process you can recover from. Building a chain where each link can be bypassed independently costs nothing at design time and is worth a great deal at eleven at night in a venue.

## Build it

1. **Start with a generator.** Any Module D shader. Output to the window, confirm a picture.
2. **Insert a filter.** A Module F shader with one `image` input. Connect the generator's outlet to its inlet and its outlet to the window. The chain is now two links.
3. **Insert a second filter and swap their order.** Blur then grade, then grade then blur. The difference is usually obvious and occasionally enormous.
4. **Add a second generator and a mixer.** Two chains meeting. Use the library's video mixer, or [Unit 23]({{ site.baseurl }}/learn/23-compositing.html)'s shader with two inputs.
5. **Now measure.** Note the frame rate with one link, then with six. If it falls faster than the shaders' individual costs explain, you have found the memory traffic.
6. **Put a downsample before the most expensive filter** and see what it buys and what it costs.
7. **Fuse two filters** that you have stopped wanting to reorder, into one shader with both operations. Measure again.
8. **Make every link bypassable** and practise switching one out while playing.

## Feedback at the patch level

[Unit 18]({{ site.baseurl }}/learn/18-feedback.html) built feedback inside one shader with a persistent buffer. A patch can do it too: route a chain's output back into an earlier process's input.

The two feel different and both are worth having. **Inside a shader** the transform between iterations is code: precise, self-contained, and portable, which is why the course's feedback shader travels as one file. **At the patch level** the transform between iterations is *a whole other process*, which can be anything the library offers and can be changed during a performance. A feedback loop whose transform is a shader you can swap live is a genuinely different instrument.

The practical caveat is the same in both cases: a feedback loop needs a float buffer or it sticks, and it needs a decay under 1 or it saturates.

## Reading someone else's chain

A large part of learning this is reading patches other people built, and there
are three questions that make an unfamiliar one legible quickly.

**Where does the texture start?** Find the processes with no texture inlet.
Those are the generators, and everything else is downstream of one of them.

**Where does it end?** Find what is addressed at the window device. Work
backwards from there and the chain assembles itself.

**What is not in the chain?** A process that is present, has no path to the
window, and is not bypassed is either a mistake or a spare. Both are worth
asking about, and in a patch built for performance the spares are usually the
most interesting part: they are what the person expected to need.

## Common mistakes

- **A chain longer than it needs to be.** Every link is a round trip.
- **Fusing too early.** A fused shader cannot be reordered, and reordering is most of how a patch gets designed.
- **Assuming `RENDERSIZE` is the input size.** In a chain it is not.
- **Grading in the middle.** It belongs at the end.
- **No bypass anywhere.** One broken link and there is nothing to do.
- **Building the chain before any shader is finished.** Get each one right alone; a wrong shader inside a chain is much harder to diagnose.
- **Forgetting that a mixer's inputs may be different sizes.** They frequently are.

## Exercise

Build a four-link chain: two generators, a mixer, and two filters, ending at the window, with every link independently bypassable.

Requirements: at least one filter runs at reduced resolution; the grade is last; the chain holds 60 frames per second at your output resolution; and you can reorder the two filters during playback without stopping.

**Success criterion:** bypassing any single link leaves a picture that still makes sense, and swapping the filter order visibly changes the result. Write down the frame rate with all links active and with the chain reduced to one; if the difference is larger than the shaders' individual costs, you have measured the round trips, which is the point of the exercise.

## Going further

- [Video mixing and mapping in *score*]({{ site.docs_baseurl }}/common-practices/11-video-mixing-and-mapping.html), including the eight-channel mixer.
- [The graphics pipeline]({{ site.docs_baseurl }}/in-depth/video.html), on the render graph.
- [Unit 34]({{ site.baseurl }}/learn/34-cost.html), for measuring rather than guessing at all of this.
