---
layout: default
title: "Unit 36: Loading, editing, and live-coding a shader in score"
description: "The editing loop that makes score worth using: change a line, press two keys, and the running score picks it up without stopping. Also how to get the code back out."
parent: Units
nav_order: 39
unit: "36"
permalink: /learn/36-live-coding.html
score_version: "3.8.2"
reading_time: "13 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 36: Loading, editing, and live-coding a shader in score

{% include unit_meta.html %}

> **Before this unit** read [Unit 35]({{ site.baseurl }}/learn/35-score-pipeline.html).
>
> **You will need** *score* {{ page.score_version }} and a shader you are willing to break.
>
> **You will build** the fastest editing loop in this course, and a habit that stops you losing work in it.

## Why this matters

Every player on this site recompiles when you reload the page. *score* recompiles **while the score is playing**, without stopping it, in about the time it takes to lift your fingers off the keys. That is a different activity from editing a file: you are adjusting something that is running, in front of whatever it is running for.

The reason to take it seriously rather than treat it as a convenience is that it changes what you write. When a change costs a second, you try things. When it costs a rebuild and a restart, you plan. Most of the good decisions in the shaders in this course came from the first mode.

It also has one trap that has cost people real work, and this unit exists partly to name it: **an edited shader lives inside the score document, not in the file you loaded it from**.

## The idea

**Three ways in.** Drag an `.fs` file onto an interval. Drop it into *score*'s user library folder and pick it from the process library. Or add an empty ISF shader process and paste.

**The script editor is a window button on the node header.** *score*'s live-coding page describes it as the small window button, the second one, on each script-based node. The same mechanism serves shaders, JavaScript, Faust, and the rest, so it is worth learning once.

**{% include shortcut.html content="Ctrl+Enter" %} recompiles.** The reference page for the shader process says so plainly: the shader updates automatically, during execution. That is the whole loop.

**Both stages are editable.** The editor exposes the fragment and the vertex shader, which matters for the VSA shaders of [Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html).

**Editing the header rebuilds the inlets.** Add an input to the JSON and a new inlet appears. This is the part worth exploring during a rehearsal rather than during a show, because an inlet that disappears takes its connection with it.

**Almost everything else is editable too.** Processes can be added, removed, and altered while the score plays. The documented exception is devices: **a new device cannot be added during playback**, and a window is a device.

**Keep the score running with a trigger.** A timeline that ends stops. *score*'s own suggestion for live coding is to use triggers to keep the running parts running forever, so an interval behaves like a patch in Max or Pure Data. Do this before you start editing, or you will spend the session pressing play.

**The edited source lives in the document.** This is the trap. Once you have edited a shader in *score*, the version in the `.score` file is the authority, and the `.fs` you dragged in is a stale copy. A live-coded improvement can be trapped in a document you later abandon. The habit that prevents it is boring and reliable: **when you get something you like, copy it back out to the file, immediately**.

## Build it

1. **Set up so the score does not stop.** Make an interval, add a trigger that loops it, and start playback. Nothing below works well against a timeline that keeps ending.
2. **Load a shader** from this course's library. Confirm it appears in the window.
3. **Open the script editor** from the node header's window button.
4. **Change one constant** and press {% include shortcut.html content="Ctrl+Enter" %}. The picture changes without a gap.
5. **Break it on purpose.** Delete a semicolon and recompile. *score* reports the error and keeps rendering the last shader that worked, which is the behaviour you want in a performance: a typo does not black the output.
6. **Add an input to the JSON header** and recompile. A new inlet appears on the process.
7. **Remove an input that has a connection.** The inlet goes and the connection with it. Do this once now so it does not surprise you later.
8. **Copy the source back out to a file.** Now, while you are thinking about it, not later.

## A working method for live coding

**Prepare more than you improvise.** The shaders that survive a live session are the ones with the right parameters already exposed. Live coding is for the thing you did not anticipate; a slider is for the thing you did.

**Change one thing at a time.** A recompile that changes three things and looks wrong gives you no information.

**Keep a known-good version in the file** and a scratch copy in the document.

**Watch the inspector's texture preview, not the output window.** The output is usually on a projector pointing away from you.

**Have a way back.** A second process with the last good version, bypassed, costs nothing and is the difference between a wobble and a stop.

## Getting the code back out

Three routes, in order of preference.

**Copy from the editor.** Select all, copy, paste into your file. Boring and reliable.

**Save the document and read the JSON.** A `.score` file is text, and an edited shader's source is in it. This is how to recover something from a document whose original file you have lost.

**Version the file, not the document.** The `.fs` files are the deliverable and the `.score` documents are the arrangement. Keeping the shaders in a repository and the documents beside them is the arrangement this course uses and recommends.

## What live coding is good for, and what it is not

It is worth being blunt about this, because the practice attracts a certain
amount of romance and the romance produces bad shows.

**Good for:** finding a look during a rehearsal, adjusting to a room, responding
to something in a set that nobody planned, and the whole of development. The
speed of the loop is genuinely transformative when you are searching rather than
executing.

**Bad for:** anything that has to happen on a cue. A recompile takes a moment,
the moment is not deterministic, and a typo at the wrong time is a black frame
in front of an audience. Anything the piece *needs* should be a parameter driven
by an automation curve or a control, which is [Unit 37]({{ site.baseurl }}/learn/37-parameters-as-inlets.html).

The division that works: **live-code the search, automate the performance**. A
shader that arrives at a show with the right dozen parameters exposed does not
need to be edited during it, and that is the goal rather than a failure of
nerve.

## Common mistakes

- **Losing an edit inside a document.** The one that actually happens.
- **Adding a device during playback.** The documented exception.
- **Editing against a timeline that keeps ending.** Use a trigger.
- **Editing the header during a show.** Inlets and connections move.
- **Assuming a compile error stops the output.** It does not, which is good, and it also means a shader can look unchanged because your edit never compiled. Read the error.
- **Live coding what should have been a parameter.** If you find yourself editing the same number repeatedly, it wanted to be an inlet.

## Exercise

Set up a score that loops forever with one shader, and live-code it through four distinct states without ever stopping playback.

Requirements: each state is a visible change; at least one adds a new input to the header and drives it from an automation; at least one is a compile error you recover from; and at the end you have the final source saved to a `.fs` file.

**Success criterion:** playback never stopped, and the saved file reproduces the final state when loaded into a fresh document. If it does not, something you changed lived only in the document, which is exactly the habit this unit is trying to build.

## Going further

- [Live coding in *score*]({{ site.docs_baseurl }}/common-practices/8-live-coding.html).
- [The ISF shader process]({{ site.docs_baseurl }}/processes/shaders.html), on editing both stages.
- [Unit 37]({{ site.baseurl }}/learn/37-parameters-as-inlets.html), next, on the sliders that mean you have to live-code less.
