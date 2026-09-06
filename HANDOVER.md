# Handover

Status and queue for *Learn shader art*. Read `CLAUDE.md` first for the rules and
the toolchain; this file is what is done, what is next, and what is waiting on a
decision.

Last updated 2026-09-06.

## Where things stand

**22 of 47 units written.** Phases 1 and part of 2:

| Module | Units | State |
|:-------|:------|:------|
| A Orientation | 00 to 02 | written |
| B Coordinates and colour | 03 to 06 | written |
| C Shapes as fields | 07 to 11, P1 | written |
| D Noise and randomness | 12 to 16, P2 | written |
| E Time, feedback, and state | 17 to 19 | written |
| F Images and processing | 20 to 23 | **not started, blocked, see below** |
| G Three dimensions | 24 to 27, P3 | not started |
| H Formats and stages | 28 to 33 | not started |
| I What a shader costs | 34 | not started |
| J Shaders in ossia score | 35 to 41, P4 | not started |
| K Capstone | 42 | not started |

**21 shaders**, all compiling under glslang, all running in the browser player,
all listed at `/library`. The site builds under both configs and html-proofer
passes. CI is green.

## Start here: Module F is blocked on one missing feature

**Nothing in the toolchain can supply an image to a shader.** ISF `image` inputs
are parsed, declared as samplers, and reachable through the `IMG_` accessors,
and neither `scripts/render.py` nor `assets/js/shader-player.js` ever binds
anything to them. Module F is entirely about shaders that take an input, so it
cannot start until this exists.

The design that fits the project's one-source rule:

1. **Write the test card as an ISF shader**, `library/shaders/20/testcard.fs`:
   colour bars, a greyscale ramp, frequency wedges, and a region with fine
   detail, all procedural.
2. **Render it once to `docs/learn/assets/images/testcard.png`** and commit it,
   with a spec under `figures/` so it is reproducible.
3. **Both runtimes load that PNG**, so there is one source and no second
   implementation to drift. Writing the test card twice, once in PIL and once in
   canvas 2D, would reintroduce exactly the divergence the manifest design was
   built to prevent.
4. **`render.py` gains `--image name=path`**, defaulting to the test card.
5. **The player gains an image source picker** per `image` input: the test card,
   a file the reader drops in, and a webcam. The webcam is worth having: Module
   F is about processing an input and a reader's own face is a better test
   image than any card.

Note that the player already caps live WebGL contexts at eight and releases the
least recently seen, so a video or webcam texture must be released in
`_release()` too or it will leak.

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

- **No *ossia score* figures at all.** `scripts/capture.py` and
  `scripts/typeinto.py` are carried over from the score course and have not been
  run in this repository. Every Phase 4 unit needs at least one figure and none
  of them can be rendered offline, because the subject is the application.
  `checks/FIGURES-PENDING.md` has the details.
- **Only one rendered clip so far**, `docs/learn/assets/p2/p2-01.mp4`. The live
  players carry the load elsewhere, which is the design, but Modules C to G
  would each be better with one clip sweeping the parameter the unit is about.
  These are `render.py --sweep` jobs and need no new machinery.
- **`figures/03.json` does not exist and is wanted.** Unit 03 needs the same
  circle at several aspect ratios, which needs either a montage step in
  `render.py` or the ability to animate `RENDERSIZE`. The montage is cheaper.
- **The browser and the offline renderer use different float precision** for
  persistent buffers: RGBA16F in WebGL, full float offline. No visible
  difference so far. Recorded in `checks/18-feedback.md`.
- **Unit 19's shader has not been tested on a second GPU.** It claims two GPUs
  diverge within a minute, which is the standard expectation for a chaotic
  system in float arithmetic and is untested here.

## Things that were learned the hard way

All of these are in `CLAUDE.md` in full. In brief, because they cost real time:

- **glslang is stricter than the NVIDIA driver, and this bit three times.**
  `flat` and `sample` are reserved words, `round` is a built-in, and all three
  compiled locally and would have failed in every browser. `isf.py` now refuses
  them at parse time, and `scripts/setup.sh` fetches glslang into `.venv/bin` so
  the check can run before a push rather than in CI.
- **A persistent target must swap after the pass that writes it**, not at the
  end of the frame. Otherwise several passes on one target overwrite each other
  instead of stepping. Both runtimes were changed together and must stay in
  agreement.
- **A simulation cannot iterate inside one pass.** Its neighbourhood comes from
  a texture that does not update mid-pass.
- **A stateful shader cannot be sampled at an instant.** A still has to be
  stepped to from frame zero, which `render.py` now does automatically.
- **A PNG poster of a noise field is larger than the H.264 clip it posters.**
  Clip posters are downscaled to 960 wide.

## The next three commits, if nothing changes

1. Image input support in both runtimes, plus the test-card shader and its
   committed PNG. Unblocks Module F.
2. Units 20 to 23, with their shaders.
3. Module G, which is the largest remaining block of new material and the one
   whose shaders are most expensive to get right.
