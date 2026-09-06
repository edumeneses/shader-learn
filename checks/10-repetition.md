# checks/10-repetition

**Unit 10.** Domain repetition. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `10-repetition` | `library/shaders/10/repetition.fs` | none, infinite, limited, and polar, field or shape view |

## Attribution

The limited-repetition clamp-the-index form is Inigo Quilez's, credited in the
shader.

## Re-verify when

- **The shader is edited.** The unit quotes all three folds verbatim and its
  steps depend on the `LABELS` order and on the Cell size and Shape size
  controls being able to produce the chopping trap.

## Design notes

**The shape view is the default here**, unlike Units 07 to 09. The subject is a
pattern, and a pattern is read as shapes; the field view is used only in step 3,
where the discontinuous isolines are the evidence that the field lies.

The controls deliberately allow an invalid combination. A reader has to be able
to make the trap happen; a shader that silently clamped the shape to fit its
cell would have hidden the unit's most important point.

## Corrections and open questions

- The unit states that GLSL's `mod` returns a result with the sign of the
  divisor, unlike C's `%`. Confirmed against the spec: `mod(x, y)` is
  `x - y * floor(x/y)`.
- The neighbour-checking correct form is described and **not** implemented in
  the shader. It is nine evaluations and would have made the shader harder to
  read for a case the unit tells the reader to avoid rather than handle. Unit 16
  implements it, where it is unavoidable.
