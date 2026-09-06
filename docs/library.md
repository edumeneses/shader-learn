---
layout: default
title: Shader library
nav_order: 2
permalink: /library
---

# The shader library

{: .no_toc }

Every shader in the course, in one place, each one running. There are no
snippets here: each card is a real file under `library/shaders/` in the
[repository]({{ site.gh_edit_repository }}), it compiles, and it opens in
*ossia score* with its controls already built.

Players start when you scroll to them and give their graphics context back when
you scroll away, so this page can hold all of them at once without exhausting
the browser.

1. TOC
{:toc}

## How to use one of these

1. Press `‹›` on any player to see its source, and `copy` to take it.
2. Save it with a `.fs` extension.
3. Drag it onto an interval in *ossia score*, or drop it into the user library
   folder and pick it from the process list.
4. The inputs in its JSON header become inlets, with the ranges the header
   declares. Drive them with an automation curve, an OSC address, a MIDI
   control, or another process's output; the shader neither knows nor cares.
   [Unit 37]({{ site.baseurl }}/learn/37-parameters-as-inlets.html) is that step
   in full.

Licence is [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/).
Where a shader is based on someone else's work, its `CREDIT` field says so and
the card repeats it.

{% assign shaders = site.data.shaders | sort: "id" %}

## Every shader

<div class="shader-index" markdown="0">
{% for shader in shaders %}
  {% assign unit = site.data.units | where: "num", shader.unit | first %}
  <section class="shader-card" id="shader-{{ shader.id }}">
    <h3><code>{{ shader.id }}</code></h3>
    <p class="shader-card-meta">
      {% if unit %}
        {% if unit.written %}
          <a href="{{ site.baseurl }}/learn/{{ unit.slug }}.html">Unit {{ unit.num }}: {{ unit.title }}</a>
        {% else %}
          Unit {{ unit.num }}: {{ unit.title }} (not yet written)
        {% endif %}
        &middot;
      {% endif %}
      {{ shader.inputs }} input{% if shader.inputs != 1 %}s{% endif %}
      &middot; {{ shader.passes }} pass{% if shader.passes != 1 %}es{% endif %}
      {% if shader.mode == "compute" %}&middot; compute{% endif %}
      {% for category in shader.categories %}&middot; {{ category }}{% endfor %}
    </p>
    <p class="shader-card-desc">{{ shader.description }}</p>
    {% if shader.credit and shader.credit != "" %}
      <p class="shader-card-credit">{{ shader.credit }}</p>
    {% endif %}
    {% include shader.html id=shader.id height="300" %}
    <p class="shader-card-path"><code>{{ shader.source }}</code></p>
  </section>
{% endfor %}
</div>

## What the fields mean

**Inputs** is how many named parameters the shader exposes. A shader in this
course exposes every number a viewer might want to move, so a low count usually
means the shader is deliberately minimal rather than that it is simple.

**Passes** is how many times the shader runs per frame. One is the normal case.
More than one means the shader writes into a buffer that a later pass reads,
which is how feedback and separable blurs work;
[Unit 18]({{ site.baseurl }}/learn/18-feedback.html) and
[Unit 21]({{ site.baseurl }}/learn/21-convolution.html) are the two units that
need it.

**Compute** marks a shader with no fragment stage. WebGL 2 has no compute stage,
so those cards show an explanation rather than a player; they run in *ossia
score* and in the rendered clips.
[Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html) covers them.
