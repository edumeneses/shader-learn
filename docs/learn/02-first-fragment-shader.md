---
layout: default
title: "Unit 02: Your first fragment shader"
description: "Four shaders in increasing order of ambition: a flat colour, the coordinate as colour, one channel, and the coordinate moved by time. Everything after this is a variation on the fourth."
parent: Units
nav_order: 2
unit: "02"
permalink: /learn/02-first-fragment-shader.html
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 02: Your first fragment shader

{% include unit_meta.html %}

> **Before this unit** read [Unit 00]({{ site.baseurl }}/learn/00-what-a-shader-is.html) and [Unit 01]({{ site.baseurl }}/learn/01-where-shaders-run.html).
>
> **You will need** a text editor, and either this page's player or *ossia score*.
>
> **You will build** four shaders, each one line different from the last, ending with a moving image made from a coordinate and a clock.

## Why this matters

The gap between reading about shaders and writing one is smaller than it looks, and it is worth crossing in a single sitting. There are exactly three things to learn: where the coordinate comes in, where the colour goes out, and what a uniform is. Everything else in this course is arithmetic between those two points.

The four shaders below are deliberately trivial. Their value is that each one isolates a single idea, so that when something breaks later you have a working mental image of what the smallest correct shader looks like.

## The idea

**A colour is four numbers.** Red, green, blue, and alpha, each normally between 0 and 1 rather than 0 and 255. `vec4(1.0, 0.0, 0.0, 1.0)` is opaque red. There is no colour type and no palette; a colour is arithmetic, which is why every operation in this course is a multiplication or a mix rather than a call to a colour library.

**A coordinate is two numbers.** In ISF, `isf_FragNormCoord` runs from 0 at the left edge to 1 at the right, and from 0 at the bottom to 1 at the top. Bottom, not top: this is the normalised convention, and it is the reason the course never touches `gl_FragCoord` directly.

**A uniform is a number from outside.** `TIME` is the seconds since the shader started. `RENDERSIZE` is the size of the picture in pixels. Anything you declare in the JSON header becomes one too, and the host builds a control for it. A uniform is the same for every pixel in a frame and changes between frames.

**`vec2`, `vec3`, `vec4` are the workhorses.** They do arithmetic component by component, so `uv * 2.0` doubles both components, and `a + b` on two `vec3`s adds three pairs of numbers. They can be built from parts, `vec3(uv, 0.0)`, and taken apart by name, `uv.x`, `colour.rgb`, `colour.b`. That last habit, called swizzling, extends to reordering: `colour.bgr` is a real expression and it is how you swap channels without an `if`.

**`fract` is your first useful function.** It returns the fractional part of a number, so it turns any rising value into a repeating 0-to-1 ramp. Almost every repeating pattern in this course starts with `fract`, and Unit 10 makes that a technique.

### The shape of an ISF file

```glsl
/*{
  "DESCRIPTION": "...",
  "ISFVSN": "2.0",
  "INPUTS": [
    { "NAME": "rate", "TYPE": "float", "LABEL": "Rate",
      "DEFAULT": 0.2, "MIN": 0.0, "MAX": 2.0 }
  ]
}*/

void main() {
    vec2 uv = isf_FragNormCoord;
    gl_FragColor = vec4(uv, 0.0, 1.0);
}
```

That is the whole format at this stage. The comment at the top is JSON, and the host reads it to build controls; `rate` is then available in the code as a `float`, with no further declaration. The body is one function. Unit 29 fills in the other twenty keys.

## Build it

Run the player below and step the **Stage** control from 1 to 4, reading the source after each step.

{% include shader.html id="02-first-steps" height="340" pointer="none" caption="Four shaders in one file, selected by a long input. Each stage is a few lines different from the one before it, and stage 4 is the first one that moves." %}

1. **Stage 1, a flat colour.** The function ignores the coordinate entirely and answers the same thing everywhere. Change **Flat colour** and confirm nothing else about the picture can change. This is the smallest shader that does anything, and it is worth writing once by hand so you know the shape of the file.

