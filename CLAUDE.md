# CLAUDE.md

Instructions for Claude Code working in this repository. Read `HANDOVER.md` next
for current status and the task queue.

## What this is

A zero-to-hero course in **shader art**: 47 units (42 lessons, 4 milestones, 1
capstone), each 10 to 15 minutes to read, taking a reader from a first fragment
shader to a playable visual instrument running in *ossia score*.

Authored by Eduardo Meneses (Société des Arts Technologiques, Montréal). Content
is CC BY-SA 4.0; the vendored Jekyll theme keeps its MIT terms.

Published for review at <https://www.edumeneses.com/shader-learn/>. **Deliberately
not indexed** while it is a draft: every page carries a `noindex` tag, gated on
`noindex: true` in `_config.yml`. Do not remove that without being asked.

## Non-negotiables

1. **Every shader is ISF, and it is the deliverable.** A unit's shader is a real
   file under `library/shaders/`, it compiles, it runs in the browser player, it
   runs in the offline renderer, and it opens in *ossia score* with no edit. A
   snippet in a fenced code block that is not backed by a file is a snippet, not
   a shader; use one only to show a fragment of a file that does exist.
2. **Parameters are named for their role, never for the device.** `focus`, not
   `mouse`. `drive`, not `audioLevel`. `origin`, `warp`, `density`, `tilt`. This
   is the course's central convention and the reason its shaders are usable in a
   performance. Every number that changes the picture is an input with a `LABEL`,
   a `MIN`, and a `MAX`.
3. **GLSL ES 3.00 throughout.** WebGL 2 accepts nothing else; NVIDIA accepts it
   through `ARB_ES3_compatibility`; *score* produces it. One source, three
   runtimes. Never write a shader that only compiles as desktop GLSL.
4. **Published permalinks are contractual.** Future videos will point at them.
   Never rename a written unit's slug; add a new number or a Part II instead.
5. **`_data/units.yml` is the single source of truth** for unit numbers, slugs,
   titles, and budgets. `scripts/check_units.py` validates every page against it.
6. **Ground every claim in the software or its documentation.** Read the
   reference page, or run the shader, before asserting behaviour. When the docs
   and the build disagree, believe the build and record it in `checks/`.
7. **Units that touch *ossia score* target 3.8.2**, the AppImage at
   `~/Applications/ossia.score-3.8.2-linux-x86_64.AppImage`. They declare
   `score_version`; `check_units.py` fails on a mismatch. Units in Phases 1 to 3
   need no *score* at all and must not require it.
8. **No specific hardware for the reader.** Figures are rendered on an RTX 4080
   Super because a recording has to be made somewhere. Nothing in the course may
   need that card. Where a technique is genuinely expensive, say so and give the
   cheaper version.
9. **Every unit stays inside 1,200 to 1,800 words** of body prose. Code fences
   and link targets are stripped before counting, so a unit with a lot of GLSL
   still has to carry its prose.

## Layout

```
docs/learn/NN-<slug>.md          unit pages; nav_order is the position in units.yml
docs/learn/assets/NN/            that unit's figures: .mp4 clips with .png posters
docs/learn/assets/shaders/       generated shader manifests, committed
library/shaders/NN/<name>.fs     the ISF sources
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
./scripts/setup.sh                    # build .venv, prove the GPU path works
source .venv/bin/activate

python3 scripts/check_units.py        # run before every commit
python3 scripts/build_shaders.py      # after editing any .fs; COMMIT the result
python3 scripts/validate_glsl.py      # glslang; stricter than the NVIDIA driver
python3 scripts/render.py --spec figures/07.json
./preview.sh                          # serve on 127.0.0.1:4000
```

`check_units.py` enforces required front matter, the word budget, permalink
stability, the pinned *score* version, that a `checks/` note exists, that the
page matches its entry in `units.yml`, that every internal `/learn/` link
resolves to a known slug, that every embedded shader has a built manifest, and
that every figure's files exist.

**`build_shaders.py` output is committed and CI checks it.** Editing a `.fs`
without rebuilding is the most likely way to break the build, and the failure
reads as "manifest is out of date", not as a shader error.

## The architecture, and why

`scripts/isf.py` is the single ISF implementation. It parses the JSON header and
translates the GLSL to ES 3.00. Everything downstream reads it:

- `scripts/render.py` renders figures on the GPU;
- `scripts/build_shaders.py` writes the manifests the browser player consumes;
- `scripts/check_units.py` and `validate_glsl.py` refuse to publish what will not
  compile.

**The browser never parses ISF.** An earlier design would have had a second
parser in JavaScript, and any divergence between the two would show up as a
figure that does not match the player beside it. If you find yourself adding ISF
knowledge to `assets/js/shader-player.js`, it belongs in `isf.py` and in the
manifest instead.

## Facts about the rendering setup. Do not rediscover these.

- **The GPU path is EGL with no surface**, through moderngl's `egl` backend, at
  `require=460`. It needs no X server and no compositor, which is why it works
  where a screen capture does not.
- **A GPU surface cannot be screen-captured under this compositor.** The score
  course records this: the window's own drawable reads black and a root capture
  of the region reads flat grey. That is the reason `render.py` exists rather
  than a screenshot script. For *ossia score*'s own interface, `scripts/capture.py`
  and `scripts/typeinto.py` (both carried over from the score course) still work;
  for shader output, always render offline.
- **`#version 300 es` compiles on desktop NVIDIA** through `ARB_ES3_compatibility`,
  which is what makes browser-and-renderer parity possible. Confirmed on driver
  580.173.02, GL 4.6.
