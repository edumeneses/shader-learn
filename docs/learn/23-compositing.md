---
layout: default
title: "Unit 23: Blend modes, alpha, and compositing several layers"
description: "Blend modes are two-line formulas, not menu items. Alpha is the part that goes wrong, and it goes wrong only at soft edges, which is why nobody notices until they do."
parent: Units
nav_order: 25
unit: "23"
permalink: /learn/23-compositing.html
reading_time: "13 min"
practice_time: "25 min"
glsl: "GLSL ES 3.00"
---

# Unit 23: Blend modes, alpha, and compositing several layers

{% include unit_meta.html %}

> **Before this unit** read [Unit 20]({{ site.baseurl }}/learn/20-sampling.html) and [Unit 04]({{ site.baseurl }}/learn/04-colour.html).
>
> **You will need** the player below.
>
> **You will build** the blend formulas as arithmetic, and a clear view of the alpha convention that bites.

## Why this matters

[Unit 00]({{ site.baseurl }}/learn/00-what-a-shader-is.html) said a shader has no layers and no draw order: everything is one function, and stacking is arithmetic you write. This unit is that arithmetic, done deliberately.

It matters for two reasons beyond tidiness. First, blend modes are how a piece gets depth: a scene composited from three layers with different modes reads as having space in it in a way one layer does not. Second, in *ossia score* a whole chain of processes is a compositing graph, and knowing what each mode does at the formula level is what lets you predict a patch rather than experiment with it.

The part that actually goes wrong is alpha. Straight and premultiplied alpha agree wherever a pixel is fully transparent or fully opaque, and differ everywhere in between, so a mistake shows up only along soft edges: a dark fringe around a glow, a halo around a title, a blurred layer with a grey border. Those are one convention mismatch and they are very hard to find by staring.

## The idea

**A blend mode is a function of two colours.** Backdrop `b`, source `s`, both per channel:

| Mode | Formula |
|:-----|:--------|
| normal | `s` |
| add | `b + s` |
| multiply | `b * s` |
| screen | `b + s - b*s` |
| overlay | multiply or screen, chosen by `b` |
| soft light | a smooth version of overlay |
| difference | `abs(b - s)` |
| colour dodge | `b / (1 - s)` |
| linear burn | `b + s - 1` |

Two operations each. These are the definitions from the PDF imaging model, which CSS and every compositing application inherited, so learning them here means knowing what a menu item in any other tool will do.

**Add and screen both lighten, differently.** Add is what happens when two lights hit the same surface, and it clips as soon as the sum passes 1. Screen is the complement of multiplying the complements, so it approaches 1 without ever reaching it. Add for light sources; screen for a lightening that has to stay controlled.

**Overlay is multiply and screen with a switch.** Where the backdrop is dark it multiplies and where it is light it screens, so it increases contrast while keeping both ends. Soft light is the same intent with a smooth transition instead of a hard one at 0.5, which is why it looks gentler.

**Straight alpha and premultiplied alpha.** In straight alpha, the colour is the layer's colour and alpha says how much of it to use: `mix(b, s, a)`. In premultiplied alpha, the colour has already been scaled by alpha, so compositing is `s + b * (1 - a)`.

Premultiplied is the better convention and it is what a compositor uses internally, for a specific reason: it survives filtering. Blur a straight-alpha image and the colour of a fully transparent pixel, which is arbitrary, bleeds into its neighbours; blur a premultiplied one and nothing bleeds, because a transparent pixel's colour is zero.

**The two agree at 0 and 1 and differ in between**, which is why the symptom is always a fringe.

**In linear light, or not?** Add and screen model light arriving, so they belong in linear light. Multiply, overlay, and soft light were defined on codes and were tuned by eye there, so the physically correct version genuinely looks different from what a designer expects. This course's position: convert for the ones that model light, and treat the rest as stylistic. The player has a switch so you can decide rather than be told.

**Order matters and is not associative.** Screen then multiply is not multiply then screen. A compositing chain is a sequence, and in *ossia score* that sequence is the order of processes in the patch.

## Build it

