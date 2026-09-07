# Handover

Status and queue for *Learn shader art*. Read `CLAUDE.md` first for the rules and
the toolchain; this file is what is done, what is next, and what is waiting on a
decision.

Last updated 2026-09-06, end of the first session.

## Where things stand

**All 47 units are written.** 36 shaders, all compiling under glslang, all
listed at `/library`, all but the compute one running live in the browser. The
site builds under both configs, html-proofer passes, and CI is green.

| Module | Units | State |
|:-------|:------|:------|
| A Orientation | 00 to 02 | written |
| B Coordinates and colour | 03 to 06 | written |
| C Shapes as fields | 07 to 11, P1 | written |
| D Noise and randomness | 12 to 16, P2 | written |
| E Time, feedback, and state | 17 to 19 | written |
| F Images and processing | 20 to 23 | written |
| G Three dimensions | 24 to 27, P3 | written |
| H Formats and stages | 28 to 33 | written |
| I What a shader costs | 34 | written |
| J Shaders in ossia score | 35 to 41, P4 | **written, unverified in the app** |
| K Capstone | 42 | written |

## Start here: finish verifying Phase 4

A first verification pass has happened and **`checks/VERIFICATION.md` is the
record**. Read it before doing any more.

What it establishes: the course's central claim, that an ISF input becomes an
inlet with its default and its range, is **verified across 44 ISF processes in
20 documents ossia ships**. So are the type mapping, the `Window:/` address, and
the VSA header keys. One overclaim in Unit 31 was found and corrected, Unit 30
gained a real shipped example of the mistake it warns about, and Units 29 and 37
gained a genuine trap: a float input with no `MIN`/`MAX` gets an inlet domain of
0 to 0, which is in a shipped example and makes any automation curve on it
produce zero.

The method that worked was reading `.score` documents, which are JSON, rather
than driving the interface. It is faster, it covers every example at once, and
it produces citations.

**Driving the interface is blocked on this machine.** score launches, its window
can be found and captured, and XTEST pointer motion works; but this is a Wayland
session and no X window ever holds keyboard focus, so injected keystrokes never
reach score. Do not retry it. Install `xserver-xephyr` and run score nested, do
the keyboard steps by hand, or use an X11 session.

What is still unverified, in priority order, is at the end of
`checks/VERIFICATION.md`. The top item is Unit 36's claim that a compile error
leaves the last working shader rendering, because the unit tells a reader they
can rely on it in a performance.

## Decisions waiting on Edu

**1. Where this is published.** The Pages deployment at
<https://www.edumeneses.com/shader-learn/> is a review deployment. `noindex` is
on. Nothing about the addresses under `/learn/` needs to change whichever way
this is settled, which was the point of choosing them that way.

**2. Whether Phase 4 pins to *score* 3.8.2 or moves to a newer build.** Every
Phase 4 unit and every screenshot will be against whatever is pinned, and
`~/Applications/` also holds two `master` builds. 3.8.2 is what `_config.yml`
and `check_units.py` currently enforce.

**3. Whether the course ships a small sound file** for the audio-reactive work.
Unit 40 and the P4 milestone both want one. The renderer already synthesises a
deterministic four-on-the-floor signal, and the player generates the identical
signal in JavaScript, so a figure and a player agree without any asset. A real
file would be better material and is a licensing question rather than a
technical one.

## Known gaps, in the order they will bite

- **Phase 4 is unverified**, above. Everything else on this list is smaller.
- **No *ossia score* figures at all.** Every Phase 4 unit wants at least one and
  none can be rendered offline, because the subject is the application.
- **Two rendered clips so far**, `p2-01` and `32-01`. The live players carry the
  load elsewhere, which is the design, but Modules C to G would each be better
  with one clip sweeping the parameter the unit is about. `render.py --sweep`
  jobs, no new machinery.
- **`figures/03.json` does not exist and is wanted.** Unit 03 needs the same
  circle at several aspect ratios, which needs a montage step in `render.py` or
  the ability to animate `RENDERSIZE`. The montage is cheaper.
- **Unit 39 describes a motion detector and ships no shader for it.**
- **The player does not build mipmaps** for image inputs; the offline renderer
  does. A heavily minified image therefore aliases in the browser and not in a
  rendered figure. `checks/20-sampling.md`.
- **VSA `sound` and `floatSound` are declared and never fed.** A VSA shader that
  samples them gets an unbound sampler. `checks/31-vertex-shaders.md`.
- **The compute path has been tested on one shader and one GPU.** `EXECUTION_MODEL`
  with `"TYPE": "MANUAL"` is implemented and untested, buffers are not
  implemented, and multi-pass and persistence are not supported, which makes Unit
  32's exercise completable only in *score*. `checks/32-compute-shaders.md`.
- **The browser and the offline renderer use different float precision** for
  persistent buffers: RGBA16F in WebGL, full float offline. No visible difference
  so far. `checks/18-feedback.md`.
- **Units 21, 26, and 27 work in code space rather than linear light**, which is
  wrong by Unit 04's own rule. Each unit lists it under Common mistakes rather
  than quietly doing the right thing, because every reference shader a reader
  meets shares the shortcut. Revisit if a later unit depends on it.
- **Unit 19's shader has not been tested on a second GPU.**

## Things that were learned the hard way

All of these are in `CLAUDE.md` in full. In brief, because they cost real time:

- **glslang is stricter than the NVIDIA driver, and this bit five times.**
  `flat`, `sample`, `layout` are reserved words; `round` and `reflect` are
  built-ins. All five compiled here and would have failed in every browser.
  `isf.py` now refuses reserved words, built-in names, and the names ISF itself
  supplies, at parse time and by name, and `scripts/setup.sh` fetches glslang
  into `.venv/bin` so the check runs before a push rather than in CI. The last
  two were caught locally, which is the guard working.
- **A persistent target must swap after the pass that writes it**, not at the
  end of the frame, or several passes on one target overwrite each other instead
  of stepping. Both runtimes were changed together and must stay in agreement.
- **A simulation cannot iterate inside one pass.** Its neighbourhood comes from a
  texture that does not update mid-pass.
- **A stateful shader cannot be sampled at an instant.** A still has to be
  stepped to from frame zero, which `render.py` now does automatically.
- **A polar fold must rebuild a real position, not use arc length**, and must
  check neighbouring sectors. In two dimensions Unit 10's trap chops shapes; in
  three it makes the field over-estimate and the marcher steps through surfaces.
- **A PNG poster of a noise field is larger than the H.264 clip it posters.**
  Clip posters are downscaled to 960 wide.

## The next three commits, if nothing changes

1. Verify Phase 4 in a running *score* 3.8.2 and correct what is wrong. Capture
   figure 37-01 while there, which is the most valuable picture in the course.
2. Clips for Modules C to G, one per unit, sweeping the parameter each unit is
   about.
3. The smaller gaps above, in whatever order they start mattering.
