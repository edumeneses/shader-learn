# checks/p3-raymarched-scene

**Milestone P3.** Assembly and budgeting. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `p3-scene` | `library/shaders/p3/scene.fs` | nine towers in polar repetition, centrepiece, budget and cost views |

## Two real bugs, both from Unit 10, both fixed and both written into the unit

The reference scene first rendered with a wedge bitten out of whichever tower
sat nearest a sector boundary, and dark streaks across another. Both were the
polar fold, and neither looked like a field problem.

1. **The fold used arc length, `a * r`.** An arc is longer than the straight
   line between its ends, so the field over-estimated, and an over-estimating
   field lets the marcher step through a surface. Fixed by rebuilding a real
   position from the folded angle.
2. **The fold checked one sector.** Unit 10 said a fold assumes the nearest copy
   is in your own cell; in two dimensions breaking that chopped shapes, and here
   it over-estimates for a point whose true nearest tower is the neighbour's.
   Fixed by evaluating three sectors and taking the minimum.

Both are now explained in the milestone itself, because the reader will hit
them.

## Design notes

**Surface detail defaults to zero.** The control exists and raising it adds a
displacement, which breaks the metric and needs a lower step scale. Leaving it
on by default would have shipped a reference solution with artefacts in it; the
milestone asks the reader to make that trade deliberately instead.

The cost view distinguishes "used most of the budget" from "ran out". The unit
is explicit that red is not slow, it is **wrong**: the ray gave up before
reaching a surface, which is what produces soft haloes around silhouettes.

## Claims that were checked

- The `time` measurement in step 6 is a real workflow: three hundred frames at
  1920x1080 through `render.py`, divided. **The reference scene's own per-frame
  time has not been recorded here**, and the milestone asks the reader to record
  theirs. Do the same for the reference before this unit is treated as final.

## Corrections and open questions

- One tower in the reference render shows what looks like a notch at some camera
  angles. After both fold fixes it appears to be two adjacent towers of
  different heights overlapping in screen space, which is geometry rather than
  an artefact. **Not conclusively established.** If a reader reports it as a
  bug, believe them and look again.
