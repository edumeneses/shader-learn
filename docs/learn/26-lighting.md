---
layout: default
title: "Unit 26: Normals, lighting, and shadows from a field"
description: "The gradient of the field is the surface normal, and a second march towards the light is a shadow. Both come out of the distance function you already wrote, and the soft-shadow estimate is free."
parent: Units
nav_order: 28
unit: "26"
permalink: /learn/26-lighting.html
reading_time: "14 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 26: Normals, lighting, and shadows from a field

{% include unit_meta.html %}

> **Before this unit** read [Unit 24]({{ site.baseurl }}/learn/24-raymarching.html) and [Unit 25]({{ site.baseurl }}/learn/25-fields-3d.html).
>
> **You will need** the player below.
>
> **You will build** every lighting term separately, so that when a render looks wrong you can find out which one is wrong.

## Why this matters

A raymarcher that returns flat colour on hit gives you a silhouette. Everything that makes an image read as three-dimensional, form, depth, weight, material, comes from the lighting, and all of it is derived from the same distance function that produced the shape.

That is the pleasing part of this unit: nothing new has to be authored. There are no normals stored anywhere, no shadow maps, no light rigs. The normal is the gradient of the field. The shadow is the same march pointed at the light. Both fall out of `map`.

The other reason to build the terms separately is diagnostic. A render that looks wrong is almost always one term: a noisy normal, a shadow with the wrong bias, a specular that has swamped everything. Looking at them one at a time turns an hour of guessing into ten seconds.

## The idea

**The normal is the gradient.** The distance field increases fastest in the direction away from the surface, so its gradient is the surface normal. Central differences:

```glsl
vec2 e = vec2(EPS, 0.0);
vec3 n = normalize(vec3(
    map(p + e.xyy) - map(p - e.xyy),
    map(p + e.yxy) - map(p - e.yxy),
    map(p + e.yyx) - map(p - e.yyx)));
```

**Six extra evaluations of the whole scene.** That is the price, and it is why the normal is usually the second most expensive thing in a raymarcher. There is a four-tap tetrahedral variant that costs two thirds as much and is very slightly noisier, and it is what production shaders use.

**The epsilon is a real choice.** Too small and floating-point noise dominates, giving a normal that sparkles. Too large and the surface is smoothed, losing fine detail and rounding corners. The right value depends on the scale of the scene and on how far away the point is; scaling it with distance is the usual refinement.

**Diffuse is Lambert.** `max(dot(n, L), 0.0)`, how much the surface faces the light. This is the term that gives an object its form, and on its own it already reads as three-dimensional.

**Specular is Blinn-Phong.** `pow(max(dot(n, h), 0.0), shininess)` with `h = normalize(L - rd)`, the halfway vector. Cheaper than the reflection-vector version and better behaved at grazing angles. This is the term that says what the surface is made of: a tight bright highlight reads as hard, a broad dim one as soft.

**A shadow is a second march.** From the surface point towards the light. If anything is hit before the light is reached, the point is in shadow. It is the same loop, so it is the same cost again, and it is the main reason a scene with shadows is roughly twice the price of one without.

**Soft shadows are free.** This is the best trick in the module. While marching towards the light, the ratio of the closest approach to the distance travelled is a measure of how narrowly the ray missed an occluder, which is an estimate of the penumbra:

```glsl
res = min(res, k * d / t);
```

No extra samples, no light sampling, no noise. It is an approximation with no physical justification and it looks convincing, and it is Quilez's.

**Bias, or the surface shadows itself.** A shadow ray starting exactly on the surface immediately reports a distance of zero and the point shadows itself, giving a dark, noisy render. Start it slightly along the normal. Too little and the acne returns; too much and contact shadows detach from the objects casting them.

## Build it

{% include shader.html id="26-lighting" height="460" pointer="sun" caption="Drag the canvas to move the light. Step through the Show control to see each term on its own, which is how you debug a render that looks wrong." %}

