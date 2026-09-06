---
layout: default
title: "Unit 00: What a shader is, and what it is not"
description: "A fragment shader is a pure function from a coordinate to a colour, run once per pixel with no memory of its neighbours. Everything else in the course follows from that."
parent: Units
nav_order: 0
unit: "00"
permalink: /learn/00-what-a-shader-is.html
reading_time: "12 min"
practice_time: "none"
glsl: "GLSL ES 3.00"
---

# Unit 00: What a shader is, and what it is not

{% include unit_meta.html %}

> **Before this unit** nothing. This is the first page.
>
> **You will need** a browser. Nothing is installed in this unit.
>
> **You will build** nothing yet. You will finish with an accurate model of what the machine is doing, which is what makes the next forty units short instead of long.

## Why this matters

Almost everyone arrives at shaders with a drawing model in their head. You have a canvas, you pick up a brush, you move it, you put down a stroke, and the stroke stays where you left it. Every drawing tool ever made works that way, from a pencil to Illustrator to the HTML canvas API.

A shader does not work that way at all, and the whole difficulty of learning one is that the drawing model keeps almost working. You can produce a circle. You cannot produce it by drawing a circle. If you carry the drawing model forward you will spend a week fighting the language and conclude that shaders are needlessly hard. They are not hard; they are a different question.

The question a drawing tool asks is *where do I put this thing*. The question a shader asks is *given this position, what colour is there*. Those sound similar and they are not. The second question has no memory, no order, and no notion of an object. It is asked separately for every pixel on the screen, and the answers are computed at the same time, by thousands of cores that cannot see each other's work.

Once that lands, the rest of this course is technique.

## The idea

**A fragment shader is a function.** It takes a position and returns a colour. In the dialect this course uses, that function is called `main`, the position arrives in a variable, and the colour is written to another. Between those two lines you can do arithmetic and call maths functions, and that is essentially all.

**It runs once per pixel, in parallel.** At 1920 by 1080 your function runs a little over two million times per frame, sixty times a second. The GPU does not run them in an order you can rely on. Two invocations cannot pass each other a value, cannot take turns, and cannot agree on anything. Every one of them starts from the same inputs and differs only in the coordinate it was handed.

**A shape is therefore an inequality, not a stroke.** To make a circle you do not draw a circle. You ask each pixel how far it is from a point and colour it if that distance is less than a radius. Every pixel on the screen asks the question; most of them answer no. That is not wasteful in the way it sounds, because the cores were going to run anyway, and it is the reason a shader can render something no drawing tool can, such as a shape defined by an equation with no outline to trace.

**Values that are the same for every pixel are called uniforms.** The time, the resolution, a slider the reader is holding: these are set once per frame and read by every invocation. A uniform is how the outside world reaches a shader. Everything the reader can control on this site is a uniform, and everything an automation curve drives in *ossia score* is a uniform.

**Values that differ per pixel arrive from the stage before.** In a full pipeline, a vertex shader runs once per corner of the geometry, and the hardware interpolates its outputs across the surface in between. For the shaders in Phases 1 and 2, the geometry is a rectangle covering the screen, so the only interpolated value that matters is the normalised coordinate of the pixel. Unit 28 opens the rest of the pipeline; Unit 31 writes a vertex shader on purpose.

### What a shader is not

**It is not sequential.** There is no "first draw the background, then draw the circle on top of it". Every pixel decides its own final colour in one pass. Layering exists, but you write it as arithmetic inside the function: compute the background colour, compute the circle colour, and mix between them by a coverage value. Unit 23 makes that a discipline.

**It cannot see its neighbours.** A fragment shader has no access to the colour another pixel arrived at. This is why a blur is not one shader but two passes over a texture, which Unit 21 builds, and why anything that genuinely needs neighbours needs an image to read from rather than a screen to write to.

**It has no memory between frames**, unless you give it one. The function starts fresh sixty times a second. A shader that leaves a trail behind a moving shape does so because a pass wrote into a buffer that persisted, and Unit 18 is that mechanism in full.

**It is not a filter.** A filter takes an image and returns an image. A shader can do that, and Module F is about doing it well, but a shader with no input at all is equally normal; the shader on this page is one. In *ossia score*'s vocabulary that distinction is the difference between a generator and a filter, and Unit 38 chains them.

