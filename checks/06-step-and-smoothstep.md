# checks/06-step-and-smoothstep

**Unit 06.** Edges and coverage. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `06-edge` | `library/shaders/06/edge.fs` | four coverage methods, a magnifier that snaps to the nearest edge |

## Re-verify when

- **The shader is edited.** The unit refers to methods by number, 0 to 3,
  matching the `VALUES` array, and tells the reader to read `coverage()` and to
  notice the `0.75` factor on `fwidth`.

## Design notes

**The magnifier snaps to the nearest edge, and it has to.** At fourteen times,
the lens sees about two hundredths of a unit; a magnifier that showed wherever
the reader dropped it would show flat colour almost always, and the figure would
read as broken rather than instructive. The snap uses the field's own value and
gradient, which is a preview of the fact Unit 24 marches a ray with.

**The shape carries a standing tilt of 0.32 radians.** An aliased edge on a
perfect horizontal looks fine, so an axis-aligned shape would have hidden the
very artefact the unit is about.

The rounded rectangle rather than a circle: it gives the lens both curved and
near-straight runs to land on.

## Corrections and open questions

- The unit says `fwidth` over-estimates at a corner and that fixes cost more
  than the problem is worth. That is a judgement, and it is the standard one.
  It has **not** been demonstrated in the figure; a reader who zooms into an
  actual corner will see slightly soft geometry and should be able to find out
  why from the prose alone.
