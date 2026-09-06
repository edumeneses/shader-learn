---
layout: default
title: Units
nav_order: 1
has_children: true
permalink: /learn
---

# The course

{: .no_toc }

Forty-seven units in four phases. Each is one page, written so that it also
works as the script for a video, and each ends with an exercise that has a
success criterion you can check without asking anyone.

Units are numbered, and the numbers are the order to read them in. A unit
assumes every unit before it and nothing after it.

1. TOC
{:toc}

<div class="syllabus" markdown="1">

{% assign phases = "1,2,3,4" | split: "," %}
{% assign phase_titles = "Foundations,Techniques,Formats and cost,Shaders in ossia score" | split: "," %}

{% for phase in phases %}
{% assign phase_index = forloop.index0 %}

## Phase {{ phase }}: {{ phase_titles[phase_index] }}

{% assign phase_modules = site.data.modules | where: "phase", phase %}
{% for module in phase_modules %}
{% assign module_units = site.data.units | where: "module", module.id %}
{% assign written_count = module_units | where: "written", true | size %}

### {{ module.title }}

{{ module.blurb }}

| Unit | Title | Read | Practice |
|:-----|:------|-----:|---------:|
{% for unit in module_units -%}
| {{ unit.num }} | {% if unit.written %}[{{ unit.title }}]({{ site.baseurl }}/learn/{{ unit.slug }}.html){% else %}{{ unit.title }}{% endif %}{% if unit.kind == "milestone" %} *(milestone)*{% endif %}{% if unit.kind == "capstone" %} *(capstone)*{% endif %}{% if unit.score %} · needs *score*{% endif %} | {{ unit.read }} min | {% if unit.practice == 0 %}—{% else %}{{ unit.practice }} min{% endif %} |
{% endfor %}

{% if written_count == 0 %}*Not yet written.*{% endif %}

{% endfor %}
{% endfor %}

</div>

## How a unit is built

Every unit follows the same shape, so that a reader knows where to look for
what they need:

- **A header block** saying what you should have read first, what you need in
  front of you, and what you will have made by the end.
- **Why this matters**, one or two paragraphs on the problem the technique
  solves. This is the part that becomes narration.
- **The idea**, the concepts named and defined, with the maths written out
  where the maths is the point.
- **Build it**, a numbered walkthrough that ends with a running shader, with
  the live player beside it.
- **Look at these**, links to Shadertoy and elsewhere showing the technique
  taken further than the unit takes it.
- **Common mistakes**, the failures that look like something else.
- **Exercise**, one task with a success criterion.
- **Going further**, the reference pages and papers.

## Conventions the whole course keeps

**Every shader is ISF.** The Interactive Shader Format is GLSL with a JSON
header declaring the shader's inputs. It is what *ossia score* loads, what the
players on these pages run, and what the rendered figures were made from. Unit
29 covers the format in full; before that, you only need to know that the
header is where a parameter is declared.

**Parameters are named for their role.** A shader takes a `focus`, not a mouse
position. The pointer, an OSC message, an automation curve, and a MIDI fader
all reach the same input with nothing renamed. This costs nothing to do and it
is the difference between a shader that lives in a browser tab and one that
can be performed.

**Nothing is hard-coded that a reader might want to move.** If a number
changes the picture, it is an input with a range and a label.

**GLSL ES 3.00 throughout.** It is what WebGL 2 accepts and what *score*
produces. A shader from any unit runs unmodified in the browser, in the
offline renderer that made the figures, and in *score*.
