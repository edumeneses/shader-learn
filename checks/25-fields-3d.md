# checks/25-fields-3d

**Unit 25.** 3D primitives and operators. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `25-fields-3d` | `library/shaders/25/fields-3d.fs` | eight primitives, five operators, shaded and step-count views |

## Attribution

The distance functions are Inigo Quilez's.

## Design notes

**The gyroid is in the list because it is not a distance field.** It is an
implicit surface whose magnitude is not a distance to anything, and it is
included so a reader can see, in the step view, what marching a non-distance
costs. The unit makes the distinction between "a distance" and "a number with
the right sign" explicit, because it is the most useful thing to be able to
recognise in this module.

The octahedron is a bound rather than an exact distance, which is why it is one
line instead of ten. The unit says so.

## Re-verify when

- **The shader is edited.** The unit walks the primitive and operator `LABELS`
  in order, and step 5 depends on Step scale reaching 1.2 producing holes in the
  gyroid.

## Corrections and open questions

- Twist, bend, and revolution are described in the unit and **not** implemented
  in the shader. Revolution in particular is one line and would be worth adding
  if a later unit needs it.