2. **Stage 2, the coordinate as colour.** `colour = vec3(uv, 0.0)`. Red rises to the right, because that is `x`. Green rises upwards, because that is `y`. Blue is zero everywhere, so the bottom left corner is black and the top right is yellow. Nothing here interpolated anything: two million invocations each reported where they were, and the gradient is what that looks like.

   This picture is also the best debugging tool in the course. Whenever a shader in a later unit is wrong and you cannot see why, output the coordinate as colour and check that it is the shape you think it is.

3. **Stage 3, one channel.** `colour = vec3(uv.x)`. Putting the same number in all three channels gives grey, so this is a horizontal ramp from black to white. It makes the point that a colour is three numbers and not a thing: to go from a gradient of hue to a gradient of brightness you changed which numbers you wrote, and nothing else.

4. **Stage 4, the coordinate moved.** `fract(uv.x + TIME * rate)`. Adding a uniform to the coordinate before using it shifts the pattern; `fract` wraps the result so the ramp repeats instead of saturating. Set **Rate** to zero and the picture stops, which is the proof that nothing is moving: each frame is computed from scratch with a slightly larger `TIME`, and the motion is entirely in your eye.

5. **Now break it on purpose.** Take **Gamma** away from 1. The picture gets brighter or darker, and it does so non-linearly. That control is here only so you meet the fact early: the numbers in a shader are not brightnesses, and [Unit 04]({{ site.baseurl }}/learn/04-colour.html) is about the difference.

6. **Write it yourself.** Copy the source, delete everything inside `main`, and rebuild stage 2 from memory. It is three lines. Doing it from memory once is worth more than reading it five times.

## Look at these

{% include toy.html id="Md23DV" title="A simple UV visualiser" by="Shadertoy examples" note="The same stage-2 picture, with the coordinate written the Shadertoy way; compare the two spellings." %}
{% include toy.html id="XsXXDn" title="Creation" by="Danilo Guanabara" note="Twelve lines, nothing but a coordinate and a clock. You now understand its first two lines." %}

## Common mistakes

- **Forgetting the alpha.** `vec4(colour, 1.0)`, not `vec4(colour, 0.0)`. A fully transparent output over a black background looks exactly like a bug in your maths.
- **Writing `0` where a `float` is required.** GLSL ES is strict: `uv * 2` is an error and `uv * 2.0` is not. This catches everyone once, and the compiler message is clearer than it looks once you know to check for it.
- **Expecting `uv` to run 0 to 1 in pixels.** It is normalised. If you want pixels, multiply by `RENDERSIZE`, and be aware you have just made the shader resolution dependent, which [Unit 03]({{ site.baseurl }}/learn/03-coordinates.html) is about.
- **A trailing comma in the JSON header.** The shader then has no inputs at all and the host reports nothing useful. If your controls vanish, check the header before you check the code.
- **Assuming `TIME` starts at zero when you press play.** It does in most hosts, and in *score* it follows the transport. Never rely on its absolute value; use differences and `fract`.

## Exercise

Write a shader, from scratch, in which:

- the picture is a vertical gradient from a colour you choose at the bottom to a second colour you choose at the top;
- both colours are `color` inputs in the JSON header, so a reader can change them;
- a `float` input named `tilt` rotates the direction of the gradient without you writing a rotation matrix. One line of arithmetic mixing `uv.x` and `uv.y` will do it.

**Success criterion:** the two colours appear as colour swatches in the host's control panel, `tilt` at 0 gives a purely vertical gradient, and `tilt` at 1 gives a purely horizontal one. If the gradient looks banded rather than smooth, that is not a bug, and [Unit 04]({{ site.baseurl }}/learn/04-colour.html) explains it.

## Going further

- [The Book of Shaders, chapter 3](https://thebookofshaders.com/03/), on colour as arithmetic.
- [GLSL ES 3.00 built-in functions](https://registry.khronos.org/OpenGL-Refpages/es3.0/), the reference you will use most in Phase 1.
- [ISF input types]({{ site.isf_baseurl }}/), for the `TYPE` values available in the header.
