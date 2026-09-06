---
layout: default
title: "Unit 27: Materials, fog, occlusion, and knowing when to stop"
description: "Ambient occlusion from five samples, fog that puts a scene somewhere, per-object materials from one march, and one bounce of reflection that doubles the cost."
parent: Units
nav_order: 29
unit: "27"
permalink: /learn/27-materials-and-fog.html
reading_time: "14 min"
practice_time: "30 min"
glsl: "GLSL ES 3.00"
---

# Unit 27: Materials, fog, occlusion, and knowing when to stop

{% include unit_meta.html %}

> **Before this unit** read [Unit 26]({{ site.baseurl }}/learn/26-lighting.html).
>
> **You will need** the player below.
>
> **You will build** the four things that separate a lit render from a picture, and a clear view of what each one costs.

## Why this matters

[Unit 26]({{ site.baseurl }}/learn/26-lighting.html) produced a lit object. This unit produces a place. The difference is made by four things, and only one of them is expensive.

It is also the unit where the phrase "knowing when to stop" earns its place in the title. Every technique here can be extended: one bounce of reflection can be two, five occlusion samples can be sixteen, fog can be volumetric and marched. Each extension roughly doubles a cost that is already the largest in the shader, and almost none of them doubles how good the picture looks. Recognising the point of diminishing returns is a skill, and this is the unit to practise it in.

## The idea

**Ambient occlusion, from the field, in five samples.** Step a short way along the normal and ask how far the nearest surface is. In the open, the answer equals the distance stepped. In a crevice, something is closer, and the shortfall is how occluded the point is:

```glsl
float occ = 0.0, scale = 1.0;
for (int i = 0; i < 5; i++) {
    float h = 0.02 + 0.14 * float(i);
    occ += (h - map(p + n * h)) * scale;
    scale *= 0.72;
}
return clamp(1.0 - occ, 0.0, 1.0);
```

Five evaluations, no rays, no noise, no accumulation over frames. It is the single cheapest thing you can do to make a raymarched image look solid, and it is Quilez's.

**Occlusion belongs on ambient light, not on everything.** Ambient light arrives from all directions, so blocking directions reduces it. The key light already has a shadow. Multiplying the whole result by occlusion is the common mistake and it makes crevices black rather than merely darker.

**Fog puts the scene somewhere.** `mix(colour, fogColour, 1 - exp(-density * t * t))`, with `t` the distance marched. Squaring `t` gives a steeper falloff that reads as haze rather than as a fade. Height fog, where the density falls off with altitude, is one extra `exp` and turns a fade into weather.

Fog is also the cheapest depth cue in existence and it does something else useful: it hides the far plane, so a scene can simply stop without a visible edge.

**Materials come from the id carried out of the march.** [Unit 25]({{ site.baseurl }}/learn/25-fields-3d.html)'s `vec2` convention pays off here: one march, one hit, one id, and a lookup for colour, roughness, and reflectivity. No second pass, no sorting.

**Reflection is another march, and it costs like one.** Reflect the ray about the normal and march again. It is the same loop, the same scene function, and the same price, so a scene with one bounce of reflection is roughly twice the cost of one without. Weight it by a Fresnel term, `pow(1 - dot(-rd, n), 5)`, so that surfaces reflect strongly at grazing angles and weakly head-on, which is what real surfaces do and is most of why a reflection reads as correct.

**Tone-map at the end.** A lit scene with a specular produces values above 1. Without a tone curve from [Unit 22]({{ site.baseurl }}/learn/22-grading.html), every highlight clips to a flat white disc, which is the most recognisable tell of an untreated render.

## Build it

{% include shader.html id="27-materials" height="460" pointer="orbit" caption="Drag the canvas to orbit. Take each control to zero and back: occlusion, fog, and reflection each do a specific job, and the fastest way to learn what it is, is to remove it." %}

