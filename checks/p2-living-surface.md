# checks/p2-living-surface

**Milestone P2.** Assembly, no new technique. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `p2-living-surface` | `library/shaders/p2/living-surface.fs` | two-level warp plus Voronoi, both on one closed path |

## The loop was verified, not assumed

Rendered at 320x180 at `TIME` 0 and at `TIME` 8 with `period` 8, and compared
per pixel: **maximum difference of 1 out of 255 in each channel**, which is
float rounding. The milestone gives the reader the same two commands and the
same criterion, so this is a check they can repeat rather than a claim they have
to trust.

## Re-verify when

- **The reference shader is edited.** The unit names three decisions in it: the
  closed path applied at the warp stage rather than to `p`, the Voronoi
  travelling on the same path scaled by 1.6, and lacunarity 2.02.
- **`scripts/render.py`'s interface changes.** Steps 3 and 4 give literal
  command lines.

## Design notes

The milestone argues against crossfading rather than describing it neutrally.
That is a position, and it is the right one: a crossfaded loop is a double
exposure of itself for a second in every cycle, and the periodic-input method
costs nothing and is exact.

## Corrections and open questions

- **The two-dimensional circle uses two of the noise's dimensions for the loop,
  which imposes a slight rotational bias on the motion.** Three-dimensional
  noise with the circle in the third and fourth dimensions would avoid it. The
  unit says so rather than pretending the method is bias-free. Not visible at
  the reference settings; **verify again if travel is raised much above 1**.
