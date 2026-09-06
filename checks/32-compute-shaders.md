# checks/32-compute-shaders

**Unit 32.** Compute shaders. Pinned to *ossia score* 3.8.2.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `32-histogram` | `library/shaders/32/histogram.fs` | `MODE: COMPUTE_SHADER`, shared memory, `atomicAdd`, two barriers |

## Figures

| name | notes |
|:-----|:------|
| `32-01` | rendered by `figures/32.json`; the player cannot run this shader |

## This unit needed a compute path in the renderer

`render.py` previously refused compute shaders with a message pointing at a
script that did not exist. It now has `ComputeRenderer`: it assembles GLSL 4.30
from the `RESOURCES` block, creates each declared image, binds them, computes
the workgroup grid from `EXECUTION_MODEL`, dispatches, issues a memory barrier,
and reads the output image back.

**Desktop GLSL 4.30, not ES 3.00.** GLSL ES has no compute stage, which is also
why the browser player cannot run these and says so on the page rather than
failing silently. That boundary is the unit's own subject.

## Design notes

**The histogram is per workgroup and the unit says so.** The first version drew
what looked like a global histogram across the bottom of the image and was in
fact a smear of per-group results. Drawing each group's histogram *inside its own
tile* is honest and more informative: the mosaic shows how tonal distribution
varies across the frame, and each tile is visibly one workgroup's work.

A global histogram needs a second pass reducing through a buffer. The unit says
that too, rather than implying one pass can do it.

## Corrections and open questions

- **The compute path has been tested on exactly one shader and one GPU.**
  `EXECUTION_MODEL` with `"TYPE": "MANUAL"` is implemented and untested. Buffers
  (SSBOs) are not implemented at all; only images and textures.
- The unit's exercise asks for a three-pass reduction with persistent state.
  **`ComputeRenderer` cannot do multi-pass or persistence**, so the exercise is
  currently only completable in ossia score. Say so, or build it.