**It is not free.** Every line you write runs two million times per frame. A `for` loop with forty iterations is eighty million operations. Unit 34 is about reading that cost from the source, and it arrives late deliberately: optimising a shader before you can write one is how people end up with fast, ugly work.

## Build it: read the function

Nothing to install. Here is a shader that uses most of what this course teaches, running on your GPU right now. Drag the canvas. Move the sliders. Take **Softness** to zero and look at the edge; take **Rings** to zero and watch what is left.

{% include shader.html id="00-hello-field" height="380" pointer="focus" caption="Every number in this picture is a named parameter. None of them is called mouse, or time, or level; the pointer drives an input named focus, and the same input takes an OSC message or an automation curve without being renamed." %}

Open the source with the `‹›` button and read it against what you have just read.

1. **Find `void main()`.** That is the function. Everything above it is a declaration or a helper.
2. **Find `isf_FragNormCoord`.** That is the position, running from 0 to 1 across the picture. It is the only thing that differs between the two million invocations.
3. **Find `gl_FragColor`.** That is the answer. One assignment, once, at the end.
4. **Find `float d = length(uv - centre) - radius;`.** That is the circle, and it is one line of arithmetic with no drawing in it. `d` is negative inside the circle, zero on its edge, and positive outside. Unit 07 is about how much you can build from that one idea.
5. **Find `radius`, `softness`, `hue`.** Those are uniforms. The sliders write them; nothing else in the shader knows or cares where they came from.

Now change one thing in your head before you change it on screen: **Drift** is multiplied by `TIME`, and `TIME` is a uniform. Nothing in this shader moves. Each frame is computed from scratch with a slightly larger number, and the movement is entirely in your eye.

## Look at these

Shadertoy is where most published shader art lives. Every one of these is one function of a coordinate, exactly like the one above, and reading them now is worthwhile even though none of it will make sense yet.

{% include toy.html id="Ms2SD1" title="Seascape" by="Alexander Alekseev" note="An ocean, from arithmetic, with no geometry anywhere in it." %}
{% include toy.html id="XsXXDn" title="Creation" by="Danilo Guanabara" note="Twelve lines. Read it after Unit 14 and it will be obvious; read it now and it will not." %}
{% include toy.html id="4ttSWf" title="Rainforest" by="Inigo Quilez" note="The far end of what this course leads towards, including the cost." %}

## Common mistakes

- **Looking for a drawing API.** There is no `moveTo`, no `fillRect`, no `stroke`. If you are searching the reference for one, the model in your head is still the wrong one.
- **Assuming a loop over pixels.** Beginners often write a shader as though it draws the whole image, then wonder why the result is a flat colour. Your function sees one pixel. The loop is the hardware, and you are inside it.
- **Expecting order.** "Draw this behind that" is not available. Compute both, then mix.
- **Treating a uniform as a variable you can write.** A shader cannot assign to a uniform, and it could not usefully do so anyway: two million invocations would be fighting over it.
- **Believing a shader is only a post-process effect.** That is one use. A shader with no input generates; a shader with an input filters; a shader with several inputs composites. All three are the same kind of program.

## Exercise

No code in this unit. Do this instead, on paper or in your head, before you move on.

Describe, as arithmetic on a coordinate, how you would colour the screen so that the **left half is black and the right half is white**. You may not use an `if` that draws anything; you may only compute a number from the coordinate and use it as a colour.

Then extend it: what changes if you want the boundary **soft** over a band ten pixels wide, rather than hard?

**Success criterion:** your answer involves comparing the coordinate to a threshold and producing a number between 0 and 1, and you can say why the soft version needs to know the size of a pixel. If you got there, [Unit 06]({{ site.baseurl }}/learn/06-step-and-smoothstep.html) will feel like a formality rather than a lesson.

## Going further

- [The Book of Shaders](https://thebookofshaders.com/), by Patricio Gonzalez Vivo and Jen Lowe, is the other good introduction and takes a gentler route through the same first ideas.
- [Inigo Quilez's articles](https://iquilezles.org/articles/) are the reference for most of Phase 1 and all of Module G. Nothing else comes close.
- [Interactive Shader Format]({{ site.isf_baseurl }}), the format every shader in this course is written in.
- [*ossia score*'s ISF process]({{ site.docs_baseurl }}/processes/shaders.html), which is where these shaders end up in Phase 4.