1. **Take Occlusion to zero.** The objects lift off the floor: the contact between the sphere and the ground stops reading as contact. Bring it back and they sit down again. Five samples.
2. **Switch to occlusion only.** The white regions are open, the dark ones are enclosed. Note the darkening in the corner where the post meets the floor, and under the sphere, which is where a viewer's eye reads weight.
3. **Take Fog to zero.** The scene becomes an object on a plane rather than a place, and the far ground plane now has a visible hard edge where the march gives up.
4. **Raise Fog settles.** The fog sinks: the same density, distributed by height. This is the difference between a fade and weather, and it is one `exp`.
5. **Switch to material id.** Four flat colours, one per object. That is what the march returned, and everything else was computed from it.
6. **Take Reflection to zero and back**, and watch the frame rate in the player's clock. That difference is a second march.
7. **Raise Roughness with reflection on.** The reflection weakens and the specular broadens together, because both are describing the same physical property.
8. **Switch to steps taken.** Compare it with the same view in [Unit 25]({{ site.baseurl }}/learn/25-fields-3d.html): the reflective floor is now among the most expensive regions in the frame, because every pixel of it pays for a second march.

## Look at these

{% include toy.html id="Xds3zN" title="Raymarching primitives" by="Inigo Quilez" note="Occlusion, fog, materials, and a full lighting model in one readable file. The reference for this unit." %}
{% include toy.html id="4sfGzS" title="Ambient occlusion comparison" by="Inigo Quilez" note="The five-sample estimator against a ground truth, so you can see how good an approximation it is." %}
{% include toy.html id="ld3Gz2" title="Snail" by="Inigo Quilez" note="Every technique in Module G, at the limit, with the cost to match." %}

## Knowing when to stop

A working method, in order of how much each step improves the picture per unit of cost:

1. **Tone-map.** Free, and it fixes clipped highlights.
2. **Ambient occlusion.** Five samples, and it is the largest single improvement available.
3. **Sky-coloured ambient** instead of a constant. One function call.
4. **Fog, with height.** Two `exp` calls, and it is the cheapest depth cue there is.
5. **Soft shadows.** One extra march, and a large improvement.
6. **One bounce of reflection.** One extra march, and a modest improvement on most scenes.
7. **A second bounce.** Another march, and almost nobody will notice.
8. **Multi-sample antialiasing.** Multiplies everything above by the sample count.

Items 1 to 4 cost almost nothing and account for most of the difference between a render and a picture. Items 6 onward are where budgets go to die. If a scene is too slow, the first question is never "which of these do I remove"; it is "how many steps is the march taking", and [Unit 34]({{ site.baseurl }}/learn/34-cost.html) is about answering it.

## Common mistakes

- **Occlusion multiplied into everything**, giving black crevices.
- **Fog applied before shading** rather than after. Fog is light scattered on the way to the eye, not a property of the surface.
- **A reflection ray starting on the surface**, which immediately hits it. Same bias as a shadow ray.
- **Reflection without Fresnel**, so a floor is equally mirrored head-on and at a grazing angle, which reads as wet plastic.
- **No tone curve**, giving flat white highlights.
- **Adding a second bounce before measuring the first.**
- **Materials as a chain of `if` statements evaluated at every march step** rather than once at the hit. The march does not need to know what colour anything is.

## Exercise

Add a transparent material to this unit's scene: an object that refracts what is behind it.

Requirements: on hitting it, continue the march *inside* the object with the ray bent by `refract`, exit at the far side, bend again, and continue into the scene; a `float` named `ior` controls the index of refraction; and the object must still be lit and reflective at its surface.

**Success criterion:** at an index of 1.0 the object is invisible except for its specular and reflection. Above 1.0 the background behind it distorts, and the distortion increases with the index. If the object renders black, the interior march is hitting its own surface immediately, which is the bias problem again in a new place.

## Going further

- [Inigo Quilez, free ambient occlusion](https://iquilezles.org/articles/), the five-sample estimator.
- [Inigo Quilez, fog](https://iquilezles.org/articles/fog/), including the scattering-aware version.
- [Milestone P3]({{ site.baseurl }}/learn/p3-raymarched-scene.html), next, which asks you to build one of these to a budget.
