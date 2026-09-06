# checks/09-transforms

**Unit 09.** Inverse transforms and the scale correction. No *ossia score*.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `09-transforms` | `library/shaders/09/transforms.fs` | correct, uncompensated, and non-uniform scale, field or shape view |

## Re-verify when

- **The shader is edited.** The unit refers to modes 0, 1, and 2 by number and
  quotes `float d = shape(p / s) * s;` as the correct form.

## Design notes

**A cross rather than a circle.** Rotation has to be unmistakable, and a
non-uniform stretch has to be obviously wrong; a circle would have hidden both.

**Mode 1 exists to be looked at, not used.** It renders a correct outline with a
lying field, which is precisely the bug that survives in real work because the
picture looks right. The unit's step 5 spells out the three places the lie
surfaces.

The non-uniform mode multiplies by `min(sx, sy)`, a conservative Lipschitz bound
rather than a correct distance. The unit says so and says why an exact ellipse
function is fifteen lines instead.

## Corrections and open questions

- The unit recommends elongation over scaling for making a shape longer, and
  gives the two-line form. That form is exact for shapes symmetric about the
  elongation axis and is **not** exact in general. The unit does not currently
  say so. Fix if a reader is bitten by it.
