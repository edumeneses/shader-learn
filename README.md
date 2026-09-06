# Learn shader art

A zero-to-hero course in shader art, from a first fragment shader to a playable
visual instrument in [*ossia score*](https://ossia.io). Forty-seven units, each
ten to fifteen minutes to read, each ending with an exercise that has a success
criterion.

Authored by Eduardo Meneses, [Société des Arts Technologiques](https://sat.qc.ca),
Montréal. Unit text, figures, and shader sources are licensed
[CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/); the vendored
Jekyll theme keeps its MIT terms.

Published for review at <https://www.edumeneses.com/shader-learn/>. It is
**deliberately not indexed** while it is a draft: every page carries a `noindex`
tag, gated on `noindex: true` in `_config.yml`.

## What makes this course different from a book about shaders

**Every shader on the site is running.** Each technique appears as a live WebGL 2
player with every parameter on a control, beside a clip rendered offline on a
real GPU. The two agree, because they run the same string: `scripts/isf.py`
translates each ISF source to GLSL ES 3.00 once, and both the browser player and
the offline renderer consume that translation. There is no second parser to
drift.

**Every shader is ISF, and ISF is what *ossia score* loads.** The file you read
in a unit is the file you drop into *score*. Nothing is ported, rewritten, or
adapted between the page and the stage.

**Parameters are named for their role, never for the device that drives them.**
A shader takes a `focus`, not a mouse position; a `drive`, not an audio level.
The pointer in the browser, an OSC message from a phone, an automation curve in
a score document, and a hand on a MIDI fader reach the same input with nothing
renamed. That convention costs nothing and it is the difference between a shader
that lives in a browser tab and one that can be performed.

## Layout

```
docs/learn/NN-<slug>.md          unit pages; nav_order is the position in units.yml
docs/learn/assets/NN/            that unit's figures: .mp4 clips with .png posters
docs/learn/assets/shaders/       generated shader manifests, committed
library/shaders/NN/<name>.fs     the ISF sources, which are the deliverable
library/scores/                  runnable ossia score documents for Phase 4
checks/<slug>.md                 per-unit: what to re-verify, and corrections
figures/NN.json                  render specs: size, duration, parameter automation
figures/raw/                     intermediates, not committed
_data/units.yml                  unit numbers, slugs, titles, budgets, written flags
_data/modules.yml                module titles, phases, and blurbs
scripts/                         the toolchain, below
```

## The toolchain

```bash
./scripts/setup.sh                       # build .venv and prove the GPU path works
source .venv/bin/activate

python3 scripts/check_units.py           # run before every commit
python3 scripts/build_shaders.py         # after editing any .fs; commit the result
python3 scripts/validate_glsl.py         # stricter than the NVIDIA driver
python3 scripts/render.py --spec figures/07.json    # (re)make a unit's figures
./preview.sh                             # serve on 127.0.0.1:4000
```

Python 3.11 rather than the system default: the default here is 3.14, which has
no dev headers installed, and `moderngl` builds a C extension.

Ruby uses the vendored bundle. The system ruby names its bundler `bundle3.3`
rather than `bundle`; `preview.sh` prefers whichever exists.

### How figures are made

Offline, headless, on the local GPU, through an EGL context with no surface.
That path talks to `libEGL_nvidia` directly, so it needs no X server and no
compositor, and it produces the same pixels a year from now.

```bash
python3 scripts/render.py library/shaders/07/warp.fs \
    --out docs/learn/assets/07/07-01 --formats mp4,gif,png \
    --size 1280x720 --duration 8 --fps 30 --sweep warp=0.0:1.0
```

A `--sweep` or a keyframed `automation` block in a spec is how a clip
demonstrates what a parameter does: the figure moves the control the reader is
about to move. Encoding is NVENC where the format allows it, and raw frames are
piped straight to `ffmpeg` rather than written out, because a six-second 1080p
clip is about 1.5 GB of intermediate PNGs otherwise.

### Why GLSL ES 3.00 everywhere

WebGL 2 accepts nothing else, and desktop NVIDIA accepts ES shaders through
`ARB_ES3_compatibility`. One source string runs in the browser, in the offline
renderer, and in *score*. That is not a convenience; it is what makes a figure a
recording of the player rather than a lookalike.

## Continuous integration

`git push` to `origin` runs two workflows:

- **build** — structural checks, a check that the committed shader manifests
  match their sources, `glslangValidator` over every shader (stricter than the
  driver the figures are rendered on, so a shader that only compiles on NVIDIA
  fails here rather than in a reader's browser), a Jekyll build with each
  config, and html-proofer.
- **pages** — the same checks, then a production build and a Pages deployment.

## Hosting

Where this finally lives is undecided, and the page addresses under `/learn/`
are chosen to stay valid either way. The GitHub Pages deployment is a review
deployment, not a decision.

## Relation to the *ossia score* course

This course is a sibling of [Learn score](https://github.com/edumeneses/score-learn)
and reuses its Jekyll backend, its CI shape, and its discipline about grounding
every claim in the software. It does not follow score-docs' page template: a
course whose subject moves needs a page built around a player rather than around
a screenshot.