1. **Start on flat, no lighting.** A silhouette. This is what [Unit 24]({{ site.baseurl }}/learn/24-raymarching.html) produced and it is worth seeing again to appreciate what the rest of the unit adds.
2. **Switch to normals as colour.** Red, green, and blue are the three components. Every surface facing the same way is the same colour, and that is what makes this the most useful debugging view in three dimensions.
3. **Raise Normal epsilon to about 0.05, still on the normals view.** Corners round off and detail smooths away. Now take it to its minimum and look for sparkle. Both failure modes, on one slider.
4. **Switch to diffuse only.** Form appears. Drag the light and watch the terminator, the line between lit and unlit, move across the surface.
5. **Switch to specular only.** A single bright region. Raise Shininess and it tightens; lower it and it spreads over the whole object. That control is the material.
6. **Switch to shadow only, with Shadows on hard.** Black and white, with a hard edge. Now switch to soft and watch the edge gain a penumbra that widens with distance from the caster, which is what a real shadow does.
7. **Raise Shadow softness.** The penumbra narrows, because the constant is a scale on how quickly closeness turns into darkness. It is a look, not a physical quantity.
8. **Switch to everything.** Ambient, diffuse, shadow, specular, fog. Take Ambient to zero and note that unlit regions become pure black, which is what a scene with one light and no bounce actually looks like and is almost never what you want.

## Look at these

{% include toy.html id="lsKcDD" title="Soft shadows in raymarched scenes" by="Inigo Quilez" note="The estimator this unit uses, with the improved version that fixes its banding." %}
{% include toy.html id="Xds3zN" title="Raymarching primitives" by="Inigo Quilez" note="A complete lighting model: key, fill, bounce, specular, occlusion, and fog." %}
{% include toy.html id="4tByz3" title="Normal estimation" by="Inigo Quilez" note="The four-tap tetrahedral normal, which is a third cheaper than six taps." %}

## Ambient, and the difference between plastic and solid

The term this unit leaves weakest is ambient, and it is worth being explicit about why, because it is the single most common reason a raymarched image looks like a rendering rather than like a place.

A flat ambient constant says the same amount of light arrives at every point from every direction. Nothing in the world does that. Two cheap improvements change the picture completely, and neither costs a march:

**Sample the sky in the normal's direction.** Instead of `ambient * base`, use `skyColour(n) * base`. An upward-facing surface picks up the sky and a downward-facing one picks up the ground, which is what actually happens outdoors, and it costs one function call.

**Add a bounce light.** One extra diffuse term, pointing roughly opposite the key light, dim and tinted towards whatever the ground is. It stands in for light that hit the floor and came back up, and without it every shadowed side of every object is dead.

The third improvement, ambient occlusion, needs the field again and is [Unit 27]({{ site.baseurl }}/learn/27-materials-and-fog.html).

## Common mistakes

- **No shadow bias**, giving self-shadowing acne that looks like a broken normal.
- **Too much bias**, so contact shadows float away from their objects.
- **An epsilon tuned at one scale.** A scene that zooms needs a distance-dependent epsilon.
- **Forgetting to normalise the normal.** Central differences give a vector proportional to the gradient, not a unit vector.
- **Adding specular to an unlit surface.** Multiply it by the shadow term or objects glint in their own shadows.
- **Lighting in code space.** [Unit 04]({{ site.baseurl }}/learn/04-colour.html) applies with full force here: lighting is addition of light, and adding codes is wrong. This unit's shader does not convert, which is the common shortcut, and [Unit 27]({{ site.baseurl }}/learn/27-materials-and-fog.html) is where it starts to matter.
- **Ambient as a flat constant.** It works and it is why so many raymarched scenes look plastic. Sampling the sky in the normal's direction costs nothing more and looks far better.

## Exercise

Add a second light of a different colour to this unit's setup, with its own direction, intensity, and shadow, and expose all three as inputs.

Then add a `bool` named `showTerms` that renders the scene as a false-colour breakdown: red for the first light's contribution, green for the second's, blue for ambient.

**Success criterion:** with one light at full and the other at zero the render matches the single-light version exactly. In the breakdown view, no pixel is lit by a light whose shadow says it is occluded. If a surface shows a highlight inside a shadow, the specular term is missing its shadow multiplier.

## Going further

- [Inigo Quilez, soft shadows](https://iquilezles.org/articles/rmshadows/), including the banding fix.
- [Inigo Quilez, normals for an SDF](https://iquilezles.org/articles/normalsSDF/), for the tetrahedral variant.
- [Unit 27]({{ site.baseurl }}/learn/27-materials-and-fog.html), next, which adds occlusion, fog, and materials.
