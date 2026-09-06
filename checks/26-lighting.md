# checks/26-lighting

**Unit 26.** Normals, lighting, and shadows. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `26-lighting` | `library/shaders/26/lighting.fs` | six views, one per lighting term; hard and soft shadows |

## Attribution

The soft-shadow estimator is Inigo Quilez's.

## Design notes

**Every term has its own view.** That is the unit's method: a render that looks
wrong is almost always one term, and looking at them one at a time turns an hour
of guessing into ten seconds. The normals view in particular is the most useful
debugging tool in three dimensions and a reader should leave this unit reaching
for it by reflex.

Normal epsilon is a control so both failure modes can be produced: too large
rounds corners away, too small sparkles.

## Re-verify when

- **The shader is edited.** The unit quotes the central-difference normal
  verbatim and walks the `Show` and `Shadows` `LABELS` in order.

## Corrections and open questions

- **The shader lights in code space, not in linear light.** That is wrong by
  Unit 04's own rule, since lighting is addition of light. It is the common
  shortcut and every reference shader a reader meets will share it. The unit
  lists it under Common mistakes rather than quietly doing the right thing.
  **Reconsider for Unit 27 and P3**, where several light sources are summed and
  the error compounds.
- The four-tap tetrahedral normal is described and not implemented; the shader
  uses six taps.
