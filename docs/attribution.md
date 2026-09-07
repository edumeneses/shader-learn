---
layout: default
title: Attribution
nav_order: 3
permalink: /attribution
---

# Attribution

{: .no_toc }

This course stands on published work, most of it given away freely by people who
did not have to. This page says whose, and what was taken.

1. TOC
{:toc}

## What is and is not original here

**Every shader in this course was written for it.** None is copied from
Shadertoy, from vertexshaderart.com, from the Vidvox ISF library, or from any
other collection. You can check that against the [library]({{ site.baseurl }}/library),
where each one's source is on the page.

**Almost none of the ideas in them are original.** Distance functions, the
smooth minimum, the cosine palette, gradient noise, domain warping, soft
shadows, ambient occlusion, sphere tracing, hash functions, tone curves: all of
these are other people's work, published and explained by them, and this course
uses them the way everyone else does. Each shader's `CREDIT` field names the
ones it uses, and the list below is the whole set.

One shader deserves a specific note. [Unit 30]({{ site.baseurl }}/learn/30-porting.html)
is about porting a Shadertoy into ISF, and the obvious way to write it would
have been to port a real one. Its shader is instead a **generic plasma written
for this course**, deliberately, so that a unit about other people's work does
not republish a specific person's without asking. The unit points readers at
real Shadertoys to port themselves, and tells them to check the licence and
credit the author.

## Who this course borrows from

**[Inigo Quilez](https://iquilezles.org/)** more than anyone else, and it is not
close. The two-dimensional and three-dimensional distance functions, the
polynomial smooth minimum, the cosine palette, the distance-field visualisation
used throughout Module C, domain warping and its constants, the Voronoi
true-edge pass, the soft-shadow estimator, the five-sample ambient occlusion,
the limited-repetition clamp, and the raymarching idiom itself. His articles are
cited on nearly every page of Phases 1 and 2 and the course would not exist in
this form without them.

**[Dave Hoskins](https://www.shadertoy.com/view/4djSRW)** for the fract-multiply
hash family, which every noise function in Module D is built on.

**Chris Wellons** for the `lowbias32` integer bit mixer, and **Mark Jarzynski
and Marc Olano** for [testing hash functions for GPU rendering](https://jcgt.org/published/0009/03/02/)
rather than guessing, which is why [Unit 12]({{ site.baseurl }}/learn/12-hashes.html)
recommends the one it does.

**[Ken Perlin](https://mrl.cs.nyu.edu/~perlin/paper445.pdf)** for gradient noise
and for the quintic interpolation curve.

**Steven Worley** for [cellular noise](https://dl.acm.org/doi/10.1145/237170.237267).

**John Hart** for [sphere tracing](https://link.springer.com/article/10.1007/s003710050084),
which is the algorithm the whole of Module G runs on.

**John Pearson** for the Gray-Scott parameterisation, **Alan Turing** for the
[idea that two diffusion rates make a pattern](https://royalsocietypublishing.org/doi/10.1098/rstb.1952.0012),
and **[Karl Sims](https://www.karlsims.com/rd.html)** for the clearest
explanation of the model and the weighted Laplacian this course uses.

**[Krzysztof Narkowicz](https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/)**
for the ACES filmic curve fit, and **Erik Reinhard** for the tone curve named
after him.

**Thomas Porter and Tom Duff** for [premultiplied alpha](https://dl.acm.org/doi/10.1145/800031.808606),
and the **PDF imaging model**, restated by the [CSS compositing specification](https://www.w3.org/TR/compositing-1/),
for the blend modes.

**Irwin Sobel and Gary Feldman** for the edge operator, and **Jim Blinn** for
the halfway-vector specular.

**[Vidvox](https://isf.video)** for the Interactive Shader Format itself, and
for the several hundred ISF shaders that ship inside *ossia score*.

**The [ossia](https://ossia.io) project** for *score*, for its ISF, compute, and
Vertex Shader Art processes, and for documentation precise enough that most of
Phase 4 could be written against it.

**[vertexshaderart.com](https://www.vertexshaderart.com/)** for the form
[Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html) teaches and the
uniform names it uses.

## Per shader

Generated from the `CREDIT` field of each file, so it cannot drift from what the
shaders actually say.

| Shader | Credit |
|:-------|:-------|
{% assign shaders = site.data.shaders | sort: "id" -%}
{% for s in shaders -%}
| [`{{ s.id }}`]({{ site.baseurl }}/library#shader-{{ s.id }}) | {{ s.credit }} |
{% endfor %}

## Licence

Unit text, figures, and shader sources are
[CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). The vendored
Jekyll theme keeps its MIT terms.

That licence covers this course's own work. It does not and cannot relicense
anything above: the distance functions, hashes, estimators, and curves remain
their authors', under whatever terms those authors chose, and this course uses
them on the same footing as everyone else does. Where a technique came from a
paper, the paper is cited; where it came from an article or a Shadertoy, that is
linked.

If you think something here is used wrongly, or credited wrongly, or not
credited at all, please
[open an issue]({{ site.gh_edit_repository }}/issues). That is a correction
worth making quickly and it will be.