- **glslang is stricter than the NVIDIA driver, and this has bitten twice.**
  `flat` and `sample` are reserved words and `round` is a built-in; the driver
  here compiled all three as ISF input names and glslang rejected them, which
  would have reached a reader as a blank player. `scripts/isf.py` now refuses
  reserved words, built-in function names, and the names ISF itself supplies, at
  parse time and by name. **Run `validate_glsl.py` before pushing anyway**;
  `scripts/setup.sh` fetches glslang into `.venv/bin`, so it is available without
  root.
- **OpenGL reads pixels bottom-up.** `render.py` flips before encoding. A figure
  that comes out mirrored vertically is that flip, not the shader.
- **NVENC is used for mp4 and CPU libvpx for webm.** If NVENC sessions are
  exhausted, `--no-nvenc` falls back to libx264 and is about four times slower.
- **A GIF needs the two-pass palette filter.** A default GIF encode bands every
  gradient, and gradients are most of what these figures are.
- **The system python is 3.14 and has no dev headers**, so `moderngl` will not
  build against it. Use 3.11, which `scripts/setup.sh` does.
- **The system bundler is `bundle3.3`, not `bundle`.** `preview.sh` prefers
  whichever exists.
- **The interface cannot be driven here, and `checks/VERIFICATION.md` says why.**
  `scripts/capture.py` finds, sizes, and captures the score window, and XTEST
  **pointer** motion works. Focus can also be taken: `_NET_ACTIVE_WINDOW` with
  source 1 and a **real server timestamp** is accepted where `capture.py`'s
  source-2 `CurrentTime` version is refused. **Keys still do not arrive**,
  because Xwayland runs with `-enable-ei-portal` and XTEST keyboard is not
  delivered to the client. Do not spend another hour on focus; it is not the
  problem. Install Xephyr and run score nested, or do the keyboard steps by hand.
- **`DISPLAY=:1` is not a second X server.** One Xwayland is started with two
  listen descriptors and serves `:0` and `:1` identically. The score course used
  `:1` and its figure notes say the work "needs an unlocked session", which is
  the real reason its keystrokes landed.
- **A `.score` document is JSON, and reading one is better evidence than a
  screenshot.** The inlets *score* builds from a shader's JSON header can be read
  directly, in bulk, across every example ossia ships. That is how the course's
  central claim about inputs becoming inlets was verified across 44 processes.
- **`$sl-*` palette variables live in `_sass/support/_variables.scss`**, not in
  the colour scheme, because `_sass/custom/custom.scss` is imported by the
  vendored light and dark schemes too and a variable defined in only one scheme
  breaks the build of the others.

## Facts about ISF. Do not rediscover these.

- **The JSON header is not necessarily the first thing in the file.** Shaders
  exported from other tools carry a licence banner above it, so the parser takes
  the first block comment containing a JSON object rather than the first bytes.
- **`gl_FragColor` is ISF's output** and is `#define`d onto a real ES 3.00 `out`
  variable. Never declare your own `out` in a course shader.
- **`isf_FragNormCoord` is the coordinate to use**, not `gl_FragCoord`. *score*'s
  pipeline can run on OpenGL, Vulkan, Metal or Direct3D, whose coordinate systems
  differ in Y direction; the ossia documentation is explicit about this. A shader
  that reaches for `gl_FragCoord` is a shader that will be upside down on someone
  else's machine.
- **Images are read through the `IMG_` accessors**, which the translation expands
  into a per-image texture read. `IMG_NORM_PIXEL(tex, uv)` and `IMG_SIZE(tex)`
  are the two you want; the translation refuses a name that is not a declared
  image, pass target, or import, which catches a typo at build time.
- **A `PERSISTENT` pass target reads what it wrote last frame.** Both the
  renderer and the player double-buffer it. That is the whole mechanism behind
  Unit 18 and Unit 19.
- **ossia score's compute variant names its input block `RESOURCES`**, not
  `INPUTS`, and requires `"MODE": "COMPUTE_SHADER"` plus at least one `PASSES`
  entry with a `LOCAL_SIZE` and an `EXECUTION_MODEL`. The parser reads both
  spellings; the browser player cannot run compute at all and says so.

## Attribution

**Every shader here was written for this course; almost none of the ideas were.**
`docs/attribution.md` is the page that says whose they are, and its per-shader
table is generated from each file's `CREDIT` field, so the two cannot drift.

When you write or edit a shader, **put what it borrows in `CREDIT`**, by name.
The audit that produced the current credits found 24 gaps, all of them things
used correctly and cited nowhere. Inigo Quilez alone accounts for the distance
functions, the smooth minimum, the cosine palette, domain warping, the Voronoi
edge pass, the soft-shadow and occlusion estimators, and the field visualisation
used throughout Module C.

Unit 30's shader is deliberately a generic plasma rather than a real Shadertoy
port, so that a unit about other people's work does not republish a specific
person's. Keep it that way.

## Writing style

Edu's profile is at `/media/Storage/Assistant/writing_style_profile.md` when that
directory exists; read it before drafting prose. In short: direct topic
sentences, active voice, Oxford commas, **no em or en dashes**, semicolons for
parallel clauses, precise transitions ("However", "In contrast", never "Also"),
no clichés, no padding. Units are written to be read aloud: "Why this matters",
"The idea", and "Exercise" become video narration nearly verbatim.

Each unit follows one shape: a before/need/build blockquote, Why this matters,
The idea, a numbered *Build it* walkthrough with the live player, *Look at
these* (Shadertoy and elsewhere), Common mistakes, Exercise, Going further.

## Commits

One commit per coherent block. Write what changed and *why*, and record what was
learned or corrected; the commit log is part of the project's memory. Run
`check_units.py`, `build_shaders.py --check`, and a build before committing.
`git push` goes to `origin` on Edu's personal GitHub, which triggers CI:
structural checks, manifest freshness, glslang validation, a build with each
config, html-proofer, and a Pages deploy.