{% include shader.html id="23-compositing" height="440" pointer="origin" caption="Drag the canvas to move the layer over different regions of the test card. Each blend mode behaves differently over the white bar, the black bar, and the continuous-tone region, and that is the fastest way to learn what each one is for." %}

1. **Start on normal and drag the layer around.** It covers what it is over. Opacity fades it. Nothing surprising, and it is the baseline.
2. **Switch to multiply.** The layer darkens everything and disappears over black, because anything times zero is zero. Multiply can never lighten.
3. **Switch to screen.** The opposite: it lightens and disappears over white. Screen can never darken.
4. **Switch to add** and drag over the bright bars. It clips: the sum passes 1 and stops. Compare with screen over the same region, which approaches white without flattening.
5. **Switch to overlay** and drag from the black bar to the white bar. Watch the mode change under the layer as the backdrop crosses the middle. That switch is the definition.
6. **Compare overlay and soft light** over the continuous-tone region. Soft light has no discontinuity at the midpoint, which is the whole difference.
7. **Turn Blend in linear light off and on** with add selected. The linear version is noticeably brighter in the mid tones, because adding codes underestimates how much light two sources make.
8. **Raise Edge softness, then toggle Premultiplied alpha.** With a hard edge nothing changes. With a soft edge the fringe appears and disappears. This is the bug this unit exists for, and it is invisible until you make the edge soft.
9. **Turn Show the layer's alpha on.** The mask on its own, which is what a compositor spends most of its time looking at.

## Look at these

{% include toy.html id="XdS3Rw" title="Blend modes" by="Shadertoy community" note="All of the separable modes on one page, each labelled." %}
{% include toy.html id="4dcSDl" title="Premultiplied alpha" by="Shadertoy community" note="The fringe, shown with and without, at a soft edge." %}

The [CSS compositing specification](https://www.w3.org/TR/compositing-1/) has the formal definition of every mode above, and is the clearest place to read them.

## Compositing in *ossia score*

A *score* patch is a compositing graph. Each process has texture inlets and a texture outlet, and connecting them builds the chain. The library ships blend processes, so the modes above are available without writing a shader at all, and [Unit 38]({{ site.baseurl }}/learn/38-chaining.html) builds a chain from them.

Writing your own is still worth it in two cases: when you want a mode the library does not have, and when you want the blend and the thing being blended in one process, which saves a full-resolution texture round trip. The second is a real performance argument in a large patch.

## Common mistakes

- **Mixing straight and premultiplied alpha in one chain.** Fringes at every soft edge, and only at soft edges.
- **Blurring a straight-alpha image.** The arbitrary colour of transparent pixels bleeds into the visible ones, giving a dark or coloured halo.
- **Using add where screen was wanted**, and clipping.
- **Forgetting that multiply and screen have dead zones.** Multiply does nothing over black and screen does nothing over white, and a layer that "vanished" is usually this.
- **Compositing in code space and expecting light.** Especially with add.
- **Assuming the order does not matter.** It does, in both directions.
- **Ignoring the input's alpha.** A texture from another process may carry one, and treating it as opaque is wrong exactly where the composite is soft.

## Exercise

Build a three-layer composite: the input image as a backdrop, a soft generated shape in screen mode, and a second shape in multiply, with the whole thing graded by [Unit 22]({{ site.baseurl }}/learn/22-grading.html)'s chain at the end.

Requirements: each layer has its own opacity and its own blend mode selectable from a `long` input; the composite is premultiplied throughout; and a `long` named `order` swaps which of the two layers is composited first.

**Success criterion:** swapping `order` visibly changes the result, and you can say why before you press it. At maximum edge softness there is no fringe on either layer at any blend mode. If a layer disappears entirely at some mode, check whether it is multiply over black or screen over white before assuming it is a bug.

## Going further

- [The CSS compositing and blending specification](https://www.w3.org/TR/compositing-1/), for the formal definitions.
- [Porter and Duff, *Compositing Digital Images*](https://dl.acm.org/doi/10.1145/800031.808606), 1984, which is where premultiplied alpha comes from.
- [*ossia score*'s blend processes]({{ site.docs_baseurl }}/processes/shaders.html), for the versions you do not have to write.
