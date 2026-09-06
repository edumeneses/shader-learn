---
layout: default
title: "Capstone: one finished, documented piece"
description: "The last unit. Not a new technique: one piece, finished to a standard someone else could perform, with everything the course has been insisting on actually done."
parent: Units
nav_order: 46
unit: "42"
permalink: /learn/42-capstone.html
score_version: "3.8.2"
reading_time: "14 min"
practice_time: "180 min"
glsl: "GLSL ES 3.00"
---

# Capstone: one finished, documented piece

{% include unit_meta.html %}

> **Before this** finish the course, or at least Phase 1 and the phase your piece needs.
>
> **You will need** three hours at minimum, and more if the piece deserves it.
>
> **You will build** one finished work, and the documentation that makes it performable by someone who is not you.

## Why this matters

Forty-two units of technique are worth exactly as much as the finished work that comes out of them, which for most people who learn this material is nothing. The gap between "I can do this" and "I made this" is not more technique. It is finishing, and finishing is a separate skill that this unit exists to make you practise once under supervision.

There is a second reason, and it is the one this course has been building towards since [Unit 00]({{ site.baseurl }}/learn/00-what-a-shader-is.html). A shader that lives in your editor is a demonstration. A shader with named parameters, a documented range, a measured frame rate, and a written page is a **work**: something that can be programmed, rehearsed, toured, handed over, and performed by someone who has never met you. Almost nothing in shader art reaches that standard, and the difference is not talent.

## The brief

**One piece. Finished. Documented. Performable by someone else.**

Choose one of three shapes. They are equally valid and they are graded differently.

**A fixed work.** A video piece, a print, or a fixed installation loop. Its deliverable is a rendered file and its criterion is that it survives being looked at repeatedly.

**A performable piece.** A score with a timeline, cues, and a shape you decided, that can be run by pressing play. Its criterion is that it does the same thing twice.

**An instrument.** [Milestone P4]({{ site.baseurl }}/learn/p4-visual-instrument.html) taken to a finished state. Its criterion is that someone else can play it.

Whichever you choose, all of the following hold.

1. **At least four techniques from at least three modules**, chosen because the piece needs them.
2. **Every shader is a real file** under version control, with a complete header: description, credit, categories, and every parameter labelled and ranged.
3. **No parameter is named after a device.**
4. **Everything is measured.** Frame rate at output resolution, on named hardware. Render time if it is a fixed work.
5. **It runs somewhere other than your development machine.**
6. **Written documentation**, below.
7. **Credit for everything borrowed.** Distance functions, palettes, noise, estimators, ported shaders. This course has borrowed from Inigo Quilez on almost every page and said so on almost every page.

## The documentation

Three pages at most, and it is the deliverable most likely to be skipped and most likely to be needed.

**What it is.** A paragraph. What a viewer sees and what it is for.

**How to run it.** The exact version of *score*, the files, the devices that must exist before playback, the output resolution, and the audio setup. Assume the reader has never opened it.

**The controls.** Every parameter someone might move, what it does, and its useful range. Not the full range: the useful one.

**What breaks and what to do.** Audio fails, camera fails, projector is the wrong resolution, machine is slower than expected. One line each. This is the section that gets read at eleven at night.

**The safe state.** What to do to get back to something you are willing to project.

**Credits and licence.** Yours and everyone else's.

## A working method

1. **Choose the piece before choosing the techniques.** A piece assembled from techniques you wanted to use is a demo reel. Decide what it is, then find out what it needs.
2. **Make the ugly version first, end to end.** All the way to the output, at the real resolution, in the first session. Everything you learn after that is refinement; everything you learn from a piece that never reached the output is speculation.
3. **Measure early.** [Unit 34]({{ site.baseurl }}/learn/34-cost.html). A piece you discover is too slow at the end is a piece you rebuild.
4. **Get it out of your editor early too.** On the venue's machine, the actual projector, the real speakers. Everything is different there and none of the differences are interesting.
5. **Cut.** The single most reliable improvement available to any piece at this stage is removing something. If you cannot decide, remove the newest thing.
6. **Write the documentation before you think you are finished.** Writing down what each control does is the fastest way to find out that three of them do nothing useful.
7. **Show it to someone and say nothing.** Where they look and when they look away is worth more than what they say afterwards.
8. **Stop.** A finished piece with one flaw beats an unfinished piece with none, and this is the point at which most people in this field do neither.

## On scope

Three hours is the stated practice time and it is a floor, not a target. The
more useful guidance is about what to attempt.

**Smaller than you think.** Nearly every capstone that fails, fails by being too
large: a piece with four sections when one would have been finished, an
instrument with twenty controls when six would have been played. A single
technique, executed to a standard, is a better outcome than five roughed in.

**Something you already half know how to make.** This is not the place to learn
a technique. Use what you can already do and spend the effort on finishing,
which is the skill being practised.

**Something with a deadline.** An imaginary one works. The piece expands to fill
the time available and the last twenty percent of the polish is invisible to
everyone but you.

**Something you would show someone.** The real test of scope is whether you
would put it in front of a person you respect. If the honest answer is "once I
have added the other thing", the scope is wrong; cut until the answer is yes.

## Success criteria

- **It runs on a machine that is not yours**, from the documentation alone.
- **You measured the frame rate or the render time**, on named hardware, and wrote it down.
- **Every shader is in version control** with a complete header.
- **No parameter is named after a device.**
- **Someone else has run it** without you in the room.
- **The documentation exists** and someone used it.
- **You stopped.**

## Common ways this goes wrong

- **Starting from technique.** The commonest and the most fatal.
- **Never reaching the output.** A piece that has not been on the real device is not finished, however good it looks in a window.
- **Measuring at the end.**
- **Adding rather than cutting.**
- **No documentation**, so the piece is unperformable within a year, including by you.
- **Not crediting.** Almost all of this material stands on published work, most of it given away freely. Say so.
- **Not stopping.** There is no version of this that is finished. There is a version that is good and shipped.

## Afterwards

A few honest things about what comes next.

**Read other people's shaders.** Shadertoy, vertexshaderart.com, the Vidvox library that ships with *score*. Reading is how this field is actually learned, and after this course you can read most of it.

**Publish.** A shader with a header, a licence, and a credit is a contribution. The library in this repository is one, and it is not large.

**Learn what this course skipped.** Compute in depth, WebGPU, mesh shading, physically based rendering, GPU-accelerated video, multi-machine rendering for large installations.

**Make things that are not about shaders.** The most common failure among people who get good at this is that the technique becomes the subject. The shaders that are worth anything are the ones in service of something else.

## Going further

- [Inigo Quilez](https://iquilezles.org/), whose articles this course has cited on nearly every page.
- [The ISF library]({{ site.isf_baseurl }}/), and the Vidvox collection inside *score*.
- [*ossia score*'s documentation]({{ site.docs_baseurl }}), and its [community](https://ossia.io), who will answer questions.
- [The units of this course]({{ site.baseurl }}/learn), which are meant to be returned to rather than read once.
